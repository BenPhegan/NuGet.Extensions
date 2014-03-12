using Moq;
using NuGet.Common;

namespace NuGet.Extensions.Tests.MSBuild
{
    public class ConsoleMock {
        public static void AssertConsoleHasNoErrorsOrWarnings(Mock<IConsole> console)
        {
            console.Verify(c => c.WriteError(It.IsAny<object>()), Times.Never());
            console.Verify(c => c.WriteError(It.IsAny<string>()), Times.Never());
            console.Verify(c => c.WriteError(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never());
            console.Verify(c => c.WriteWarning(It.IsAny<string>()), Times.Never());
            console.Verify(c => c.WriteWarning(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never());
            console.Verify(c => c.WriteWarning(It.IsAny<string>(), It.IsAny<object[]>()), Times.Never());
        }
    }
}