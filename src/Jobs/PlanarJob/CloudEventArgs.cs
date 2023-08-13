using CloudNative.CloudEvents;
using MQTTnet.Server;
using System;

namespace Planar
{
    public class CloudEventArgs : EventArgs
    {
        public CloudEventArgs(CloudEvent cloudEvent, string clientId)
        {
            CloudEvent = cloudEvent;
            ClientId = clientId;
        }

        public string ClientId { get; set; }

        public CloudEvent CloudEvent { get; private set; }
    }
}