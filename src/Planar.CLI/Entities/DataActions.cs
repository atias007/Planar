using Planar.CLI.Attributes;

namespace Planar.CLI.Entities
{
    public enum DataActions
    {
        [ActionEnumOption("upsert")]
        Upsert,

        [ActionEnumOption("remove")]
        Remove
    }
}