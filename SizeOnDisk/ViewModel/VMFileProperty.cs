namespace SizeOnDisk.ViewModel
{
    public class VMFileProperty
    {
        public VMFileProperty(string key, string name, string value)
        {
            Key = key;
            Name = name;
            Value = value;
        }

        public string Key { get; }

        public string Name { get; }

        public string Value { get; }
    }
}
