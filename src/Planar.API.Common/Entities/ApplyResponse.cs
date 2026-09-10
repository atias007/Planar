using System.Collections.Generic;

namespace Planar.API.Common.Entities;

public class ApplyResponse
{
    public int TotalUpdate { get; private set; }
    public int TotalAdd { get; private set; }
    public int TotalDelete { get; private set; }
    public int TotalUnchanged { get; private set; }
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
        }
    }

    public static ApplyResponse Merge(IEnumerable<ApplyResponse> responses>)
    {
        var result = new ApplyResponse();
        foreach (var response in responses)
        {
            result.TotalAdd += response.TotalAdd;
            result.TotalUpdate += response.TotalUpdate;
            result.TotalDelete += response.TotalDelete;
            result.TotalUnchanged += response.TotalUnchanged;
            result.Items.AddRange(response.Items);
        }
        return result;
    }
}

public class ApplyResponseItem(string key, ApplyAction action, string description, string? source = null)
{
    public string Key { get; private set; } = key;
    public string Action { get; private set; } = action.ToString();
    public int ActionId { get; private set; } = (int)action;
    public string Description { get; private set; } = description;
    public string? Source { get; private set; } = string.IsNullOrWhiteSpace(source) ? null : source;
}

public enum ApplyAction
{
    Add,
    Update,
    Delete,
    Unchanged
}