namespace Sharpen
{
    public class PropertyChangeEvent : EventObject
    {
        public string PropertyName { get; private set; }
        public object OldValue { get; private set; }
        public object NewValue { get; private set; }

        public PropertyChangeEvent(object source, string propertyName, object oldValue, object newValue)
            : base(source)
        {
            PropertyName = propertyName;
            OldValue = oldValue;
            NewValue = newValue;
        }
    }
}