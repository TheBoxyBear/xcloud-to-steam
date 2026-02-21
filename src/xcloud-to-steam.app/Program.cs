using Avalonia;
using Avalonia.Threading;

using MsBox.Avalonia;
using MsBox.Avalonia.Enums;

using System;

namespace xCloudToSteam.App;

internal sealed class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		try
		{
			BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
		}
		catch (Exception e)
		{
			var box = MessageBoxManager.GetMessageBoxStandard("Unhandled exception", e.ToString(), ButtonEnum.Ok);
			box.ShowAsync().GetAwaiter().GetResult();
			Environment.Exit(1);
		}
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();

	public static async void HandleException(Exception ex)
	{
		await Dispatcher.UIThread.Invoke(async () =>
		{
			var box = MessageBoxManager.GetMessageBoxStandard("Unhandled exception", ex.ToString(), ButtonEnum.Ok);
			await box.ShowAsync();
		});

		Environment.Exit(1);
	}
}
