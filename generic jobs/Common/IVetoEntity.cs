namespace Common;

public interface IVetoEntity
{
    string Key { get; }
    bool Veto { get; }
    string? VetoReason { get; }
}