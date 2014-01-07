namespace Rhino
{
    public class InstanceIdInfo
    {
        public int Id { get; private set; }
        public PropertyAttributes Attributes { get; private set; }

        public InstanceIdInfo(int id, PropertyAttributes attributes)
        {
            Id = id;
            Attributes = attributes;
        }
    }
}