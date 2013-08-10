using NuGet.Common;
using NuGet.Extensions.Commands;
using Moq;
using NuGet.Extras.Caches;
using NuGet.Extras.Tests.TestObjects;

namespace NuGet.Extensions.Tests.TestObjects
{
    public class TestGetCommand : Get
    {
        Mock<MockFileSystem> mockFileSystem;

        public TestGetCommand(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider, IPackageRepository cacheRepository, Mock<MockFileSystem> fileSystem)
            : base(packageRepositoryFactory, sourceProvider, cacheRepository, fileSystem.Object, new MemoryBasedPackageCache(new Mock<IConsole>().Object)) 
        {
            mockFileSystem = fileSystem;
        }

        protected override IFileSystem CreateFileSystem(string path)
        {
            mockFileSystem.Setup(m => m.Root).Returns(path);
            return mockFileSystem.Object;
        }
    }
}
