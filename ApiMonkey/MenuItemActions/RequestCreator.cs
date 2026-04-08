using ApiMonkey.Models;
using ApiMonkey.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.MenuItemActions;

internal class RequestCreator(RequestStore requestStore) : IMenuItemAction
{
    void IMenuItemAction.Execute() => requestStore.CreateRequest();
}
