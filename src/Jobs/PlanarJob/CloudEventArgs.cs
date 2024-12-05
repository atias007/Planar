using CloudNative.CloudEvents;
using System;

namespace Planar
{
    public class CloudEventArgs(CloudEvent cloudEvent, string clientId) : EventArgs
    {
        public string ClientId { get; set; } = clientId;

        public CloudEvent CloudEvent { get; private set; } = cloudEvent;
    }
}