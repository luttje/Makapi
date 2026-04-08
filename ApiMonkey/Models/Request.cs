using ApiMonkey.Services;
using CommunityToolkit.Mvvm.ComponentModel;
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

public enum TabState
{
    Headers,
    Body,
}

[ObservableObject]
public partial class Request
{
    public const string EXTENSION = "apirequest.json";

    public TabState CurrentRequestTab { get; set; } = TabState.Body;
    public TabState CurrentResponseTab { get; set; } = TabState.Body;

    [JsonInclude]
    public string Id { get; private set; }
    [JsonIgnore] public string Path { get; private set; }
    [JsonIgnore] public RequestCollection? Collection { get; internal set; }

    [ObservableProperty]
    public partial string? Name { get; set; }
    partial void OnNameChanged(string? value) => Save();

    [ObservableProperty]
    public partial string? Method { get; set; }
    partial void OnMethodChanged(string? value) => Save();

    [ObservableProperty]
    public partial string? Url { get; set; }
    partial void OnUrlChanged(string? value) => Save();

    [ObservableProperty]
    public partial string? Body { get; set; }
    partial void OnBodyChanged(string? value) => Save();

    [JsonInclude]
    public ObservableCollection<Header> Headers { get; private set; } = [];
    [JsonIgnore] public ApiResponse CachedResponse { get; internal set; }

    private readonly JsonSerializerOptions _saveJsonOptions = new()
    {
        WriteIndented = true
    };

    private bool _ready = false;

    [JsonConstructor]
    private Request() { }

    public Request(RequestCollection? collection, string defaultRequestsPath)
    {
        Id = Guid.NewGuid().ToString();
        Path = collection?.Path != null
            ? System.IO.Path.Combine(collection.Path, $"{Id}.{EXTENSION}")
            : System.IO.Path.Combine(defaultRequestsPath, $"{Id}.{EXTENSION}");
        Name = "Unnamed Request";
        Collection = collection;
        Method = "GET";
        Url = "https://echo.free.beeceptor.com";
        Body = "{\n  \"title\": \"foo\",\n  \"body\": \"bar\",\n  \"userId\": 1\n}";

        ResetToDefaultRequestHeaders();
        UpdateHeaderListeners();

        MarkReady();
        Save();
    }

    /// <summary>
    /// Only once marked ready will the request auto-save. This is to prevent saving an incomplete request while it's still being deserialized from disk.
    /// </summary>
    private void MarkReady()
    {
        Headers.CollectionChanged += (sender, args) =>
        {
            UpdateHeaderListeners();
            Save();
        };

        UpdateHeaderListeners();
        _ready = true;
    }

    private void UpdateHeaderListeners()
    {
        foreach (var header in Headers)
        {
            header.PropertyChanged += Header_PropertyChanged;
        }
    }

    private void Header_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Save();
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

        var json = ToJson();
        File.WriteAllText(Path, json);
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