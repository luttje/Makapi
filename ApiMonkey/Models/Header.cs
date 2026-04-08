using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace ApiMonkey.Models;

[ObservableObject]
public partial class Header
{
    [ObservableProperty]
    public partial string? Name { get; set; }

    [ObservableProperty]
    public partial string? Value { get; set; }

    [JsonConstructor]
    public Header() { }
}
