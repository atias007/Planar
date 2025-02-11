namespace CommonJob.MessageBrokerEntities
{
    public class KeyValueObject
    {
        public string Key { get; set; } = string.Empty;

#if NETSTANDARD2_0
        public object Value { get; set; }
#else
        public object? Value { get; set; }
#endif
    }
}