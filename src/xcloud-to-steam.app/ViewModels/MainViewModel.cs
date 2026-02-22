using Avalonia.Styling;
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
using System.Threading;
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
	private ObservableCollection<SteamUser> _steamUsers;

	[ObservableProperty]
	private SteamUser _selectedSteamUser;

	[ObservableProperty]
	private string[] _markets = [ "CA", "US", "GB", "DE", "FR", "JP" ];

	[ObservableProperty]
	private string _selectedMarket = "US";

	[ObservableProperty]
	private ObservableCollection<KeyValuePair<string, ShortcutConfigProfile>> _configProfiles;

	[ObservableProperty]
	private KeyValuePair<string, ShortcutConfigProfile> _selectedConfigProfile;

	[ObservableProperty]
	private bool _locked = true;

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
	private List<SteamShortcut> m_shortcuts;
	private SteamUserSession m_session;

	public MainViewModel() { }

	public async Task Initialize()
	{
		Task<ProductDetails[]> getCatalogTask = Task.Run(GetCatalogTask);

		SteamUsers = [.. SteamManager.GetUsers()];
		SelectedSteamUser = SteamUsers.FirstOrDefault(user => user.MostRecent, SteamUsers[0]);
		m_session = new(SelectedSteamUser);

		Task shortcutTask = Task.Run(LoadCurrentUserShortcuts);

		m_config = JsonSerializer.Deserialize(File.ReadAllText("config.json"), AppJsonContext.Default.AppConfig)!;

		ConfigProfiles = [.. m_config.Profiles.GetProfilesForCurrentOS()];
		SelectedConfigProfile = ConfigProfiles.First();

		ObservableCollection<ProductSelection> selections = new(GetSelectionsFromCatalog(await getCatalogTask));

		await shortcutTask;
		UpdateStatusesForCurrentUser(selections);

		Dispatcher.UIThread.Post(() =>
		{
			ProductSelections = selections;
			Locked = false;
		});
	}

	private static Task<ProductDetails[]> GetCatalogTask()
		=> xCloudApi.GetCatalog().ToArrayAsync().AsTask()
			.ContinueWith(task => xCloudApi.GetDetails(task.Result, "CA").ToArrayAsync().AsTask())
			.Unwrap();

	private static IEnumerable<ProductSelection> GetSelectionsFromCatalog(ProductDetails[] details)
		=> details.FilterEditions().OrderBy(d => d.ProductTitle).Select(entry =>
		{
			string formattedTitle = entry.ProductTitle;

			if (formattedTitle.EndsWith(" Standard Edition", StringComparison.OrdinalIgnoreCase))
				formattedTitle = formattedTitle[..^" Standard Edition".Length];

			formattedTitle = formattedTitle.Replace("©", "").Replace("®", "").Replace("™", "");

			return new ProductSelection(formattedTitle != entry.ProductTitle
				? entry with { ProductTitle = formattedTitle } : entry);
		});

	private void LoadCurrentUserShortcuts()
	{
		m_shortcuts = SteamShortcut.Read(m_session);
		m_shortcutDict = m_shortcuts
			.Where(static s => s.IsXCloudShortcut)
			.ToDictionary(static s => s.XCloudStoreId);
	}

	public void UpdateStatusesForCurrentUser(ObservableCollection<ProductSelection> selections)
	{
		foreach (ProductSelection selection in selections)
		{
			ProductSelectionState newState = m_shortcutDict.ContainsKey(selection.Details.StoreId)
				? ProductSelectionState.Added
				: ProductSelectionState.Missing;

			if (selection.SelectionState != newState)
			{
				m_selectionGroups[selection.SelectionState].Remove(selection);
				UpdateSelectionState(selection, newState);
			}
		}
	}

	partial void OnSelectedSteamUserChanged(SteamUser? oldValue, SteamUser newValue)
	{
		// Handled more optimally in Initialize
		if (oldValue is null)
			return;

		LoadCurrentUserShortcuts();
		UpdateStatusesForCurrentUser(ProductSelections);

		m_session = new(SelectedSteamUser);
	}

	async partial void OnSelectedMarketChanged(string? oldValue, string newValue)
	{
		// Handled better in Initialize
		if (oldValue is null)
			return;

		Locked = true;

		ObservableCollection<ProductSelection> selections = [.. GetSelectionsFromCatalog(await GetCatalogTask())];
		UpdateStatusesForCurrentUser(selections);

		ProductSelections = selections;

		Locked = false;
	}

	[RelayCommand]
	public void Product_OnClick(ProductSelection selection)
	{
		m_selectionGroups[selection.SelectionState].Remove(selection);

		UpdateSelectionState(selection, selection.SelectionState switch
		{
			ProductSelectionState.Missing => ProductSelectionState.ToAdd,
			ProductSelectionState.Added => ProductSelectionState.ToRemove,
			ProductSelectionState.ToAdd => ProductSelectionState.Missing,
			ProductSelectionState.ToRemove => ProductSelectionState.Added
		});
	}

	private void UpdateSelectionState(ProductSelection selection, ProductSelectionState newState)
	{
		m_selectionGroups[newState].AddLast(selection);
		selection.SelectionState = newState;
	}

	[RelayCommand]
	public async Task Apply()
	{
		Locked = true;
		ApplyStatus = "Applying...";

		LinkedList<ProductSelection>
			toAddList    = m_selectionGroups[ProductSelectionState.ToAdd],
			toModifyList = m_selectionGroups[ProductSelectionState.Added],
			toRemoveList = m_selectionGroups[ProductSelectionState.ToRemove];

		uint
			completedTasks = 0,
			totalTasks     = (uint)(toAddList.Count + toModifyList.Count + toRemoveList.Count);

		ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = 4 };

		Task addTask = Parallel.ForEachAsync(toAddList, parallelOptions,
			async (selection, ct) =>
			{
				try
				{
					SteamShortcut shortcut = await xCloudShortcutManager.CreateShortcut(
							m_session, selection.Details, SelectedConfigProfile.Value, ct);

					lock (m_shortcuts)
						m_shortcuts.Add(shortcut);

					lock (m_shortcutDict)
						m_shortcutDict[selection.Details.StoreId] = shortcut;

					OnTaskComplete();
				}
				catch (Exception ex) { Program.HandleException(ex); }
			});

		Task modifyTask = Parallel.ForEachAsync(toModifyList, parallelOptions,
			async (selection, ct) =>
			{
				try
				{
					await xCloudShortcutManager.ModifyShortcut(
						m_session, m_shortcutDict[selection.Details.StoreId], selection.Details, SelectedConfigProfile.Value, ct);

					OnTaskComplete();
				}
				catch (Exception ex) { Program.HandleException(ex); }
			});

		foreach (ProductSelection toRemove in toRemoveList)
			if (m_shortcutDict.TryGetValue(toRemove.Details.StoreId, out SteamShortcut? shortcut))
			{
				m_shortcuts.Remove(shortcut);
				m_shortcutDict.Remove(toRemove.Details.StoreId);

				OnTaskComplete();
			}

		await Task.WhenAll(addTask, modifyTask);

		foreach (ProductSelection selection in toAddList)
			UpdateSelectionState(selection, ProductSelectionState.Added);

		foreach (ProductSelection selection in toRemoveList)
			UpdateSelectionState(selection, ProductSelectionState.Missing);

		m_selectionGroups[ProductSelectionState.ToAdd].Clear();
		m_selectionGroups[ProductSelectionState.ToRemove].Clear();

		await SteamShortcut.Write(m_session, m_shortcuts);

		// Could be ran inline, but queing it avoids race conditions with the parallel tasks updating the status
		Dispatcher.UIThread.Post(() =>
		{
			ApplyStatus = "Done!";
			Locked = false;
		}, DispatcherPriority.Background);

		void OnTaskComplete()
		{
			Interlocked.Increment(ref completedTasks);

			Dispatcher.UIThread.Post(() =>
				ApplyStatus = $"Applying... ({completedTasks}/{totalTasks})",
				DispatcherPriority.Background);
		}
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
