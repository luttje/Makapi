using ApiMonkey.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ApiMonkey.Models;

internal enum TabState
{
    Headers,
    Body,
}

internal class Request : INotifyPropertyChanged, INotifyCollectionChanged
{
    public const string EXTENSION = "apirequest.json";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    public TabState CurrentRequestTab { get; set; } = TabState.Body;
    public TabState CurrentResponseTab { get; set; } = TabState.Body;

    [JsonInclude]
    public string Id { get; private set; }
    [JsonIgnore] public string Path { get; private set; }
    [JsonIgnore] public RequestCollection? Collection { get; internal set; }

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
    [JsonIgnore] public ApiResponse CachedResponse { get; internal set; }

    private readonly JsonSerializerOptions _saveJsonOptions = new()
    {
        WriteIndented = true
    };

    private bool _ready = false;

    [JsonConstructor]
    private Request() { }

    public Request(RequestCollection? collection = null)
    {
        Id = Guid.NewGuid().ToString();
        Path = collection?.Path != null
            ? System.IO.Path.Combine(collection.Path, $"{Id}.{EXTENSION}")
            : System.IO.Path.Combine(SettingsManager.GetDefaultRequestsPath(), $"{Id}.{EXTENSION}");
        Name = "Unnamed Request";
        Collection = collection;
        Method = "GET";
        Url = "https://echo.free.beeceptor.com";
        Body = "{\n  \"title\": \"foo\",\n  \"body\": \"bar\",\n  \"userId\": 1\n}";

        ResetToDefaultRequestHeaders();

        MarkReady();
        Save();
    }

    /// <summary>
    /// Only once marked ready will the request auto-save. This is to prevent saving an incomplete request while it's still being deserialized from disk.
    /// </summary>
    private void MarkReady()
    {
        _ready = true;
    }

    public string ToJson()
    {
        return JsonSerializer.Serialize(this, _saveJsonOptions);
    }

    public void Save()
    {
        if (!_ready)
            return;

        Directory.CreateDirectory(
            System.IO.Path.GetDirectoryName(Path) ?? throw new InvalidOperationException("Invalid request path")
        );

        File.WriteAllText(Path, ToJson());
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
        Save();
    }

    internal static Request FromJson(string json, string path)
    {
        var request = JsonSerializer.Deserialize<Request>(json);
        request ??= new Request();
        request.Path = path;

        request.MarkReady();

        return request;
    }

    internal void Delete()
    {
        File.Delete(Path);
    }
}