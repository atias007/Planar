using System;
using System.Text.Json;

namespace Planar.CLI.General
{
    internal class ServerSendEvent<T> where T : class
    {
        public string Event { get; set; } = string.Empty;
        public T? Data { get; set; }
        public string Id { get; set; } = string.Empty;

        public bool Parse(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) { return false; }
            var parts = value.Split(':', 2);
            if (parts.Length != 2) { return false; }
            var key = parts[0].Trim();
            var val = parts[1].Trim();
            switch (key)
            {
                case "event":
                    Event = val;
                    return false;

                case "data":
                    Data = JsonSerializer.Deserialize<T>(val);
                    return false;

                default:
                    return false;

                case "id":
                    Id = val;
                    return true;
            }
        }
    }

    internal sealed class WaitEventData
    {
        public TimeSpan? EstimatedEndTime { get; set; }
        public int TotalRunningInstances { get; set; }
    }
}