namespace Common;

public enum CheckStatus
{
    Inactive,
    Ignore,
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
}