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

        [SetUp]
        public void SetUp()
        {
            CloneCommand = new TestCloneCommand(Utilities.GetFactory(), Utilities.GetSourceProvider());
            CloneCommand.Console = new Mock<IConsole>().Object;
            CloneCommand.ApiKey = "apiKey";
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