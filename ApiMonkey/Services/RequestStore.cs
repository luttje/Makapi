using ApiMonkey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.Services;

internal delegate void RequestEeventHandler(Request request);
internal delegate void CollectionEventHandler(RequestCollection collection);

internal class RequestStore
{
    public event RequestEeventHandler? RequestAdded;
    public event RequestEeventHandler? RequestRemoved;
    public event CollectionEventHandler? CollectionAdded;
    public event CollectionEventHandler? CollectionRemoved;

    private readonly List<Request> _rootRequests = [];
    private readonly List<RequestCollection> _collections = [];

    private static RequestStore? _instance;
    public static RequestStore Instance
    {
        get
        {
            if (_instance == null)
                _instance = new RequestStore();

            return _instance;
        }
    }

    internal Request CreateRequest(RequestCollection? collection = null)
    {
        var request = new Request(collection);

        if (collection != null)
            collection.Requests.Add(request);
        else
            _rootRequests.Add(request);

        RequestAdded?.Invoke(request);

        return request;
    }

    internal RequestCollection CreateCollection()
    {
        var collection = new RequestCollection();

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

        _collections.Remove(collection);

        CollectionRemoved?.Invoke(collection);
    }
}
