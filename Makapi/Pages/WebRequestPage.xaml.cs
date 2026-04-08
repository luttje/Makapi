using Makapi.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Linq;

namespace Makapi.Pages;

public sealed partial class WebRequestPage : Page
{
  public WebRequestPageViewModel ViewModel { get; } = new();

  public WebRequestPage()
  {
    InitializeComponent();
  }

  protected override void OnNavigatedTo(NavigationEventArgs e)
  {
    base.OnNavigatedTo(e);

    var requestId = e.Parameter as string
        ?? throw new ArgumentNullException(nameof(e.Parameter));

    ViewModel.LoadRequest(requestId);

    // Restore selected tabs
    SetActiveTab(RequestTabView, ViewModel.Request!.CurrentRequestTab,
                 RequestBodyTab, RequestHeadersTab);
    SetActiveTab(ResponseTabView, ViewModel.Request!.CurrentResponseTab,
                 ResponseBodyTab, ResponseHeadersTab);
  }

  private static void SetActiveTab(
      TabView tabView, TabState state,
      TabViewItem bodyTab, TabViewItem headersTab)
  {
    tabView.SelectedItem = state == TabState.Body ? bodyTab : headersTab;
  }

  private void RequestTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (ViewModel.Request is null)
      return;

    var tab = e.AddedItems.FirstOrDefault() as TabViewItem ?? RequestBodyTab;

    ViewModel.Request.CurrentRequestTab = tab == RequestBodyTab
        ? TabState.Body
        : TabState.Headers;
  }

  private void ResponseTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
  {
    if (ViewModel.Request is null)
      return;

    var tab = e.AddedItems.FirstOrDefault() as TabViewItem ?? ResponseBodyTab;

    ViewModel.Request.CurrentResponseTab = tab == ResponseBodyTab
        ? TabState.Body
        : TabState.Headers;
  }
}