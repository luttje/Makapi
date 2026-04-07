using ApiMonkey.Models;
using ApiMonkey.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.MenuItemActions;

internal class CollectionCreator() : IMenuItemAction
{
    void IMenuItemAction.Execute() => RequestStore.Instance.CreateCollection();
}