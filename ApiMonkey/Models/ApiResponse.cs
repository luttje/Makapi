using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.Models;

internal class ApiResponse
{
    public string Body { get; private set; }
    public List<Header> Headers { get; private set; }

    internal ApiResponse(string body, List<Header> headers)
    {
        Body = body;
        Headers = headers;
    }
}
