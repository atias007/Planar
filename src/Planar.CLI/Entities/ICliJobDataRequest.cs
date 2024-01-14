namespace Planar.CLI.Entities
{
    public interface ICliDataRequest
    {
        DataActions? Action { get; set; }
        string DataKey { get; set; }
        string? DataValue { get; set; }
    }
}