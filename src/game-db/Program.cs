// See https://aka.ms/new-console-template for more information
using System.Text;

using xCloudToSteam.Core;
using xCloudToSteam.xCloud;
using xCloudToSteam.xCloud.Model;

IEnumerable<ProductDetails> games = xCloudShortcutManager.FilterEditions(
		xCloudApi.GetDetails(
			xCloudApi.GetCatalog().ToBlockingEnumerable(), "CA").ToBlockingEnumerable());

StringBuilder sb = new(
"""
# xCloud Game Database

Use this reference to create game shortcuts in your Steam library. Use the *Store ID* as the launch option.

| Title | Publisher | Store ID | xCloud ID |
|-------|-----------|------------------|----------|

""");

foreach (ProductDetails details in games.OrderBy(details => details.ProductTitle))
{
	string title = details.ProductTitle.Replace("©", "").Replace("®", "").Replace("™", "");
	sb.AppendLine($"| [{title}](https://xbox.com/play/games/{details.StoreId}) | {details.PublisherName} | {details.StoreId} | {details.XCloudTitleId} |");
}

File.WriteAllText("output.md", sb.ToString());
