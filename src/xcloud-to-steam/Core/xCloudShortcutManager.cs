using System.Text.RegularExpressions;

using xCloudToSteam.Core.Config;
using xCloudToSteam.Steam;
using xCloudToSteam.Steam.Model;
using xCloudToSteam.xCloud;
using xCloudToSteam.xCloud.Model;

namespace xCloudToSteam.Core;

public static partial class xCloudShortcutManager
{
	private const string xCloudTag = "xCloud";

	extension (SteamShortcut shortcut)
	{
		public bool IsXCloudShortcut
			=> shortcut.Tags.ElementAtOrDefault(0) == xCloudTag;

		public string XCloudStoreId
		{
			get
			{
				if (!shortcut.IsXCloudShortcut)
					throw new InvalidOperationException("Shortcut is not an xCloud shortcut");

				return shortcut.Tags.ElementAtOrDefault(1) ?? throw new InvalidDataException("xCloud shortcut missing store id tag");
			}
		}
	}

	public static SteamShortcut? Find(string storeId, params IEnumerable<SteamShortcut> shortcuts)
		=> shortcuts.FirstOrDefault(s => s.IsXCloudShortcut && s.XCloudStoreId == storeId);

	public static async Task<SteamShortcut> CreateShortcut(SteamUserSession session, ProductDetails details, ShortcutConfigProfile config, CancellationToken cancellationToken = default)
	{
		SteamShortcut shortcut = SteamShortcut.Create(FillTemplate(config.AppName, details), FillTemplate(config.Exe, details)) with
		{
			StartDir      = FillTemplate(config.WorkingDir, details),
			LaunchOptions = FillTemplate(config.Args, details),
			Tags          = [xCloudTag, details.StoreId]
		};

		if (details.ImagePoster is not null)
		{
			string coverPath = SteamManager.GetGridImagePath(session, shortcut.AppId, ImageType.Cover);
			await DownloadImage(details.ImagePoster, coverPath, cancellationToken);
		}

		string heroPath = SteamManager.GetGridImagePath(session, shortcut.AppId, ImageType.Hero);
		StoreDetails storeDetails = await xCloudApi.GetStoreDetails(details.StoreId, cancellationToken);

		string heroArtUrl = storeDetails.ProductSummaries[0].Images.SuperHeroArt;

		if (heroArtUrl is not null)
			await DownloadImage(heroArtUrl, heroPath, cancellationToken);

		return shortcut;
	}

	public static async Task ModifyShortcut(SteamUserSession session, SteamShortcut shortcut, ProductDetails details, ShortcutConfigProfile config, CancellationToken cancellationToken = default)
	{
		if (!shortcut.IsXCloudShortcut)
			throw new ArgumentException("Shortcut is not an xCloud shortcut", nameof(shortcut));

		shortcut.AppName       = FillTemplate(config.AppName, details);
		shortcut.Exe           = FillTemplate(config.Exe, details);
		shortcut.StartDir      = FillTemplate(config.WorkingDir, details);
		shortcut.LaunchOptions = FillTemplate(config.Args, details);

		string coverPath = SteamManager.GetGridImagePath(session, shortcut.AppId, ImageType.Cover);

		if (!File.Exists(coverPath) && details.ImagePoster is not null)
			await DownloadImage(details.ImagePoster, coverPath, cancellationToken);

		string heroPath = SteamManager.GetGridImagePath(session, shortcut.AppId, ImageType.Hero);

		if (!File.Exists(heroPath))
		{
			StoreDetails storeDetails = await xCloudApi.GetStoreDetails(details.StoreId);
			string heroArtUrl = storeDetails.ProductSummaries[0].Images.SuperHeroArt;

			if (heroArtUrl is not null)
				await DownloadImage(heroArtUrl, heroPath, cancellationToken);
		}
	}

	private static string FillTemplate(string template, ProductDetails details)
		=> s_templateRegex.Replace(template, match =>
			match.Groups["varName"].Value.ToLower() switch
			{
				"title"     => details.ProductTitle ?? "Unknown Title",
				"publisher" => details.PublisherName ?? "Unknown Publisher",
				"xcloudid"  => details.XCloudTitleId ?? "Unknown xCloud Id",
				"storeid"   => details.StoreId ?? "Unknown store Id",
				"steam"     => SteamManager.SteamPath,
				"home"		=> Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				_ => match.Value
			});

	private static readonly Regex s_templateRegex = TemplateRegex();

	[GeneratedRegex("\\{(?<varName>\\w+)\\}", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
	private static partial Regex TemplateRegex();

	private static async Task DownloadImage(string url, string outputPath, CancellationToken cancellationToken)
	{
		if (url.StartsWith("//"))
			url = "https:" + url;

		if (string.IsNullOrEmpty(Path.GetExtension(outputPath)))
			outputPath += ".png";

		if (File.Exists(outputPath))
			return;

		Stream webStream;

		using (HttpClient client = new())
			webStream = await client.GetStreamAsync(url, cancellationToken);

		using FileStream fs = File.OpenWrite(outputPath);
		webStream.CopyTo(fs);
	}
}
