using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Makapi.Models;
using Makapi.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace Makapi.Pages;

[ObservableObject]
public partial class WebRequestPageViewModel
{
    private readonly RequestStore _requestStore;

    public IReadOnlyList<string> Methods { get; } =
    [
        "GET", "POST", "PUT", "DELETE", "PATCH", "HEAD", "OPTIONS"
    ];

    [ObservableProperty]
    public partial Request? Request { get; set; }

    [ObservableProperty]
    public partial string ResponseBody { get; set; } = "";

    [ObservableProperty]
    public partial int ResponseHeaderCount { get; set; }

    [ObservableProperty]
    public partial int ResponseBodyLength { get; set; }

    [ObservableProperty]
    public partial bool ResponseLoaded { get; set; }

    [ObservableProperty]
    public partial bool IsSending { get; set; }

    [ObservableProperty]
    public partial string? ErrorMessage { get; set; }

    public ObservableCollection<HeaderEntry> RequestHeaders { get; } = [];
    public ObservableCollection<Header> ResponseHeaders { get; } = [];

    public string EditorTheme => Application.Current.RequestedTheme == ApplicationTheme.Dark
        ? "vs-dark"
        : "vs-light";

    public WebRequestPageViewModel()
    {
        _requestStore = App.Services.GetRequiredService<RequestStore>();
    }

    public void LoadRequest(string requestId)
    {
        Request = _requestStore.GetRequestById(requestId);

        foreach (var header in Request.Headers)
        {
            RequestHeaders.Add(new HeaderEntry(header, this));
        }

        RequestHeaders.CollectionChanged += (s, e) =>
        {
            if (Request is null)
                return;

            Request.Headers.Clear();

            foreach (var headerEntry in RequestHeaders)
            {
                Request.Headers.Add(headerEntry.Header);
            }
        };

        if (Request.CachedResponse is ApiResponse cached)
            ApplyResponse(cached);
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        if (Request is null)
            return;

        IsSending = true;
        ResponseLoaded = false;
        ErrorMessage = null;
        ResponseHeaders.Clear();

        try
        {
            var response = await new ApiClient().SendRequest(
                Request.Url,
                Request.Method,
                Request.Body,
                Request.Headers.ToArray());

            Request.CachedResponse = response;
            ApplyResponse(response);
        }
        catch (System.Net.Http.HttpRequestException ex)
        {
            ErrorMessage = $"Request failed: {ex.Message}";
            ResponseBody = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"An error occurred: {ex.Message}";
            ResponseBody = string.Empty;
        }
        finally
        {
            IsSending = false;
        }
    }

    [RelayCommand]
    private void AddRequestHeader() =>
        RequestHeaders.Add(new HeaderEntry(new Header(), this));

    [RelayCommand]
    private void RemoveRequestHeader(HeaderEntry? headerEntry)
    {
        if (headerEntry is not null)
            RequestHeaders.Remove(headerEntry);
    }

    private void ApplyResponse(ApiResponse response)
    {
        ResponseBody = response.Body;
        ResponseHeaderCount = response.Headers.Count();
        ResponseBodyLength = response.Body.Length;
        ResponseLoaded = true;

        ResponseHeaders.Clear();

        foreach (var header in response.Headers)
        {
            ResponseHeaders.Add(header);
        }
    }
}