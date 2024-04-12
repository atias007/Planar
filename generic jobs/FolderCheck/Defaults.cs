using Common;

namespace FolderCheck;

internal class Defaults : BaseDefault, IFolder
{
    public Defaults()
    {
        RetryCount = 1;
        RetryInterval = TimeSpan.FromSeconds(10);
        MaximumFailsInRow = 5;
    }

    //// --------------------------------------- ////

    public static Defaults Empty => new();
}