using ApiMonkey.MenuItemActions;
using ApiMonkey.Models;
using ApiMonkey.Pages;
using ApiMonkey.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI;
using static System.Net.Mime.MediaTypeNames;

namespace ApiMonkey
{
    public sealed partial class MainWindow : Window
    {
        private readonly ObservableCollection<NavigationViewItemBase> _menuItems = [];
        private readonly List<NavigationViewItemBase> _permanentItems = [];

        private readonly Dictionary<string, NavigationViewItem> _collectionMenuItems = [];
        private NavigationViewItem? _contextMenuItem;

        public MainWindow()
        {
            InitializeComponent();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);

            InitializeMenuItems();

            RequestStore.Instance.RequestAdded += RequestStore_RequestAdded;
            RequestStore.Instance.RequestRemoved += RequestStore_RequestRemoved;
            RequestStore.Instance.CollectionAdded += RequestStore_CollectionAdded;
            RequestStore.Instance.CollectionRemoved += RequestStore_CollectionRemoved;
        }

        private void RequestStore_RequestAdded(Request request)
        {
            NavigationViewItem menuItem;

            if (request.Collection != null)
            {
                var collectionMenuItem = _collectionMenuItems.GetValueOrDefault(request.Collection.Id);

                if (collectionMenuItem == null)
                    throw new ArgumentNullException(nameof(collectionMenuItem));

                menuItem = AddRequestMenuItem(request, (IList<object>?)collectionMenuItem.MenuItems);

            }
            else
            {
                menuItem = AddRequestMenuItem(request);
            }

            OpenMenuItem(menuItem);
        }

        private void RequestStore_RequestRemoved(Request request)
        {
            // Find the menu item for the request and remove it
            var menuItem = _menuItems.OfType<NavigationViewItem>()
                .FirstOrDefault(item => item.Tag is FrameOpener opener &&
                                        opener.PageType == typeof(WebRequestPage) &&
                                        opener.Parameter as string == request.Id);
            if (menuItem != null)
            {
                _menuItems.Remove(menuItem);
            }
            else
            {
                // If not found in the main menu, check within collections
                foreach (var collectionMenuItem in _collectionMenuItems.Values)
                {
                    var requestMenuItem = collectionMenuItem.MenuItems.OfType<NavigationViewItem>()
                        .FirstOrDefault(item => item.Tag is FrameOpener opener &&
                                                opener.PageType == typeof(WebRequestPage) &&
                                                opener.Parameter as string == request.Id);
                    if (requestMenuItem != null)
                    {
                        collectionMenuItem.MenuItems.Remove(requestMenuItem);
                        break;
                    }
                }
            }
        }

        private void RequestStore_CollectionAdded(RequestCollection collection)
        {
            var menuItem = AddCollectionMenuItem(collection);
            OpenMenuItem(menuItem);
        }

        private void RequestStore_CollectionRemoved(RequestCollection collection)
        {
            // Find the menu item for the collection and remove it
            var menuItem = _collectionMenuItems.GetValueOrDefault(collection.Id);
            if (menuItem != null)
            {
                _menuItems.Remove(menuItem);
                _collectionMenuItems.Remove(collection.Id);
            }
        }

        private NavigationViewItemBase AddPermanentMenuItem(NavigationViewItemBase menuItem)
        {
            _permanentItems.Add(menuItem);
            _menuItems.Add(menuItem);
            return menuItem;
        }

