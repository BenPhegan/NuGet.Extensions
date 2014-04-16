using Moq;
using NuGet.Common;

namespace NuGet.Extensions.Tests.MSBuild
{
    public class ConsoleMock {
        private readonly Mock<IConsole> _console;

        public ConsoleMock()
        {
            _console = new Mock<IConsole>();
            _console.Setup(c => c.WriteError(It.IsAny<string>(), It.IsAny<object[]>())).Callback(() =>_console.Object.WriteError((object) null));
            _console.Setup(c => c.WriteError(It.IsAny<string>())).Callback(() => _console.Object.WriteError((object) null));
            _console.Setup(c => c.WriteWarning(It.IsAny<string>(), It.IsAny<object[]>())).Callback(() => _console.Object.WriteWarning("warning"));
            _console.Setup(c => c.WriteWarning(It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<object[]>())).Callback(() => _console.Object.WriteWarning("warning"));
            _console.Setup(c => c.WriteWarning(It.IsAny<bool>(), It.IsAny<string>())).Callback(() => _console.Object.WriteWarning("warning"));
        }

        public IConsole Object { get { return _console.Object; } }

        public void AssertConsoleHasNoErrorsOrWarnings()
        {
            _console.Verify(c => c.WriteError(It.IsAny<object>()), Times.Never());
            _console.Verify(c => c.WriteWarning(It.IsAny<string>()), Times.Never());
        }

        public void AssertConsoleHasErrors()
        {
            _console.Verify(c => c.WriteError(It.IsAny<object>()), Times.AtLeastOnce());
        }
    }
}