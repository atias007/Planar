namespace Common;

public interface INamedCheckElement : ICheckElement
{
    string Name { get; }
}