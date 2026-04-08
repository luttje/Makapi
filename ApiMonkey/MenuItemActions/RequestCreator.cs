using ApiMonkey.Services;

namespace ApiMonkey.MenuItemActions;

internal class RequestCreator(RequestStore requestStore) : IMenuItemAction
{
    void IMenuItemAction.Execute() => requestStore.CreateRequest();
}
