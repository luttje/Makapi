using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;

namespace Makapi.Pages;

[ObservableObject]
public partial class SettingsPageViewModel
{
    [ObservableProperty]
    public partial XamlRoot? XamlRoot { get; set; }
}
