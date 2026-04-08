using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ApiMonkey.Models;

internal enum TabState
{
    Headers,
    Body,
}

internal class Request : INotifyPropertyChanged, INotifyCollectionChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public TabState CurrentRequestTab { get; set; } = TabState.Body;
    public TabState CurrentResponseTab { get; set; } = TabState.Body;

    public string Id { get; private set; }
    public RequestCollection? Collection { get; internal set; }

    private string? _name;
    public string? Name
    {
        get => _name;
        set
        {
            if (_name != value)
            {
                _name = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _method;
    public string? Method
    {
        get => _method;
        set
        {
            if (_method != value)
            {
                _method = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _url;
    public string? Url
    {
        get => _url;
        set
        {
            if (_url != value)
            {
                _url = value;
                OnPropertyChanged();
            }
        }
    }

    private string? _body;
    public string? Body
    {
        get => _body;
        set
        {
            if (_body != value)
            {
                _body = value;
                OnPropertyChanged();
            }
        }
    }

    public ObservableCollection<Header> Headers { get; private set; } = [];
    public ApiResponse CachedResponse { get; internal set; }

    public Request(RequestCollection? collection = null)
    {
        Id = Guid.NewGuid().ToString();
        Name = "Unnamed Request";
        Collection = collection;
        Method = "GET";
        Url = "https://echo.free.beeceptor.com";
        Body = "{\n  \"title\": \"foo\",\n  \"body\": \"bar\",\n  \"userId\": 1\n}";

        ResetToDefaultRequestHeaders();
    }

    private void ResetToDefaultRequestHeaders()
    {
        Headers.Add(new Header
        {
            Name = "Content-Type",
            Value = "application/json"
        });
        Headers.Add(new Header
        {
            Name = "Accept",
            Value = "application/json"
        });
        Headers.Add(new Header
        {
            Name = "User-Agent",
            Value = "ApiMonkey/1.0"
        });
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}