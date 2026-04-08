using ApiMonkey.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

    internal void LoadRequestsFromDisk()
    {
        var roots = _settingsManager.Settings.RequestRoots;

        foreach (var root in roots)
        {
            LoadRequestsFromDirectory(root);
        }
    }

    private void LoadRequestsFromDirectory(string directory)
    {
        if (!Directory.Exists(directory))
            return;

        var requestFiles = Directory.GetFiles(directory, $"*.{Request.EXTENSION}", SearchOption.AllDirectories);

        // Find collections, then load them
        var collectionInfos = Directory.GetFiles(directory, $"*.{RequestCollection.EXTENSION}", SearchOption.AllDirectories);
        var collectionDirectories = new Dictionary<string, RequestCollection>();

        foreach (var collectionInfoFile in collectionInfos)
        {
            var collectionDirectory = Path.GetDirectoryName(collectionInfoFile);

            var json = File.ReadAllText(collectionInfoFile);
            var collection = RequestCollection.FromJson(json, collectionDirectory);

            collectionDirectories.Add(collectionDirectory, collection);

            _collections.Add(collection);
            CollectionAdded?.Invoke(collection);
        }

        // Load requests afterwards, so they fill into their collections if needed
        foreach (var file in requestFiles)
        {
            // TODO: Implement version check and guard against corrupted files
            var json = File.ReadAllText(file);
            var request = Request.FromJson(json, file);
            var fileDirectory = Path.GetDirectoryName(file);

            if (collectionDirectories.TryGetValue(fileDirectory, out var collection))
            {
                request.Collection = collection;
            }

            _rootRequests.Add(request);
            RequestAdded?.Invoke(request);
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
