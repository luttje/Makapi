using Makapi.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Makapi.Services;

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
            List<string> reqFiles;
            List<string> colFiles;

            if (Directory.Exists(root))
            {
                reqFiles = await Task.Run(() => SafeEnumerateFiles(root, $"*.{Request.EXTENSION}").ToList());
                colFiles = await Task.Run(() => SafeEnumerateFiles(root, $"*.{RequestCollection.EXTENSION}").ToList());
            }
            else
            {
                reqFiles = [];
                colFiles = [];
            }

            await LoadRequestsFromDirectoryAsync(root, reqFiles, colFiles);
        }
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directory, string pattern)
    {
        IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(directory, pattern);
        }
        catch (UnauthorizedAccessException) { yield break; }
        catch (IOException) { yield break; }

        foreach (var file in files)
            yield return file;

        IEnumerable<string> subdirs;
        try
        {
            subdirs = Directory.EnumerateDirectories(directory);
        }
        catch (UnauthorizedAccessException) { yield break; }
        catch (IOException) { yield break; }

        foreach (var subdir in subdirs)
        {
            foreach (var file in SafeEnumerateFiles(subdir, pattern))
                yield return file;
        }
    }

    private async Task LoadRequestsFromDirectoryAsync(
        string directory,
        List<string> requestFiles,
        List<string> collectionInfoFiles)
    {
        var collectionDirectories = new Dictionary<string, RequestCollection>();

        foreach (var collectionInfoFile in collectionInfoFiles)
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

    internal IEnumerable<Request> GetAllRequests()
    {
        foreach (var request in _rootRequests)
            yield return request;

        foreach (var collection in _collections)
        {
            foreach (var request in collection.Requests)
                yield return request;
        }
    }

    internal void ClearAll()
    {
        _rootRequests.Clear();
        _collections.Clear();
    }

    internal Request? GetRequestByFilePath(string filePath)
    {
        var req = _rootRequests.FirstOrDefault(r =>
            string.Equals(r.Path, filePath, StringComparison.OrdinalIgnoreCase));
        if (req != null) return req;

        foreach (var collection in _collections)
        {
            req = collection.Requests.FirstOrDefault(r =>
                string.Equals(r.Path, filePath, StringComparison.OrdinalIgnoreCase));
            if (req != null) return req;
        }

        return null;
    }

    internal RequestCollection? GetCollectionByDirectory(string directory)
    {
        return _collections.FirstOrDefault(c =>
            string.Equals(c.Path, directory, StringComparison.OrdinalIgnoreCase));
    }
}
