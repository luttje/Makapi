using ApiMonkey.Services;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.MenuItemActions;

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