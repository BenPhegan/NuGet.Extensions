using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NuGet.Extensions.ExtensionMethods;

namespace NuGet.Extensions.Tests.Mocks
{
    public class MockFileSystem : IFileSystem
    {
        private ILogger _logger;

        public MockFileSystem()
            : this(@"C:\MockFileSystem\")
        {

        }

        public MockFileSystem(string root)
        {
            Root = root;
            Paths = new Dictionary<string, Func<Stream>>(StringComparer.OrdinalIgnoreCase);
            Deleted = new HashSet<string>();
        }

        public DateTimeOffset GetLastAccessed(string path)
        {
            throw new NotImplementedException();
        }

        public virtual ILogger Logger
        {
            get
            {
                return _logger ?? NullLogger.Instance;
            }
            set
            {
                _logger = value;
            }
        }

        public virtual string Root
        {
            get;
            private set;
        }

        public virtual IDictionary<string, Func<Stream>> Paths
        {
            get;
            private set;
        }

        public virtual HashSet<string> Deleted
        {
            get;
            private set;
        }

        public virtual void CreateDirectory(string path)
        {
            Paths.Add(path, null);
        }

        public virtual void DeleteDirectory(string path, bool recursive = false)
        {
            foreach (var file in Paths.Keys.ToList())
            {
                if (file.StartsWith(path))
                {
                    Paths.Remove(file);
                }
            }
            Deleted.Add(path);
        }

        public virtual IEnumerable<string> GetFiles(string path, bool recursive)
        {
            var files = Paths.Select(f => f.Key);
            if (recursive)
            {
                path = PathUtility.EnsureTrailingSlash(path);
                files = files.Where(f => f.StartsWith(path, StringComparison.OrdinalIgnoreCase));
            }
            else
            {
                files = files.Where(f => Path.GetDirectoryName(f).Equals(path, StringComparison.OrdinalIgnoreCase));
            }

            return files;
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter, bool recursive)
        {
            if (String.IsNullOrEmpty(filter) || filter == "*.*")
            {
                filter = "*";
            }

            // TODO: This is just flaky. We need to make it closer to the implementation that Directory.Enumerate supports perhaps by using PathResolver.
            var files = GetFiles(path, recursive);
            if (!filter.Contains("*"))
            {
                return files.Where(f => f.Equals(Path.Combine(path, filter), StringComparison.OrdinalIgnoreCase));
            }

            Regex matcher = GetFilterRegex(filter);
            return files.Where(f => matcher.IsMatch(f));
        }

        public virtual string GetFullPath(string path)
        {
            return Path.Combine(Root, path);
        }

        public virtual IEnumerable<string> GetFiles(string path)
        {
            return Paths.Select(f => f.Key)
                        .Where(f => Path.GetDirectoryName(f).Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public virtual IEnumerable<string> GetFiles(string path, string filter)
        {
            Regex matcher = FindFilesPatternToRegex.Convert(filter);

            return GetFiles(path).Where(f => matcher.IsMatch(Path.GetFileName(f)));
        }

        private static Regex GetFilterRegex(string wildcard)
        {
            string pattern = String.Join(String.Empty, wildcard.Split('.').Select(GetPattern));
            return new Regex(pattern, RegexOptions.IgnoreCase);
        }

        private static string GetPattern(string token)
        {
            return token == "*" ? @"(.*)" : @"(" + token + ")";
        }

        public virtual void DeleteFile(string path)
        {
            Paths.Remove(path);
            Deleted.Add(path);
        }

        public virtual bool FileExists(string path)
        {
            return Paths.ContainsKey(path);
        }

        public virtual Stream OpenFile(string path)
        {
            Func<Stream> factory;
            if (!Paths.TryGetValue(path, out factory))
            {
                throw new FileNotFoundException(path + " not found.");
            }
            return factory();
        }

        public string ReadAllText(string path)
        {
            return OpenFile(path).ReadToEnd();
        }

        public virtual bool DirectoryExists(string path)
        {
            return Paths.Select(file => file.Key)
                        .Any(file => Path.GetDirectoryName(file).Equals(path, StringComparison.OrdinalIgnoreCase));
        }

        public virtual IEnumerable<string> GetDirectories(string path)
        {
            return Paths.GroupBy(f => Path.GetDirectoryName(f.Key))
                        .SelectMany(g => IFileSystemExtensions.GetDirectories(g.Key))
                        .Where(f => Path.GetDirectoryName(f) != null && !String.IsNullOrEmpty(f) &&
                               Path.GetDirectoryName(f).Equals(path, StringComparison.OrdinalIgnoreCase))
                        .Distinct();
        }

        public virtual void AddFile(string path)
        {
            AddFile(path, new MemoryStream());
        }

        public void AddFile(string path, string content)
        {
            AddFile(path, content.AsStream());
        }

        public virtual void AddFile(string path, Stream stream, bool overrideIfExists)
        {
            var ms = new MemoryStream((int)stream.Length);
            stream.CopyTo(ms);
            byte[] buffer = ms.ToArray();
            Paths[GetFullPath(path)] = () => new MemoryStream(buffer);
        }

        public virtual void AddFile(string path, Stream stream)
        {
            AddFile(path, stream, overrideIfExists: true);
        }

        public virtual void AddFile(string path, Func<Stream> getStream)
        {
            Paths[path] = getStream;
        }

        public virtual void AddFile(string path, Action<Stream> getStream)
        {
            AddFile(path, getStream.Target.ToString());
        }

        public virtual Stream CreateFile(string path)
        {
            return new MemoryStream();
        }

        public virtual DateTimeOffset GetLastModified(string path)
        {
            return DateTime.UtcNow;
        }

        public virtual DateTimeOffset GetCreated(string path)
        {
            return DateTime.UtcNow;
        }

        public virtual void MakeFileWritable(string path)
        {
            throw new NotImplementedException();
        }
    }
}
