using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public enum DataActions
    {
        [ActionEnumOption("put")]
        Put,

        [ActionEnumOption("remove")]
        Remove
    }
}