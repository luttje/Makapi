using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Makapi.Models;

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

    [JsonIgnore] public string Id { get; private set; }
    [JsonIgnore] public string Path { get; private set; }
    [JsonIgnore] public RequestCollection? Collection { get; internal set; }

    [ObservableProperty]
    public partial string? Name { get; set; }
    partial void OnNameChanged(string? value)
    {
        RenameFileToMatchName(value);
        Save();
    }

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
    private CancellationTokenSource? _debounceCts;
    private const int DebounceDelayMs = 500;

    [GeneratedRegex(@"[^a-z0-9]+")]
    private static partial Regex KebabRegex();

    [JsonConstructor]
    private Request() { }

    public Request(RequestCollection? collection, string defaultRequestsPath)
    {
        var dir = collection?.Path ?? defaultRequestsPath;
        Path = ResolveUniqueFilePath(dir, ToKebabCase("Unnamed Request"), EXTENSION);
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
        if (string.IsNullOrEmpty(Id))
            Id = Guid.NewGuid().ToString();

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

        _debounceCts?.Cancel();
        _debounceCts = new CancellationTokenSource();
        var token = _debounceCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(DebounceDelayMs, token);

                if (!token.IsCancellationRequested)
                {
                    await SaveAsync();
                }
            }
            catch (TaskCanceledException)
            {
            }
        }, token);
    }

    private async Task SaveAsync()
    {
        Directory.CreateDirectory(
            System.IO.Path.GetDirectoryName(Path) ?? throw new InvalidOperationException("Invalid request path")
        );

        var json = ToJson();
        await File.WriteAllTextAsync(Path, json);
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
            Value = "Makapi/1.0"
        });
    }

    /// <summary>
    /// Creates a new instance of the Request class from a JSON string and assigns the specified path.
    /// </summary>
    /// <param name="json">A JSON-formatted string representing the request data. If null or invalid, a new Request instance is created.</param>
    /// <param name="path">The path to assign to the Request instance. This value is set on the resulting object.</param>
    /// <returns>A Request instance populated with data from the JSON string and the specified path.</returns>
    /// <exception cref="JsonException">Thrown when the JSON string is invalid and cannot be deserialized into a Request object.</exception>
    internal static Request FromJson(string json, string path)
    {
        var request = JsonSerializer.Deserialize<Request>(json);
        request ??= new Request();
        request.Path = path;

        request.MarkReady();

        return request;
    }

    public static string ToKebabCase(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "unnamed-request";

        var kebab = KebabRegex().Replace(name.Trim().ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrEmpty(kebab) ? "unnamed-request" : kebab;
    }

    private static string ResolveUniqueFilePath(string directory, string baseName, string extension, string? excludePath = null)
    {
        Directory.CreateDirectory(directory);

        var candidate = System.IO.Path.Combine(directory, $"{baseName}.{extension}");
        if (!File.Exists(candidate) || string.Equals(candidate, excludePath, StringComparison.OrdinalIgnoreCase))
            return candidate;

        for (int i = 2; ; i++)
        {
            candidate = System.IO.Path.Combine(directory, $"{baseName}-{i}.{extension}");
            if (!File.Exists(candidate) || string.Equals(candidate, excludePath, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }
    }

    private void RenameFileToMatchName(string? newName)
    {
        if (!_ready)
            return;

        var dir = System.IO.Path.GetDirectoryName(Path);
        if (dir is null)
            return;

        var newBaseName = ToKebabCase(newName);
        var newPath = ResolveUniqueFilePath(dir, newBaseName, EXTENSION, excludePath: Path);

        if (string.Equals(Path, newPath, StringComparison.OrdinalIgnoreCase))
            return;

        var oldPath = Path;
        Path = newPath;

        if (File.Exists(oldPath))
            File.Move(oldPath, newPath, overwrite: false);
    }

    internal void MoveToDirectory(string newDirectory)
    {
        var oldPath = Path;
        var fileName = System.IO.Path.GetFileName(oldPath);
        var newPath = System.IO.Path.Combine(newDirectory, fileName);

        if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase))
            return;

        Path = newPath;

        _debounceCts?.Cancel();
        _debounceCts = null;

        _ = SaveAsync().ContinueWith(_ =>
        {
            if (File.Exists(oldPath))
                File.Delete(oldPath);
        });
    }

    internal void Delete()
    {
        File.Delete(Path);
    }
}