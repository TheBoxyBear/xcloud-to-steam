using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using System.Linq;
using System.Threading.Tasks;

using xCloudToSteam.App.ViewModels;
using xCloudToSteam.App.Views;

namespace xCloudToSteam.App;

public partial class App : Application
{
	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);

		Dispatcher.UIThread.UnhandledException += (_, args) =>
		{
			Program.HandleException(args.Exception);
			//args.Handled = true;
		};
		TaskScheduler.UnobservedTaskException += (_, args) =>
		{
			Program.HandleException(args.Exception);
			//args.SetObserved();
		};
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
		{
			// Avoid duplicate validations from both Avalonia and the CommunityToolkit.
			// More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
			DisableAvaloniaDataAnnotationValidation();
			desktop.MainWindow = new MainView
			{
				DataContext = new MainViewModel(),
			};
		}

		base.OnFrameworkInitializationCompleted();
	}

	private static void DisableAvaloniaDataAnnotationValidation()
	{
		// Get an array of plugins to remove
		var dataValidationPluginsToRemove =
			BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

		// remove each entry found
		foreach (DataAnnotationsValidationPlugin plugin in dataValidationPluginsToRemove)
			BindingPlugins.DataValidators.Remove(plugin);
	}
}