        private NavigationViewItem AddRequestMenuItem(Request request, IList<object>? parent = null)
        {
            var menuItem = new NavigationViewItem
            {
                Content = request.Name,
                Tag = new FrameOpener(MainFrame, typeof(WebRequestPage), request.Id),
                Icon = new SymbolIcon(Symbol.Document),
            };
            menuItem.DataContext = menuItem;

            request.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Request.Name))
                {
                    menuItem.Content = request.Name;
                }
            };

            if (parent != null)
                parent.Add(menuItem);
            else
                _menuItems.Add(menuItem);

            return menuItem;
        }

        private NavigationViewItem AddCollectionMenuItem(RequestCollection collection)
        {
            var menuItem = new NavigationViewItem
            {
                Content = collection.Name,
                Tag = new FrameOpener(MainFrame, typeof(CollectionPage), collection.Id),
                Icon = new SymbolIcon(Symbol.Folder),
            };

            collection.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(RequestCollection.Name))
                {
                    menuItem.Content = collection.Name;
                }
            };

            menuItem.MenuItems.Add(new NavigationViewItem
            {
                Content = "New Request",
                Tag = new CollectionRequestCreator(collection),
                Icon = new SymbolIcon(Symbol.Add)
            });

            _menuItems.Add(menuItem);
            menuItem.IsExpanded = true;

            _collectionMenuItems.Add(collection.Id, menuItem);

            return menuItem;
        }

        private void InitializeMenuItems()
        {
            _menuItems.Clear();

            AddPermanentMenuItem(new NavigationViewItem
            {
                Content = "New Request",
                Tag = new RequestCreator(),
                Icon = new SymbolIcon(Symbol.Add)
            });

            AddPermanentMenuItem(new NavigationViewItem
            {
                Content = "New Collection",
                Tag = new CollectionCreator(),
                Icon = new SymbolIcon(Symbol.NewFolder)
            });

            AddPermanentMenuItem(new NavigationViewItemSeparator());

            RequestsNavigationView.MenuItemsSource = _menuItems;

            RequestsNavigationView.FooterMenuItems.Add(new NavigationViewItem
            {
                Content = "Settings",
                Tag = new FrameOpener(MainFrame, typeof(SettingsPage)),
                Icon = new SymbolIcon(Symbol.Setting)
            });
        }

        private void RequestsNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var selectedItem = args.SelectedItem as NavigationViewItem;

            if (selectedItem == null)
                return;

            if (selectedItem.Tag is not IMenuItemAction action)
                return;

            action.Execute();
        }

        private void OpenMenuItem(NavigationViewItem menuItem)
        {
            if (menuItem.Tag is IMenuItemAction action)
                action.Execute();

            RequestsNavigationView.SelectedItem = menuItem;
        }

        private void RequestsNavigationView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            var source = e.OriginalSource as DependencyObject;
            _contextMenuItem = FindAncestor<NavigationViewItem>(source);

            if (_contextMenuItem == null)
                return;

            ContextCommandBarFlyout.ShowAt(_contextMenuItem, new FlyoutShowOptions
            {
                ShowMode = FlyoutShowMode.Transient,
                Placement = FlyoutPlacementMode.Left,
                Position = e.GetPosition(_contextMenuItem)
            });
        }

        private static T? FindAncestor<T>(DependencyObject? element) where T : DependencyObject
        {
            while (element != null)
            {
                if (element is T match)
                    return match;

                element = VisualTreeHelper.GetParent(element);
            }

            return null;
        }

        private void ContextDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_contextMenuItem?.Tag is not FrameOpener opener)
                return;


            if (opener.Parameter is not string id)
                return;

            if (opener.PageType == typeof(WebRequestPage))
            {
                RequestStore.Instance.DeleteRequest(id);
            }
            else if (opener.PageType == typeof(CollectionPage))
            {
                RequestStore.Instance.DeleteCollection(id);
            }
        }

        private void CtrlF_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            RequestsSearchBox.Focus(FocusState.Programmatic);
        }

        private void TitleBar_PaneToggleRequested(TitleBar sender, object args)
        {
            RequestsNavigationView.IsPaneOpen = !RequestsNavigationView.IsPaneOpen;
        }

        private void RequestsSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                // TODO: Put a record type in this list instead and search by more than just name (e.g. url, headers, body, etc.)
                var suggestions = new List<string>();

                if (suggestions.Count > 0)
                {
                    RequestsSearchBox.ItemsSource = suggestions
                        .OrderByDescending(i => i.StartsWith(sender.Text, StringComparison.CurrentCultureIgnoreCase))
                        .ThenBy(i => i)
                        .ToList();
                }
                else
                {
                    // RequestsSearchBox.ItemsSource = new string[] { "No results found" };
                    RequestsSearchBox.ItemsSource = new string[] { "Search functionality is not implemented yet" };
                }
            }
        }
    }
}
