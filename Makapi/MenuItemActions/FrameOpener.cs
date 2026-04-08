using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;

namespace Makapi.MenuItemActions;

internal class FrameOpener(Frame frame, Type pageType, object? parameter = null) : IMenuItemAction
{
  public Type PageType => pageType;
  public object? Parameter => parameter;

  void IMenuItemAction.Execute() => frame.NavigateToType(
      pageType,
      parameter,
      new FrameNavigationOptions
      {
        IsNavigationStackEnabled = false,
      }
  );
}