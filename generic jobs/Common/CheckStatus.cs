namespace Common;

public enum CheckStatus
{
    Inactive,   // Active property = False
    Ignore,     // Not binding to current trigger
    Success,
    CheckWarning,
    CheckError,
    Exception
}

public static class CheckStatusExtentions
{
    public static bool IsValidStatus(this CheckStatus status)
    {
        return status == CheckStatus.Success ||
            status == CheckStatus.Inactive ||
            status == CheckStatus.Ignore ||
            status == CheckStatus.CheckWarning;
    }

    public static bool IsInvalidStatus(this CheckStatus status) => !status.IsValidStatus();
}