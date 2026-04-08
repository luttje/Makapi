using ApiMonkey.Services;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ApiMonkey.Models;

internal class RequestCollection : INotifyPropertyChanged
{
    public const string EXTENSION = "apicollection.json";

    public event PropertyChangedEventHandler? PropertyChanged;

    [JsonInclude]
    public string Id { get; private set; }
    [JsonIgnore] public string Path { get; private set; }
    [JsonIgnore] public List<Request> Requests { get; private set; } = [];

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

    private readonly JsonSerializerOptions _saveJsonOptions = new()
    {
        WriteIndented = true
    };

    private bool _ready = false;

    [JsonConstructor]
    private RequestCollection() { }

    public RequestCollection(string path)
    {
        Id = Guid.NewGuid().ToString();
        Path = path;
        Name = "Unnamed Collection";

        // We add the path to the setting roots, so we can find the collection when we later open the app
        SettingsManager.Settings.TryAddExclusiveRoot(path);
        SettingsManager.Save();

        MarkReady();
        Save();
    }

    public void Save()
    {
        if (!_ready)
            return;

        Directory.CreateDirectory(Path);

        var collectionFilePath = System.IO.Path.Combine(Path, $"collection.{EXTENSION}");
        var json = JsonSerializer.Serialize(this, _saveJsonOptions);

        File.WriteAllText(collectionFilePath, json);
    }

    /// <summary>
    /// Only once marked ready will the request auto-save. This is to prevent saving an incomplete request while it's still being deserialized from disk.
    /// </summary>
    private void MarkReady()
    {
        _ready = true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        Save();
    }

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
