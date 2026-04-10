using Makapi.MenuItemActions;
using Makapi.Messages;
using Makapi.Models;
using Makapi.Pages;
using Makapi.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Makapi
{
    public sealed partial class MainWindow : Window
    {
        private readonly ObservableCollection<NavigationViewItemBase> _menuItems = [];
        private readonly Dictionary<string, NavigationViewItem> _collectionMenuItems = [];
        private NavigationViewItem? _contextMenuItem;
        private bool _noAutoOpen = false;
        private readonly RequestStore _requestStore;
        private readonly IMessenger _messenger;
        private readonly SettingsManager _settingsManager;

        public MainWindow()
        {
            InitializeComponent();

            _requestStore = App.Services.GetRequiredService<RequestStore>();
            _messenger = App.Services.GetRequiredService<IMessenger>();
            _settingsManager = App.Services.GetRequiredService<SettingsManager>();

            _messenger.Register<SettingsChangedMessage>(this, async (r, m) => await RefreshMenuItemsAsync());

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(TitleBar);

            _requestStore.RequestAdded += RequestStore_RequestAdded;
            _requestStore.RequestRemoved += RequestStore_RequestRemoved;
            _requestStore.CollectionAdded += RequestStore_CollectionAdded;
            _requestStore.CollectionRemoved += RequestStore_CollectionRemoved;
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

            if (!_noAutoOpen)
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

            if (!_noAutoOpen)
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
                Tag = new CollectionRequestCreator(collection, _requestStore),
                Icon = new SymbolIcon(Symbol.Add)
            });

            _menuItems.Add(menuItem);
            menuItem.IsExpanded = true;

            _collectionMenuItems.Add(collection.Id, menuItem);

            return menuItem;
        }

        public async Task RefreshMenuItemsAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;

            _menuItems.Clear();
            _collectionMenuItems.Clear();
            RequestsNavigationView.FooterMenuItems.Clear();

            _menuItems.Add(new NavigationViewItem
            {
                Content = "New Request",
                Tag = new RequestCreator(_requestStore),
                Icon = new SymbolIcon(Symbol.Add)
            });

            _menuItems.Add(new NavigationViewItem
            {
                Content = "New Collection",
                Tag = new CollectionCreator(RootGrid.XamlRoot, _requestStore, _settingsManager),
                Icon = new SymbolIcon(Symbol.NewFolder)
            });

            _menuItems.Add(new NavigationViewItem
            {
                Content = "Open...",
                Tag = new FileOpener(RootGrid.XamlRoot, OpenFileAndNavigateAsync),
                Icon = new SymbolIcon(Symbol.OpenFile)
            });

            _menuItems.Add(new NavigationViewItemSeparator());

            RequestsNavigationView.MenuItemsSource = _menuItems;

            RequestsNavigationView.FooterMenuItems.Add(new NavigationViewItem
            {
                Content = "Settings",
                Tag = new FrameOpener(MainFrame, typeof(SettingsPage)),
                Icon = new SymbolIcon(Symbol.Setting)
            });

            _noAutoOpen = true;
            _requestStore.ClearAll();

            // Yield so the UI message pump can render the overlay before blocking work begins
            await Task.Yield();

            await _requestStore.LoadRequestsFromDiskAsync();
            _noAutoOpen = false;

            LoadingOverlay.Visibility = Visibility.Collapsed;
        }

        private async Task OpenFileAndNavigateAsync(string? requestFilePath, string? collectionDirectory)
        {
            var directory = collectionDirectory ?? Path.GetDirectoryName(requestFilePath)!;

            if (!_settingsManager.Settings.RequestRoots.Contains(directory))
            {
                _settingsManager.Settings.RequestRoots.Add(directory);
                _settingsManager.Save();
            }

            await RefreshMenuItemsAsync();

            NavigationViewItem? menuItem = null;

            if (requestFilePath != null)
            {
                var request = _requestStore.GetRequestByFilePath(requestFilePath);
                if (request != null)
                    menuItem = FindMenuItemForRequest(request.Id);
            }
            else if (collectionDirectory != null)
            {
                var collection = _requestStore.GetCollectionByDirectory(collectionDirectory);
                if (collection != null)
                    menuItem = _collectionMenuItems.GetValueOrDefault(collection.Id);
            }

            if (menuItem != null)
                OpenMenuItem(menuItem);
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
                _requestStore.DeleteRequest(id);
            }
            else if (opener.PageType == typeof(CollectionPage))
            {
                _requestStore.DeleteCollection(id);
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

        private List<SearchResult> SearchRequests(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return [];

            var results = new List<SearchResult>();
            var searchLower = searchText.ToLower();

            // Get all requests from root and collections
            var allRequests = _requestStore.GetAllRequests();

            foreach (var request in allRequests)
            {
                var matchScore = 0;
                var matchDetails = new List<string>();

                // Search in name (highest priority)
                if (request.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                {
                    matchScore += request.Name.StartsWith(searchText, StringComparison.OrdinalIgnoreCase) ? 100 : 50;
                    matchDetails.Add("name");
                }

                // Search in URL
                if (request.Url?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                {
                    matchScore += 30;
                    matchDetails.Add("url");
                }

                // Search in headers
                foreach (var header in request.Headers)
                {
                    if (header.Name?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true ||
                        header.Value?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                    {
                        matchScore += 10;
                        if (!matchDetails.Contains("headers"))
                            matchDetails.Add("headers");
                    }
                }

                // Search in body
                if (request.Body?.Contains(searchText, StringComparison.OrdinalIgnoreCase) == true)
                {
                    matchScore += 20;
                    matchDetails.Add("body");
                }

                if (matchScore > 0)
                {
                    var collectionPrefix = request.Collection != null ? $"{request.Collection.Name} / " : "";
                    var displayText = $"{collectionPrefix}{request.Name ?? "Unnamed"} ({string.Join(", ", matchDetails)})";
                    results.Add(new SearchResult(request, displayText, matchScore));
                }
            }

            return results.OrderByDescending(r => r.MatchScore).ToList();
        }

        private void RequestsSearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                var searchResults = SearchRequests(sender.Text);

                if (searchResults.Count > 0)
                {
                    RequestsSearchBox.ItemsSource = searchResults;
                }
                else if (!string.IsNullOrWhiteSpace(sender.Text))
                {
                    RequestsSearchBox.ItemsSource = new[]
                    {
                        new FakeSearchResult("No results found")
                    };
                }
                else
                {
                    RequestsSearchBox.ItemsSource = null;
                }
            }
        }

        private void RequestsSearchBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is SearchResult result)
            {
                // Find the menu item for the selected request and open it
                var menuItem = FindMenuItemForRequest(result.Request.Id);
                if (menuItem != null)
                {
                    OpenMenuItem(menuItem);
                    sender.Text = string.Empty;
                    sender.ItemsSource = null;
                }
            }
            else if (args.ChosenSuggestion is FakeSearchResult)
            {
                // Do nothing when the "No results found" item is selected
                sender.Text = string.Empty;
                sender.ItemsSource = null;
            }
            else if (!string.IsNullOrWhiteSpace(args.QueryText))
            {
                // User pressed Enter without selecting a suggestion, open the first result
                var searchResults = SearchRequests(args.QueryText);
                if (searchResults.Count > 0)
                {
                    var menuItem = FindMenuItemForRequest(searchResults[0].Request.Id);
                    if (menuItem != null)
                    {
                        OpenMenuItem(menuItem);
                        sender.Text = string.Empty;
                        sender.ItemsSource = null;
                    }
                }
            }
        }

        private NavigationViewItem? FindMenuItemForRequest(string requestId)
        {
            // Search in root menu items
            var menuItem = _menuItems.OfType<NavigationViewItem>()
                .FirstOrDefault(item => item.Tag is FrameOpener opener &&
                                        opener.PageType == typeof(WebRequestPage) &&
                                        opener.Parameter as string == requestId);

            if (menuItem != null)
                return menuItem;

            // Search within collection menu items
            foreach (var collectionMenuItem in _collectionMenuItems.Values)
            {
                menuItem = collectionMenuItem.MenuItems.OfType<NavigationViewItem>()
                    .FirstOrDefault(item => item.Tag is FrameOpener opener &&
                                            opener.PageType == typeof(WebRequestPage) &&
                                            opener.Parameter as string == requestId);

                if (menuItem != null)
                    return menuItem;
            }

            return null;
        }

        private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
        {
            await RefreshMenuItemsAsync();
        }
    }
}
