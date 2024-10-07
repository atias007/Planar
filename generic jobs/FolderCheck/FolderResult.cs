namespace FolderCheck;

internal class FolderResult
{
    public long TotalSize { get; set; }
    public long FileSize { get; set; }
    public int FileCount { get; set; }
    public DateTime? CreatedAge { get; set; }
    public DateTime? ModifiedAge { get; set; }
}