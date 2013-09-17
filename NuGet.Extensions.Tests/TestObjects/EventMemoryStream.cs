using System;
using System.Diagnostics;
using System.IO;

namespace NuGet.Extensions.Tests.TestObjects
{
    internal class EventMemoryStream : MemoryStream
    {
        private static Action<Stream> _closeAction;

        public EventMemoryStream(Action<Stream> closeAction)
        {
            Debug.Assert(closeAction != null);
            _closeAction = closeAction;
        }

        public override void Close()
        {
            _closeAction(this);
            base.Close();
        }
    }
}