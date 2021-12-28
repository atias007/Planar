namespace CommonJob
{
    public interface ICommonJob
    {
        void SetJobRunningProperty<TPropery>(string key, TPropery value);

        TPropery GetJobRunningProperty<TPropery>(string key);

        bool ContainsJobRunningProperty(string key);
    }
}