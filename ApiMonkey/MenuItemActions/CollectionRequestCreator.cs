using ApiMonkey.Models;
using ApiMonkey.Services;

namespace ApiMonkey.MenuItemActions;

internal class CollectionRequestCreator(RequestCollection collection, RequestStore requestStore) : IMenuItemAction
{
    void IMenuItemAction.Execute() => requestStore.CreateRequest(collection);
}
