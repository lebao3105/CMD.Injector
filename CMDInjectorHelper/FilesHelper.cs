using System.Diagnostics;
using System.IO;

namespace CMDInjectorHelper
{
    public sealed class FilesHelper
    {
        public static string CurrentFolder { get; set; }

        #region Copies
        public static uint CopyFile(string source, string destination)
        {
            Debug.WriteLine($"Copying {source} to {destination}");
            return Globals.nrpc.FileCopy(source, destination, 0);
        }

        public static uint CopyFileToDir(string sourceDir, string fileName, string destinationDir, string newName = null)
        {
            return CopyFile($"{sourceDir}\\{fileName}", $"{destinationDir}\\{(newName == null ? fileName : newName)}");
        }

        public static uint CopyFileToDir(string fileName, string destinationDir)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(CurrentFolder), "Set FilesHelper.CurrentFolder first!");
            return CopyFile($"{CurrentFolder}\\{fileName}", $"{destinationDir}\\{fileName}");
        }

        /// <summary>
        /// Copies a file from the application's folder.
        /// </summary>
        /// <param name="path">The relative path to the file.</param>
        /// <param name="destinationDir">The destination folder.</param>
        public static uint CopyFromAppRoot(string path, string destination)
            => CopyFileToDir($"{Globals.installedLocation.Path}\\{path}", destination);

        /// <summary>
        /// Copies a file from the application's folder.
        /// </summary>
        /// <param name="path">The relative path to the folder CONTAINING the file. If it's null then <see cref="CurrentFolder"/> will be used.</param>
        /// <param name="fileName">The file to copy.</param>
        /// <param name="destinationDir">The destination folder.</param>
        /// <returns></returns>
        public static uint CopyFromAppRoot(string path, string fileName, string destinationDir)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.Assert(!string.IsNullOrWhiteSpace(CurrentFolder), "Set FilesHelper.CurrentFolder first!");
                return CopyFromAppRoot(CurrentFolder, fileName, destinationDir);
            }
            return CopyFileToDir($"{Globals.installedLocation.Path}\\{path}", fileName, destinationDir);
        }

        /// <summary>
        /// A lot of things from this project get copied to %SYSTEM32%. So why not?
        /// </summary>
        public static uint CopyToSystem32(string sourceDir, string fileName, string newName = null)
        {
            return CopyFileToDir(sourceDir, fileName, "C:\\Windows\\System32\\", newName);
        }

        public static uint CopyToSystem32(string fileName, string newName = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(CurrentFolder), "Set FilesHelper.CurrentFolder first!");
            return CopyToSystem32(CurrentFolder, fileName, newName);
        }

        public static uint CopyToSystem32FromAppRoot(string path, string fileName, string newName = null)
        {
            return CopyToSystem32($"{Globals.installedLocation.Path}\\{path}", fileName, newName);
        }

        public static uint CopyToSystem32FromAppRoot(string fileName, string newName = null)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(CurrentFolder), "Set FilesHelper.CurrentFolder first!");
            return CopyToSystem32FromAppRoot(CurrentFolder, fileName, newName);
        }
        #endregion
    }
}
