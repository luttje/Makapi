using ApiMonkey.Models;

namespace ApiMonkey.Pages;

/// <summary>
/// Pairs a header with the parent ViewModel's commands,
/// so DataTemplate buttons can bind commands via {x:Bind}
/// </summary>
public sealed class HeaderEntry(
    Header header,
    WebRequestPageViewModel owner)
{
    public Header Header { get; } = header;
    public WebRequestPageViewModel Owner { get; } = owner;
}