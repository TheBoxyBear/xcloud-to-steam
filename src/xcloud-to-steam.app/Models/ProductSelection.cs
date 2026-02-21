using CommunityToolkit.Mvvm.ComponentModel;

using xCloudToSteam.xCloud.Model;

namespace xCloudToSteam.App.Models;

public enum ProductSelectionState : byte
{
	Missing,
	Added,
	ToAdd,
	ToRemove
}

public partial class ProductSelection(ProductDetails details) : ObservableObject
{
	[ObservableProperty]
	private ProductDetails _details = details;

	[ObservableProperty]
	private ProductSelectionState _selectionState = ProductSelectionState.Missing;
}
