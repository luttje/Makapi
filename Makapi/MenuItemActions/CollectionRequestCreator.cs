using Makapi.Models;
using Makapi.Services;

namespace Makapi.MenuItemActions;

internal class CollectionRequestCreator(RequestCollection collection, RequestStore requestStore) : IMenuItemAction
{
    void IMenuItemAction.Execute() => requestStore.CreateRequest(collection);
}
