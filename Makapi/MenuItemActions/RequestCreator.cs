using Makapi.Services;

namespace Makapi.MenuItemActions;

internal class RequestCreator(RequestStore requestStore) : IMenuItemAction
{
  void IMenuItemAction.Execute() => requestStore.CreateRequest();
}
