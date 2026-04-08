using ApiMonkey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.ViewModels;

/// <summary>
/// Pairs a path value with the parent ViewModel's commands,
/// so DataTemplate buttons can bind commands via {x:Bind} without
/// escaping the template scope with ElementName.
/// </summary>
public sealed class RequestRootEntry(
    ChangingValueContainer container,
    SettingsPageViewModel owner)
{
    public ChangingValueContainer Container { get; } = container;
    public SettingsPageViewModel Owner { get; } = owner;
}