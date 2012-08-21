using NuGet.Common;
using NuGet.Extensions.Commands;
using Moq;
using NuGet.Extensions.Tests.Mocks;
using NuGet.Extras.Caches;

namespace NuGet.Extensions.Tests.TestObjects
{
    public class TestCopyCommand : Copy
    {
        public TestCopyCommand(IPackageRepositoryFactory packageRepositoryFactory, IPackageSourceProvider sourceProvider)
            : base(packageRepositoryFactory, sourceProvider) 
        {
        }
    }
}
