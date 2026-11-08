using PrintIT.ViewModels;

namespace PrintIT.Views;

public partial class ExportPage : ContentPage
{
	public ExportPage(ExportViewModel viewModel)
	{
		InitializeComponent();
		BindingContext = viewModel;
	}
}
