using CommunityToolkit.Mvvm.ComponentModel;

namespace ApiMonkey.Models;

[ObservableObject]
public partial class ChangingValueContainer
{
    [ObservableProperty]
    public partial string? Value { get; set; }

    public ChangingValueContainer(string value)
    {
        Value = value;
    }
}
