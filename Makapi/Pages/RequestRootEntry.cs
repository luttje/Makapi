using Makapi.Models;

namespace Makapi.Pages;

/// <summary>
/// Pairs a path value with the parent ViewModel's commands,
/// so DataTemplate buttons can bind commands via {x:Bind}
/// </summary>
public sealed class RequestRootEntry(
    ChangingValueContainer container,
    SettingsPageViewModel owner)
{
    public ChangingValueContainer Container { get; } = container;
    public SettingsPageViewModel Owner { get; } = owner;
}