using System.Collections.Generic;

namespace ApiMonkey.Models;

public class ApiResponse
{
    public string Body { get; private set; }
    public List<Header> Headers { get; private set; }

    public ApiResponse(string body, List<Header> headers)
    {
        Body = body;
        Headers = headers;
    }
}
