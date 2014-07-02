using System;

namespace Sharpen
{
    public class EventObject : EventArgs
    {
        public EventObject(object source)
        {
            Source = source;
        }

        public object Source { get; private set; }
    }
}