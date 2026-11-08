using PrintIT.ViewModels;

namespace PrintIT.Views;

public partial class SessionsPage : ContentPage
{
    private readonly MainViewModel _viewModel;

    public SessionsPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.LoadSessions();
    }
}