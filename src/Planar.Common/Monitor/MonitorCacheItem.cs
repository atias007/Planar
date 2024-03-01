namespace Planar.Common.Monitor
{
    public class MonitorCacheItem
    {
        private string? _eventArguments;

        public string? EventArgument

        {
            get { return _eventArguments; }
            init
            {
                _eventArguments = value;
                if (int.TryParse(_eventArguments, out var duration))
                {
                    DurationLimit = duration;
                }
            }
        }

        public string? JobName { get; init; }
        public string? JobGroup { get; init; }
        public int? DurationLimit { get; init; }
    }
}