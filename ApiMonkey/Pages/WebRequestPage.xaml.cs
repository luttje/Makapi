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

namespace ApiMonkey.Pages;

public sealed partial class WebRequestPage : Page
{
    internal Request CurrentRequest { get; private set; }

    private readonly ObservableCollection<Header> _requestHeaders = [];
    private readonly ObservableCollection<Header> _responseHeaders = [];

    public WebRequestPage()
    {
        InitializeComponent();

        RequestHeadersListView.ItemsSource = _requestHeaders;
        ResponseHeadersListView.ItemsSource = _responseHeaders;

        // Put some test data in the request body
        RequestBodyTextBox.Text = "{\n  \"title\": \"foo\",\n  \"body\": \"bar\",\n  \"userId\": 1\n}";

        FillDefaultRequestHeaders();
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
    }

    private void FillDefaultRequestHeaders()
    {
        _requestHeaders.Add(new Header
        {
            Name = "Content-Type",
            Value = "application/json"
        });
        _requestHeaders.Add(new Header
        {
            Name = "Accept",
            Value = "application/json"
        });
        _requestHeaders.Add(new Header
        {
            Name = "User-Agent",
            Value = "ApiMonkey/1.0"
        });
    }

    private async void SendButton_Click(object sender, RoutedEventArgs e)
    {
        var url = UrlTextBox.Text;
        var selectedItem = MethodComboBox.SelectedItem as ComboBoxItem;
        var method = selectedItem.Content as string ?? "GET";
        var headers = _requestHeaders;
        var body = RequestBodyTextBox.Text;

        ResponseHeadersInfoBadge.Visibility = Visibility.Collapsed;
        ResponseBodyInfoBadge.Visibility = Visibility.Collapsed;
        _responseHeaders.Clear();

        var apiClient = new ApiClient();
        var response = await apiClient.SendRequest(url, method, body, _requestHeaders.ToArray());

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

        _requestHeaders.Remove(header);
    }

    private void AddRequestHeaderButton_Click(object sender, RoutedEventArgs e)
    {
        _requestHeaders.Add(new Header());
    }
}
