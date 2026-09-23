using System.Collections.Generic;
using System.Net;

namespace Planar.API.Common.Entities;

public class ApplyResponse
{
    public int TotalUpdate { get; private set; }
    public int TotalAdd { get; private set; }
    public int TotalDelete { get; private set; }
    public int TotalUnchanged { get; private set; }
    public int TotalErrors { get; private set; }
    public List<ApplyResponseItem> Items { get; private set; } = [];

    public void AddItem(ApplyResponseItem item)
    {
        Items.Add(item);
        switch (item.ActionId)
        {
            case (int)ApplyAction.Add:
                TotalAdd++;
                break;

            case (int)ApplyAction.Update:
                TotalUpdate++;
                break;

            case (int)ApplyAction.Delete:
                TotalDelete++;
                break;

            case (int)ApplyAction.Unchanged:
                TotalUnchanged++;
                break;

            case (int)ApplyAction.Error:
                TotalErrors++;
                break;

            default:
                break;
        }
    }

    public HttpStatusCode GetStatusCode()
    {
        if (TotalErrors == 0)
        {
            return HttpStatusCode.OK;
        }

        return HttpStatusCode.MultiStatus;
    }

    public static ApplyResponse Merge(IEnumerable<ApplyResponse> responses)
    {
        var result = new ApplyResponse();
        foreach (var response in responses)
        {
            result.TotalAdd += response.TotalAdd;
            result.TotalUpdate += response.TotalUpdate;
            result.TotalDelete += response.TotalDelete;
            result.TotalUnchanged += response.TotalUnchanged;
            result.TotalErrors += response.TotalErrors;
            result.Items.AddRange(response.Items);
        }
        return result;
    }
}

public class ApplyResponseItem(string key, ApplyAction action, string description, string kind, string? source)
{
    public string Key { get; private set; } = key;
    public string Action { get; private set; } = action.ToString();
    public int ActionId { get; private set; } = (int)action;
    public string Description { get; private set; } = description;
    public string Kind { get; private set; } = kind;
    public string? Source { get; private set; } = string.IsNullOrWhiteSpace(source) ? null : source;
}

public enum ApplyAction
{
    Add,
    Update,
    Delete,
    Unchanged,
    Skipped,
    Error = 99
}