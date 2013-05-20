using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace NuGet.Extras.ExtensionMethods
{
    /// <summary>
    /// IFileSystem extensions.
    /// </summary>
    public static class IFileSystemExtensions
    {
        /// <summary>
        /// Gets files for a particular pattern recursively.
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <param name="filter"></param>
        /// <param name="option"> </param>
        /// <returns></returns>
        public static IEnumerable<string> GetFiles(this IFileSystem fileSystem, string path, string filter, SearchOption option)
        {
            if (option == SearchOption.TopDirectoryOnly)
            {
                return fileSystem.GetFiles(path, filter);
            }
            
            return fileSystem.GetFilesRecursive(path, filter);
        }

        private static IEnumerable<string> GetFilesRecursive(this IFileSystem fileSystem, string path, string filter)
        {
            var files = new List<string>();
            files.AddRange(fileSystem.GetFiles(path, filter));

            foreach (var subDir in fileSystem.GetDirectories(path))
            {
                files.AddRange(fileSystem.GetFilesRecursive(subDir, filter));
            }
            return files.Distinct();
        }

        /// <summary>
        /// Gets directories under a specified path.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetDirectories(string path)
        {
            foreach (var index in IndexOfAll(path, Path.DirectorySeparatorChar))
            {
                yield return path.Substring(0, index);
            }
            yield return path;
        }

        private static IEnumerable<int> IndexOfAll(string value, char ch)
        {
            int index = -1;
            do
            {
                index = value.IndexOf(ch, index + 1);
                if (index >= 0)
                {
                    yield return index;
                }
            }
            while (index >= 0);
        }

        /// <summary>
        /// Adds a file via a Func returning a Stream
        /// </summary>
        /// <param name="fileSystem"></param>
        /// <param name="path"></param>
        /// <param name="write"></param>
        public static void AddFile(this IFileSystem fileSystem, string path, Action<Stream> write)
        {
            using (var stream = new MemoryStream())
            {
                write(stream);
                stream.Seek(0, SeekOrigin.Begin);
                fileSystem.AddFile(path, stream);
            }
        }
    }
}
