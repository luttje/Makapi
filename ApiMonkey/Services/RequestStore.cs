using ApiMonkey.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiMonkey.Services;

public delegate void RequestEventHandler(Request request);
public delegate void CollectionEventHandler(RequestCollection collection);

public class RequestStore
{
    public event RequestEventHandler? RequestAdded;
    public event RequestEventHandler? RequestRemoved;
    public event CollectionEventHandler? CollectionAdded;
    public event CollectionEventHandler? CollectionRemoved;

    private readonly List<Request> _rootRequests = [];
    private readonly List<RequestCollection> _collections = [];
    private readonly SettingsManager _settingsManager;

    public RequestStore(SettingsManager settingsManager)
    {
        _settingsManager = settingsManager;
    }

    internal async Task LoadRequestsFromDiskAsync()
    {
        var roots = _settingsManager.Settings.RequestRoots;

        foreach (var root in roots)
        {
            await LoadRequestsFromDirectoryAsync(root);
        }
    }

    private async Task LoadRequestsFromDirectoryAsync(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        var requestFiles = Directory.GetFiles(directory, $"*.{Request.EXTENSION}", SearchOption.AllDirectories);

        var collectionInfos = Directory.GetFiles(directory, $"*.{RequestCollection.EXTENSION}", SearchOption.AllDirectories);
        var collectionDirectories = new Dictionary<string, RequestCollection>();

        foreach (var collectionInfoFile in collectionInfos)
        {
            try
            {
                var collectionDirectory = Path.GetDirectoryName(collectionInfoFile);

                var json = await File.ReadAllTextAsync(collectionInfoFile);
                var collection = RequestCollection.FromJson(json, collectionDirectory);

                collectionDirectories.Add(collectionDirectory, collection);

                _collections.Add(collection);
                CollectionAdded?.Invoke(collection);
            }
            // TODO: Find out how to report these to the user, maybe with a log file or something
            catch (OperationCanceledException) { }
            catch (JsonException) { }
        }

        foreach (var file in requestFiles)
        {
            try
            {
                var json = await File.ReadAllTextAsync(file);
                var request = Request.FromJson(json, file);
                var fileDirectory = Path.GetDirectoryName(file);

                if (collectionDirectories.TryGetValue(fileDirectory, out var collection))
                {
                    request.Collection = collection;
                }

                _rootRequests.Add(request);
                RequestAdded?.Invoke(request);
            }
            // TODO: Find out how to report these to the user, maybe with a log file or something
            catch (OperationCanceledException) { }
            catch (JsonException) { }
        }
    }

    internal Request CreateRequest(RequestCollection? collection = null)
    {
        var request = new Request(collection, _settingsManager.GetDefaultRequestsPath());

        if (collection != null)
            collection.Requests.Add(request);
        else
            _rootRequests.Add(request);

        RequestAdded?.Invoke(request);

        return request;
    }

    internal RequestCollection CreateCollection(string path)
    {
        var collection = new RequestCollection(path, _settingsManager);

        _collections.Add(collection);

        CollectionAdded?.Invoke(collection);

        return collection;
    }

    internal Request GetRequestById(string requestId)
    {
        var request = _rootRequests.FirstOrDefault(r => r.Id == requestId);

        if (request == null)
        {
            foreach (var collection in _collections)
            {
                request = collection.Requests.FirstOrDefault(r => r.Id == requestId);

                if (request != null)
                    break;
            }
        }

        if (request == null)
            throw new InvalidOperationException("Request not found.");

        return request;
    }

    internal RequestCollection GetCollectionById(string collectionId)
    {
        var collection = _collections.FirstOrDefault(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found.");

        return collection;
    }

    internal void DeleteRequest(string id)
    {
        var request = _rootRequests.FirstOrDefault(r => r.Id == id);

        if (request == null)
        {
            foreach (var collection in _collections)
            {
                request = collection.Requests.FirstOrDefault(r => r.Id == id);
                if (request != null)
                    break;
            }
        }

        if (request == null)
        {
            throw new InvalidOperationException("Request not found.");
        }

        request.Delete();

        if (request.Collection != null)
        {
            request.Collection.Requests.Remove(request);
        }
        else
        {
            _rootRequests.Remove(request);
        }

        RequestRemoved?.Invoke(request);
    }

    internal void DeleteCollection(string id)
    {
        var collection = _collections.FirstOrDefault(c => c.Id == id);

        if (collection == null)
        {
            throw new InvalidOperationException("Collection not found.");
        }

        collection.Delete();

        _collections.Remove(collection);

        CollectionRemoved?.Invoke(collection);
    }
}
