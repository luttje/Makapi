using ApiMonkey.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApiMonkey.Models;

public interface ISearchResultText
{
    string DisplayText { get; set; }
}

public class SearchResult : ISearchResultText
{
    public Request Request { get; set; }
    public string DisplayText { get; set; }
    public int MatchScore { get; set; }

    public SearchResult(Request request, string displayText, int matchScore)
    {
        Request = request;
        DisplayText = displayText;
        MatchScore = matchScore;
    }

    public override string ToString() => DisplayText;
}

public class FakeSearchResult : ISearchResultText
{
    public string DisplayText { get; set; }
    public FakeSearchResult(string displayText)
    {
        DisplayText = displayText;
    }
    public override string ToString() => DisplayText;
}