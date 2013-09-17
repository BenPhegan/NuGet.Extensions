using NuGet.Common;
using NuGet.Extensions.Caches;
using NuGet.Extensions.Commands;
using Moq;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.TestObjects
{
    public class TestGetCommand : Get
    {
        readonly Mock<MockFileSystem> mockFileSystem;

        public TestGetCommand(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider, IPackageRepository cacheRepository, Mock<MockFileSystem> fileSystem)
            : base(cacheRepository, fileSystem.Object, new MemoryBasedPackageCache(new Mock<IConsole>().Object))
        {
            RepositoryFactory = packageRepositoryFactory;
            SourceProvider = sourceProvider;
            mockFileSystem = fileSystem;
        }

        protected override IFileSystem CreateFileSystem(string path)
        {
            mockFileSystem.Setup(m => m.Root).Returns(path);
            return mockFileSystem.Object;
        }
    }
}
