using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using ApiMonkey.Models;
using ApiMonkey.Services;
using System.ComponentModel;

namespace ApiMonkey.Pages;

public sealed partial class WebRequestPage : Page
{
    internal Request CurrentRequest { get; private set; }

    private readonly ObservableCollection<Header> _responseHeaders = [];
    private readonly List<string> _methods = [];

    public WebRequestPage()
    {
        InitializeComponent();

        ResetMethods();

        ResponseHeadersListView.ItemsSource = _responseHeaders;
    }

    private void ResetMethods()
    {
        _methods.Clear();

        _methods.Add("GET");
        _methods.Add("POST");
        _methods.Add("PUT");
        _methods.Add("DELETE");
        _methods.Add("PATCH");
        _methods.Add("HEAD");
        _methods.Add("OPTIONS");

        MethodComboBox.ItemsSource = _methods;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        var requestId = e.Parameter as string;

        if (requestId == null)
        {
            throw new ArgumentNullException(nameof(requestId));
        }

        CurrentRequest = RequestStore.Instance.GetRequestById(requestId);

        RequestHeadersListView.ItemsSource = CurrentRequest.Headers;

        SetupActiveTab(RequestTabView, CurrentRequest.CurrentRequestTab);
        SetupActiveTab(ResponseTabView, CurrentRequest.CurrentResponseTab);

        if (CurrentRequest.CachedResponse != null)
            LoadResponse(CurrentRequest.CachedResponse);
    }

    private void SetupActiveTab(TabView tabView, TabState currentResponseTab)
    {
        switch (currentResponseTab)
        {
            case TabState.Headers:
                tabView.SelectedItem = tabView == ResponseTabView ? ResponseHeadersTab : RequestHeadersTab;
                break;
            case TabState.Body:
                tabView.SelectedItem = tabView == ResponseTabView ? ResponseBodyTab : RequestBodyTab;
                break;
            default:
                break;
        }
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlTextBox.Text;
        var selectedItem = MethodComboBox.SelectedItem as string;
        var method = selectedItem ?? "GET";
        var headers = CurrentRequest.Headers;
        var body = RequestBodyTextBox.Text;

        ResponseHeadersInfoBadge.Visibility = Visibility.Collapsed;
        ResponseBodyInfoBadge.Visibility = Visibility.Collapsed;
        _responseHeaders.Clear();

        var apiClient = new ApiClient();
        var response = await apiClient.SendRequest(url, method, body, CurrentRequest.Headers.ToArray());
        CurrentRequest.CachedResponse = response;

        LoadResponse(response);
    }

    private void LoadResponse(ApiResponse response)
    { 
        ResponseTextBox.Text = response.Body;

        ResponseHeadersInfoBadge.Value = response.Headers.Count();
        ResponseHeadersInfoBadge.Visibility = Visibility.Visible;

        ResponseBodyInfoBadge.Value = ResponseTextBox.Text.Length;
        ResponseBodyInfoBadge.Visibility = Visibility.Visible;

        foreach (var header in response.Headers)
        {
            _responseHeaders.Add(header);
        }
    }

    private void RemoveRequestHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        var header = (sender as Button)?.DataContext as Header;

        if (header == null)
            return;

        CurrentRequest.Headers.Remove(header);
    }

    private void AddRequestHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        CurrentRequest.Headers.Add(new Header());
    }

    private void RequestTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedTab = e.AddedItems.First() as TabViewItem ?? RequestBodyTab;

        CurrentRequest.CurrentRequestTab = selectedTab == RequestBodyTab 
            ? TabState.Body 
            : TabState.Headers;
    }

    private void ResponseTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var selectedTab = e.AddedItems.First() as TabViewItem ?? ResponseBodyTab;

        CurrentRequest.CurrentResponseTab = selectedTab == ResponseBodyTab 
            ? TabState.Body 
            : TabState.Headers;
    }
}
