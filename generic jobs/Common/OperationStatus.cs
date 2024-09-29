namespace Common;

public enum OperationStatus
{
    Inactive,
    Ignore,
    Success,
    Exception
}

public static class OperationStatusExtentions
{
    public static bool IsValidStatus(this OperationStatus status)
    {
        return status == OperationStatus.Success ||
            status == OperationStatus.Inactive ||
            status == OperationStatus.Ignore;
    }
}