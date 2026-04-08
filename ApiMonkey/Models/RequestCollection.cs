using ApiMonkey.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ApiMonkey.Models;

[ObservableObject]
public partial class RequestCollection
{
    public const string EXTENSION = "apicollection.json";

    [JsonInclude]
    public string Id { get; private set; }
    [JsonIgnore] public string Path { get; private set; }
    [JsonIgnore] public List<Request> Requests { get; private set; } = [];

    [ObservableProperty]
    public partial string? Name { get; set; }
    partial void OnNameChanged(string? value) => Save();

    private readonly JsonSerializerOptions _saveJsonOptions = new()
    {
        WriteIndented = true
    };

    private bool _ready = false;
    private CancellationTokenSource? _debounceCts;
    private const int DebounceDelayMs = 500;

    [JsonConstructor]
    private RequestCollection() { }

    public RequestCollection(string path, SettingsManager settingsManager)
    {
        Id = Guid.NewGuid().ToString();
        Path = path;
        Name = "Unnamed Collection";

        // We add the path to the setting roots, so we can find the collection when we later open the app
        settingsManager.Settings.TryAddExclusiveRoot(path);
        settingsManager.Save();

        MarkReady();
        Save();
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
        Directory.CreateDirectory(Path);

        var collectionFilePath = System.IO.Path.Combine(Path, $"collection.{EXTENSION}");
        var json = JsonSerializer.Serialize(this, _saveJsonOptions);

        await File.WriteAllTextAsync(collectionFilePath, json);
    }

    /// <summary>
    /// Only once marked ready will the request auto-save. This is to prevent saving an incomplete request while it's still being deserialized from disk.
    /// </summary>
    private void MarkReady()
    {
        _ready = true;
    }

    /// <summary>
    /// Creates a RequestCollection from a JSON string. The path is needed to set the collection's Path property, which is not stored in the JSON but is needed for the collection to function properly.
    /// </summary>
    /// <param name="json">The JSON string to deserialize</param>
    /// <param name="path">The path to the collection directory</param>
    /// <returns>A RequestCollection instance</returns>
    /// <exception cref="JsonException">Thrown if the JSON is invalid or cannot be deserialized into a RequestCollection</exception>
    internal static RequestCollection FromJson(string json, string path)
    {
        var collection = JsonSerializer.Deserialize<RequestCollection>(json);
        collection ??= new RequestCollection();
        collection.Path = path;

        collection.MarkReady();

        return collection;
    }

    internal void Delete()
    {
        Directory.Delete(Path, true);
    }
}
