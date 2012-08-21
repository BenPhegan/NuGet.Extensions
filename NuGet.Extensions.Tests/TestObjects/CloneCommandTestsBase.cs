using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using NuGet.Common;
using NuGet.Extensions.Commands;
using NuGet.Extensions.Tests.Mocks;

namespace NuGet.Extensions.Tests.TestObjects
{
    public abstract class CloneCommandTestsBase
    {
        protected Clone CloneCommand;
        protected Copy CopyCommand;

        [SetUp]
        public void SetUp()
        {
            CloneCommand = new TestCloneCommand(Utilities.GetFactory(), Utilities.GetSourceProvider());
            CloneCommand.Console = new Mock<IConsole>().Object;
            var copy = new Mock<Copy>(Utilities.GetFactory(), Utilities.GetSourceProvider());
            copy.SetupGet(c => c.ApiKey).Returns("apiKey");
            CopyCommand = copy.Object;
            SetUpTest();         
        }

        protected abstract void SetUpTest();

        [Test]
        public void CommandIsNotNull()
        {
            Assert.IsNotNull(CloneCommand);
        }
    }
}