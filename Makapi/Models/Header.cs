using CommunityToolkit.Mvvm.ComponentModel;
using System.Text.Json.Serialization;

namespace Makapi.Models;

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
