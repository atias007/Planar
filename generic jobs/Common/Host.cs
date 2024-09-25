namespace Common;

public class Host : IVetoEntity
{
    public Host(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string Key => Name;

    public bool Veto { get; set; }

    public string? VetoReason { get; set; }
}