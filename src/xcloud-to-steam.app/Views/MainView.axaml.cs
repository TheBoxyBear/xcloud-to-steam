using Avalonia.Controls;

using System;

using xCloudToSteam.App.ViewModels;

namespace xCloudToSteam.App.Views;

public partial class MainView : Window
{
	private MainViewModel ViewModel => (MainViewModel)DataContext!;

	public MainView()
	{
		InitializeComponent();
	}

	protected override async void OnOpened(EventArgs _)
	{
		if (Design.IsDesignMode)
		{
			ReadOnlySpan<string> dummyGames = [
				"Fortnite",
				"Sea of Thieves",
				"Halo Infinite",
				"Microsoft Flight Simulator",
				"Psychonauts 2",
				"Grounded",
				"State of Decay 2",
				"Tell Me Why",
				"The Outer Worlds",
				"Wasteland 3"
			];

			foreach (string title in dummyGames)
				ViewModel.ProductSelections.Add(new(new() { ProductTitle = title, StoreId = string.Empty }));

			return;
		}

		await ViewModel.LoadCatalog();
	}
}
