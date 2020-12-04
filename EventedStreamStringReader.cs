using System;
using System.Collections.Generic;
using System.Text;

namespace Geexbox.FrontendClient
{
    public class EventedStreamStringReader : IDisposable
    {
        private EventedStreamReader _eventedStreamReader;
        private bool _isDisposed;
        private StringBuilder _stringBuilder = new StringBuilder();

        public EventedStreamStringReader(EventedStreamReader eventedStreamReader)
        {
            this._eventedStreamReader = eventedStreamReader ?? throw new ArgumentNullException(nameof(eventedStreamReader));
            this._eventedStreamReader.OnReceivedLine += new EventedStreamReader.OnReceivedLineHandler(this.OnReceivedLine);
        }

        public string ReadAsString() => this._stringBuilder.ToString();

        private void OnReceivedLine(string line) => this._stringBuilder.AppendLine(line);

        public void Dispose()
        {
            if (this._isDisposed)
                return;
            this._eventedStreamReader.OnReceivedLine -= new EventedStreamReader.OnReceivedLineHandler(this.OnReceivedLine);
            this._isDisposed = true;
        }
    }
}
