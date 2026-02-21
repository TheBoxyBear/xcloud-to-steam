using Avalonia.Threading;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;

using xCloudToSteam.App.Models;
using xCloudToSteam.Core;
using xCloudToSteam.Core.Config;
using xCloudToSteam.Steam;
using xCloudToSteam.Steam.Model;
using xCloudToSteam.xCloud;
using xCloudToSteam.xCloud.Model;

namespace xCloudToSteam.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
	[ObservableProperty]
	private ObservableCollection<ProductSelection> _productSelections = [];

	[ObservableProperty]
	private SteamUser? _steamUser;

	[ObservableProperty]
	private ShortcutConfigProfile _shortcutConfigProfile;

	[ObservableProperty]
	private ObservableCollection<ShortcutConfigProfile> _configProfiles;

	[ObservableProperty]
	private bool _canApply = false;

	[ObservableProperty]
	private bool _canSelect = true;

	[ObservableProperty]
	private string _applyStatus = string.Empty;

	private Dictionary<string, SteamShortcut> m_shortcutDict = [];
	private readonly Dictionary<ProductSelectionState, LinkedList<ProductSelection>> m_selectionGroups = new()
	{
		{ ProductSelectionState.Missing, [] },
		{ ProductSelectionState.Added, [] },
		{ ProductSelectionState.ToAdd, [] },
		{ ProductSelectionState.ToRemove, [] },
	};

	private AppConfig m_config;
	private SteamUserSession m_session;
	private List<SteamShortcut> m_shortcuts;

	public MainViewModel()
	{

	}

	public async Task LoadCatalog()
	{
		Task<ProductDetails[]> getCatalogTask =
			xCloudApi.GetCatalog().ToArrayAsync().AsTask()
			.ContinueWith(task => xCloudApi.GetDetails(task.Result).ToArrayAsync().AsTask())
			.Unwrap();

		m_config       = JsonSerializer.Deserialize(File.ReadAllText("config.json"), AppJsonContext.Default.AppConfig)!;
		m_session      = new(SteamManager.GetUsers().First(u => u.MostRecent));
		m_shortcuts    = SteamShortcut.Read(m_session);
		m_shortcutDict = m_shortcuts
							.Where(static s => s.IsXCloudShortcut)
							.ToDictionary(static s => s.XCloudStoreId);

		ObservableCollection<ProductSelection> selections = [];

		foreach (ProductDetails entry in (await getCatalogTask).OrderBy(d => d.ProductTitle))
		{
			ProductSelection selection = new(entry)
			{
				SelectionState = m_shortcutDict.ContainsKey(entry.StoreId)
					? ProductSelectionState.Added
					: ProductSelectionState.Missing
			};

			selections.Add(selection);
			m_selectionGroups[selection.SelectionState].AddLast(selection);
		}

		Dispatcher.UIThread.Post(() =>
		{
			ProductSelections = selections;
			CanApply = true;
		});
	}

	[RelayCommand]
	public void Product_OnClick(ProductSelection selection)
		=> UpdateSelectionState(selection, selection.SelectionState switch
		{
			ProductSelectionState.Missing  => ProductSelectionState.ToAdd,
			ProductSelectionState.Added    => ProductSelectionState.ToRemove,
			ProductSelectionState.ToAdd    => ProductSelectionState.Missing,
			ProductSelectionState.ToRemove => ProductSelectionState.Added
		});

	private void UpdateSelectionState(ProductSelection selection, ProductSelectionState newState)
	{
		m_selectionGroups[selection.SelectionState].Remove(selection);

		m_selectionGroups[newState].AddLast(selection);
		selection.SelectionState = newState;
	}

	[RelayCommand]
	public async Task Apply()
	{
		CanApply = false;
		CanSelect = false;
		ApplyStatus = "Applying...";

		ShortcutConfigProfile profile = m_config.Profiles.GetProfilesForCurrentOS().Values.First();

		Task<SteamShortcut>[] addTasks = [.. m_selectionGroups[ProductSelectionState.ToAdd].Select(s => xCloudShortcutManager.CreateShortcut(m_session, s.Details, profile))];
		Task[] modifyTasks = [.. m_selectionGroups[ProductSelectionState.Added].Select(s => xCloudShortcutManager.ModifyShortcut(m_session, m_shortcutDict[s.Details.StoreId], s.Details, profile))];

		foreach (ProductSelection toRemove in m_selectionGroups[ProductSelectionState.ToRemove])
		{
			int removeIndex = m_shortcuts.FindIndex(s => s.IsXCloudShortcut && s.XCloudStoreId == toRemove.Details.StoreId);

			if (removeIndex >= 0)
				m_shortcuts.RemoveAt(removeIndex);
		}

		await Task.WhenAll(addTasks);
		m_shortcuts.AddRange(addTasks.Select(t => t.Result));

		foreach (ProductSelection selection in m_selectionGroups[ProductSelectionState.ToAdd].ToArray())
			UpdateSelectionState(selection, ProductSelectionState.Added);

		foreach (ProductSelection selection in m_selectionGroups[ProductSelectionState.ToRemove].ToArray())
			UpdateSelectionState(selection, ProductSelectionState.Missing);

		await Task.WhenAll(modifyTasks);

		await SteamShortcut.Write(m_session, m_shortcuts);

		ApplyStatus = "Done!";
		CanApply = true;
		CanSelect = true;
	}

	[RelayCommand]
	public void ViewDetailsPage(ProductDetails details)
	{
		string url = $"https://www.xbox.com/play/games/{details.StoreId}";

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			Process.Start("xdg-open", url);
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			Process.Start("open", url);
	}
}
