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
}

public class ApplyResponseItem(string key, ApplyAction action, string description)
{
    public string Key { get; private set; } = key;
    public string Action { get; private set; } = action.ToString();
    public int ActionId { get; private set; } = (int)action;
    public string Description { get; private set; } = description;
}

public enum ApplyAction
{
    Add,
    Update,
    Delete,
    Unchanged
}