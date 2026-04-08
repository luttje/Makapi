using Microsoft.UI.Xaml.Controls;

namespace ApiMonkey.Pages;

public sealed partial class SettingsPage : Page
{
    public SettingsPageViewModel ViewModel { get; } = new();

    public SettingsPage()
    {
        InitializeComponent();

        Loaded += (_, _) => ViewModel.XamlRoot = XamlRoot;
    }
}
