using Microsoft.Extensions.Logging;
using System.Collections;
using System.Text.RegularExpressions;

namespace BlinkSyncLib;

public enum OperationType
{
    CreateDirectory,
    CopyFile,
    DeleteFile,
    DeleteDirectory
}

public class OperationEventArgs : EventArgs
{
    public required OperationType OperationType { get; set; }
    public required string Name { get; set; }
}

/// <summary>
/// Folders and files synchronization
/// </summary>
public class Sync
{
    public event EventHandler<OperationEventArgs>? Operation;

    public Sync(InputParams parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);
        Configuration = parameters;
        SourceDirectory = new DirectoryInfo(parameters.SourceDirectory);
        DestinationDirectory = new DirectoryInfo(parameters.DestinationDirectory);
    }

    private void OnOperation(OperationType operationType, string name)
    {
        Operation?.Invoke(this, new OperationEventArgs { OperationType = operationType, Name = name });
    }

    #region PROPERTIES

    /// <summary>
    /// Get or set the source folder to synchronize
    /// </summary>
    public virtual DirectoryInfo SourceDirectory { get; private set; }

    /// <summary>
    /// Get or set the target folder where all files will be synchronized
    /// </summary>
    public virtual DirectoryInfo DestinationDirectory { get; private set; }

    /// <summary>
    /// Get or set all synronization parameters
    /// </summary>
    public InputParams Configuration { get; private set; }

    #endregion PROPERTIES

    #region METHODS

    /// <summary>
    /// Performs one-way synchronization from source directory tree to destination directory tree
    /// </summary>
    public virtual SyncResults Start()
    {
        SyncResults results = new SyncResults();

        Validate(this.SourceDirectory.FullName, this.DestinationDirectory.FullName, this.Configuration);

        // recursively process directories
        ProcessDirectory(this.SourceDirectory.FullName, this.DestinationDirectory.FullName, this.Configuration, ref results);

        return results;
    }

    /// <summary>
    /// Performs one-way synchronization from source directory tree to destination directory tree
    /// </summary>
    /// <param name="configuration"></param>
    public virtual SyncResults Start(InputParams configuration)
    {
        this.Configuration = configuration;
        return this.Start();
    }

    /// <summary>
    /// Robustly deletes a directory including all subdirectories and contents
    /// </summary>
    /// <param name="directory"></param>
    public virtual void DeleteDirectory(DirectoryInfo directory)
    {
        // make sure all files are not read-only
        FileInfo[] files = directory.GetFiles("*.*", SearchOption.AllDirectories);
        foreach (FileInfo fileInfo in files)
        {
            if (fileInfo.IsReadOnly)
            {
                fileInfo.IsReadOnly = false;
            }
        }

        // make sure all subdirectories are not read-only
        DirectoryInfo[] directories = directory.GetDirectories("*.*", SearchOption.AllDirectories);
        foreach (DirectoryInfo subdir in directories)
        {
            if ((subdir.Attributes & FileAttributes.ReadOnly) > 0)
            {
                subdir.Attributes &= ~FileAttributes.ReadOnly;
            }
        }

        // make sure top level directory is not read-only
        if ((directory.Attributes & FileAttributes.ReadOnly) > 0)
        {
            directory.Attributes &= ~FileAttributes.ReadOnly;
        }

        directory.Delete(true);
    }

    /// <summary>
    /// Gets list of files in specified directory, optionally filtered by specified input parameters
    /// </summary>
    /// <param name="directoryInfo"></param>
    /// <param name="inputParams"></param>
    /// <param name="results"></param>
    public virtual FileInfo[] GetFiles(DirectoryInfo directoryInfo, InputParams? inputParams, ref SyncResults results)
    {
        // get all files
        List<FileInfo> fileList = new List<FileInfo>(directoryInfo.GetFiles());

        // do we need to do any filtering?
        if (inputParams == null) { return fileList.ToArray(); }
        bool needFilter = inputParams.AreSourceFilesFiltered;

        if (needFilter)
        {
            for (int i = 0; i < fileList.Count; i++)
            {
                FileInfo fileInfo = fileList[i];

                // filter out any files based on hiddenness and exclude/include filespecs
                if ((inputParams.ExcludeHidden && ((fileInfo.Attributes & FileAttributes.Hidden) > 0)) ||
                     ShouldExclude(inputParams.ExcludeFiles, inputParams.IncludeFiles, fileInfo.Name))
                {
                    fileList.RemoveAt(i);
                    results.FilesIgnored++;
#pragma warning disable S127 // "for" loop stop conditions should be invariant
                    i--;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
                }
            }
        }

        return fileList.ToArray();
    }

    /// <summary>
    /// Gets list of subdirectories of specified directory, optionally filtered by specified input parameters
    /// </summary>
    /// <param name="results"></param>
    /// <param name="inputParams"></param>
    /// <param name="directoryInfo"></param>
    public virtual DirectoryInfo[] GetDirectories(DirectoryInfo directoryInfo, InputParams? inputParams, ref SyncResults results)
    {
        // get all directories
        List<DirectoryInfo> directoryList = new List<DirectoryInfo>(directoryInfo.GetDirectories());

        // do we need to do any filtering?
        if (inputParams == null) { return directoryList.ToArray(); }
        bool needFilter = inputParams.AreSourceFilesFiltered;
        if (needFilter)
        {
            for (int i = 0; i < directoryList.Count; i++)
            {
                DirectoryInfo subdirInfo = directoryList[i];

                // filter out directories based on hiddenness and exclude/include filespecs
                if ((inputParams.ExcludeHidden && ((subdirInfo.Attributes & FileAttributes.Hidden) > 0)) ||
                     ShouldExclude(inputParams.ExcludeDirs, inputParams.IncludeDirs, subdirInfo.Name))
                {
                    directoryList.RemoveAt(i);
                    results.DirectoriesIgnored++;
#pragma warning disable S127 // "for" loop stop conditions should be invariant
                    i--;
#pragma warning restore S127 // "for" loop stop conditions should be invariant
                }
            }
        }

        return directoryList.ToArray();
    }

    #endregion METHODS

    #region PRIVATES

    /// <summary>
    /// Validate folder and parameters
    /// </summary>
    /// <param name="destDir"></param>
    /// <param name="parameters"></param>
    /// <param name="srcDir"></param>
    private static void Validate(string srcDir, string destDir, InputParams parameters)
    {
        if (parameters.IncludeFiles != null && parameters.ExcludeFiles != null)
        {
            throw new InvalidDataException($"operation must have only one of the following: include files, exclude files. current operation contains both");
        }

        if (parameters.IncludeDirs != null && parameters.ExcludeDirs != null)
        {
            throw new InvalidDataException($"operation must have only one of the following: include dirs, exclude dirs. current operation contains both");
        }

        string fullSrcDir = Path.GetFullPath(srcDir) + Path.PathSeparator;
        string fullDestDir = Path.GetFullPath(destDir) + Path.PathSeparator;
        if (destDir.StartsWith(fullSrcDir) || srcDir.StartsWith(fullDestDir))
        {
            throw new InvalidDataException($"source directory {fullSrcDir} and destination directory {fullDestDir} cannot contain each other");
        }

        if (((parameters.DeleteExcludeFiles != null) || (parameters.DeleteExcludeDirs != null)) &&
            (!parameters.DeleteFromDest))
        {
            throw new InvalidDataException("exclude from deletion options require deletion enabled");
        }

        // ensure source directory exists
        if (!Directory.Exists(srcDir))
        {
            throw new InvalidDataException($"source directory {srcDir} not found");
        }
    }

    /// <summary>
    /// Recursively performs one-way synchronization from a single source to destination directory
    /// </summary>
    /// <param name="srcDir"></param>
    /// <param name="destDir"></param>
    /// <param name="inputParams"></param>
    /// <param name="results"></param>
    private bool ProcessDirectory(string srcDir, string destDir, InputParams inputParams, ref SyncResults results)
    {
        DirectoryInfo diSrc = new DirectoryInfo(srcDir);
        DirectoryInfo diDest = new DirectoryInfo(destDir);

        // create destination directory if it doesn't exist
        if (!diDest.Exists)
        {
            try
            {
                Trace(LogLevel.Information, "creating directory: {0}", diDest.FullName);

                // create the destination directory
                diDest.Create();
                OnOperation(OperationType.CreateDirectory, diDest.FullName);
                results.DirectoriesCreated++;
            }
            catch (Exception ex)
            {
                Trace(LogLevel.Error, "failed to create directory {0}. {1}", destDir, ex.Message);
                results.TotalErrors++;
                return false;
            }
        }

        // get list of selected files from source directory
        FileInfo[] fiSrc = GetFiles(diSrc, inputParams, ref results);

        // get list of files in destination directory
        FileInfo[] fiDest = GetFiles(diDest, null, ref results);

        // put the source files and destination files into hash tables
        Dictionary<string, FileInfo> hashSrc = new(fiSrc.Length);
        foreach (FileInfo srcFile in fiSrc)
        {
            hashSrc.Add(srcFile.Name, srcFile);
        }

        Dictionary<string, FileInfo> hashDest = new(fiDest.Length);
        foreach (FileInfo destFile in fiDest)
        {
            hashDest.Add(destFile.Name, destFile);
        }

        // make sure all the selected source files exist in destination
        foreach (FileInfo srcFile in fiSrc)
        {
            bool isUpToDate = false;

            // look up in hash table to see if file exists in destination
            hashDest.TryGetValue(srcFile.Name, out FileInfo? destFile);

            // if file exists and length, write time and attributes match, it's up to date
            if ((destFile != null) && (srcFile.Length == destFile.Length) &&
                (srcFile.LastWriteTime == destFile.LastWriteTime) &&
                (srcFile.Attributes == destFile.Attributes))
            {
                isUpToDate = true;
                results.FilesUpToDate++;
            }

            // if the file doesn't exist or is different, copy the source file to destination
            if (!isUpToDate)
            {
                string destPath = Path.Combine(destDir, srcFile.Name);

                // make sure destination is not read-only
                if (destFile != null && destFile.IsReadOnly)
                {
                    destFile.IsReadOnly = false;
                }

                try
                {
                    Trace(LogLevel.Information, "copying: {0} -> {1}", srcFile.FullName, Path.GetFullPath(destPath));

                    // copy the file
                    srcFile.CopyTo(destPath, true);

                    // set attributes appropriately
                    File.SetAttributes(destPath, srcFile.Attributes);
                    results.FilesCopied++;
                    OnOperation(OperationType.CopyFile, srcFile.FullName);
                }
                catch (Exception ex)
                {
                    Trace(LogLevel.Error, "failed to copy file from {0} to {1}. {2}", srcFile.FullName, destPath, ex.Message);
                    results.TotalErrors++;
                    return false;
                }
            }
        }

        // delete extra files in destination directory if specified
        if (inputParams.DeleteFromDest)
        {
            foreach (FileInfo destFile in fiDest)
            {
                hashSrc.TryGetValue(destFile.Name, out FileInfo? srcFile);
                if (srcFile == null)
                {
                    // if this file is specified in exclude-from-deletion list, don't delete it
                    if (ShouldExclude(inputParams.DeleteExcludeFiles, null, destFile.Name)) { continue; }

                    try
                    {
                        Trace(LogLevel.Information, "deleting: {0} ", destFile.FullName);

                        destFile.IsReadOnly = false;

                        // delete the file
                        destFile.Delete();
                        results.FilesDeleted++;
                        OnOperation(OperationType.DeleteFile, destFile.FullName);
                    }
                    catch (Exception ex)
                    {
                        Trace(LogLevel.Error, "failed to delete file from {0}. {1}", destFile.FullName, ex.Message);
                        results.TotalErrors++;
                        return false;
                    }
                }
            }
        }

        // Get list of selected subdirectories in source directory
        DirectoryInfo[] diSrcSubdirs = GetDirectories(diSrc, inputParams, ref results);

        // Get list of subdirectories in destination directory
        DirectoryInfo[] diDestSubdirs = GetDirectories(diDest, null, ref results);

        // add selected source subdirectories to hash table, and recursively process them
        Hashtable hashSrcSubdirs = new Hashtable(diSrcSubdirs.Length);
        foreach (DirectoryInfo diSrcSubdir in diSrcSubdirs)
        {
            hashSrcSubdirs.Add(diSrcSubdir.Name, diSrcSubdir);

            // recurse into this directory
            if (!ProcessDirectory(diSrcSubdir.FullName, Path.Combine(destDir, diSrcSubdir.Name), inputParams, ref results))
            {
                return false;
            }
        }

        // delete extra directories in destination if specified
        if (inputParams.DeleteFromDest)
        {
            foreach (DirectoryInfo diDestSubdir in diDestSubdirs)
            {
                // does this destination subdirectory exist in the source subdirs?
                if (!hashSrcSubdirs.ContainsKey(diDestSubdir.Name))
                {
                    // if this directory is specified in exclude-from-deletion list, don't delete it
                    if (ShouldExclude(inputParams.DeleteExcludeDirs, null, diDestSubdir.Name))
                    {
                        continue;
                    }

                    try
                    {
                        Trace(LogLevel.Information, "deleting directory: {0} ", diDestSubdir.FullName);

                        // delete directory
                        DeleteDirectory(diDestSubdir);
                        results.DirectoriesDeleted++;
                        OnOperation(OperationType.DeleteDirectory, diDestSubdir.FullName);
                    }
                    catch (Exception ex)
                    {
                        Trace(LogLevel.Error, "failed to delete directory {0}. {1}", diDestSubdir.FullName, ex.Message);
                        results.TotalErrors++;
                        return false;
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// For a given include and exclude list of regex's and a name to match, determines if the
    /// named item should be excluded
    /// </summary>
    /// <param name="excludeList"></param>
    /// <param name="includeList"></param>
    /// <param name="name"></param>
    private static bool ShouldExclude(Regex[]? excludeList, Regex[]? includeList, string name)
    {
        if (excludeList != null)
        {
            // check against regex's in our exclude list
            foreach (Regex regex in excludeList)
            {
                if (regex.Match(name).Success)
                {
                    // if the name matches an entry in the exclude list, we SHOULD exclude it
                    return true;
                }
            }

            // no matches in exclude list, we should NOT exclude it
            return false;
        }
        else if (includeList != null)
        {
            foreach (Regex regex in includeList)
            {
                if (regex.Match(name).Success)
                {
                    // if the name matches an entry in the include list, we should NOT exclude it
                    return false;
                }
            }

            // no matches in include list, we SHOULD exclude it
            return true;
        }

        return false;
    }

    /// <summary>
    /// Trace message
    /// </summary>
    /// <param name="message"></param>
    /// <param name="args"></param>
    private void Trace(LogLevel logLevel, string message, params object[] args)
    {
        if (Configuration.Logger == null) { return; }
        if (!Configuration.LogIoInformation && logLevel == LogLevel.Information) { return; }

#pragma warning disable CA2254 // Template should be a static expression
        Configuration.Logger.Log(logLevel, message, args);
#pragma warning restore CA2254 // Template should be a static expression

        if (Configuration.StopAtFirstError && logLevel == LogLevel.Error)
        {
            throw new InvalidOperationException(String.Format(message, args));
        }
    }

    #endregion PRIVATES
}

public class InputParams
{
    public required string SourceDirectory { get; set; }

    public required string DestinationDirectory { get; set; }

    public ILogger? Logger { get; set; }

    /// <summary>
    /// Should exclude hidden files/directories in source
    /// </summary>
    public bool ExcludeHidden { get; set; }

    /// <summary>
    /// Should delete files/directories from dest than are not present in source
    /// </summary>
    public bool DeleteFromDest { get; set; }

    public bool LogIoInformation { get; set; }

    public bool StopAtFirstError { get; set; }

    /// <summary>
    /// List of filespecs to exclude
    /// </summary>
    public Regex[]? ExcludeFiles { get; set; }

    /// <summary>
    /// List of directory specs to exclude
    /// </summary>
    public Regex[]? ExcludeDirs { get; set; }

    /// <summary>
    /// List of filespecs to include
    /// </summary>
    public Regex[]? IncludeFiles { get; set; }

    /// <summary>
    /// List of directory specs to include
    /// </summary>
    public Regex[]? IncludeDirs { get; set; }

    /// <summary>
    /// List of filespecs NOT to delete from dest
    /// </summary>
    public Regex[]? DeleteExcludeFiles { get; set; }

    /// <summary>
    /// List of directory specs NOT to delete from dest
    /// </summary>
    public Regex[]? DeleteExcludeDirs { get; set; }

    public bool AreSourceFilesFiltered
    {
        get
        {
            return ExcludeHidden || (IncludeFiles != null) || (ExcludeFiles != null) ||
                (IncludeDirs != null) || (ExcludeDirs != null);
        }
    }
}

public class SyncResults
{
    /// <summary>
    /// Get or set the number of files copied.
    /// </summary>
    public int FilesCopied { get; set; }

    /// <summary>
    /// Get or set the number of files already up to date.
    /// </summary>
    public int FilesUpToDate { get; set; }

    /// <summary>
    /// Get or set the number of files deleted.
    /// </summary>
    public int FilesDeleted { get; set; }

    /// <summary>
    /// Get or set the number of files not synchronized.
    /// </summary>
    public int FilesIgnored { get; set; }

    /// <summary>
    /// Get or set the number of new folders created.
    /// </summary>
    public int DirectoriesCreated { get; set; }

    /// <summary>
    /// Get or set the number of folders removed.
    /// </summary>
    public int DirectoriesDeleted { get; set; }

    /// <summary>
    /// Get or set the number of folder not synchronized and ignored.
    /// </summary>
    public int DirectoriesIgnored { get; set; }

    public int TotalErrors { get; set; }
}