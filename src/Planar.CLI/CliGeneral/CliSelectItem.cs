namespace Planar.CLI.CliGeneral;

public abstract class CliSelectItem
{
    public required string DisplayName { get; set; }
    public bool IsCancelItem { get; set; }
}

public sealed class CliSelectItem<T> : CliSelectItem
{
    private const string CancelOption = $"[{CliFormat.WarningColor}]<cancel>[/]";

    public T? Value { get; set; }

    public static CliSelectItem<T> CancelItem => new() { DisplayName = CancelOption, IsCancelItem = true };

    public override string ToString()
    {
        return DisplayName;
    }
}