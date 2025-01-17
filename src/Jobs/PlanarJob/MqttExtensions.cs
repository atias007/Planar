using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using MQTTnet;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

namespace Planar
{
    internal static class MqttExtensions
    {
        /// <summary>
        /// Converts this MQTT message into a CloudEvent object.
        /// </summary>
        /// <param name="message">The MQTT message to convert. Must not be null.</param>
        /// <param name="formatter">The event formatter to use to parse the CloudEvent. Must not be null.</param>
        /// <param name="extensionAttributes">The extension attributes to use when parsing the CloudEvent. May be null.</param>
        /// <returns>A reference to a validated CloudEvent instance.</returns>
        public static CloudEvent ToCloudEvent(this MqttApplicationMessage message,
            CloudEventFormatter formatter, params CloudEventAttribute[]? extensionAttributes) =>
            ToCloudEvent(message, formatter, (IEnumerable<CloudEventAttribute>?)extensionAttributes);

        /// <summary>
        /// Converts this MQTT message into a CloudEvent object.
        /// </summary>
        /// <param name="message">The MQTT message to convert. Must not be null.</param>
        /// <param name="formatter">The event formatter to use to parse the CloudEvent. Must not be null.</param>
        /// <param name="extensionAttributes">The extension attributes to use when parsing the CloudEvent. May be null.</param>
        /// <returns>A reference to a validated CloudEvent instance.</returns>
        public static CloudEvent ToCloudEvent(this MqttApplicationMessage message,
            CloudEventFormatter formatter, IEnumerable<CloudEventAttribute>? extensionAttributes)
        {
            CheckNotNull(formatter, nameof(formatter));
            CheckNotNull(message, nameof(message));

            return formatter.DecodeStructuredModeMessage(message.Payload.ToArray(), contentType: null, extensionAttributes);
        }

        /// <summary>
        /// Converts a CloudEvent to <see cref="MqttApplicationMessage"/>.
        /// </summary>
        /// <param name="cloudEvent">The CloudEvent to convert. Must not be null, and must be a valid CloudEvent.</param>
        /// <param name="contentMode">Content mode. Currently only structured mode is supported.</param>
        /// <param name="formatter">The formatter to use within the conversion. Must not be null.</param>
        /// <param name="topic">The MQTT topic for the message. May be null.</param>
        public static MqttApplicationMessage ToMqttApplicationMessage(this CloudEvent cloudEvent, ContentMode contentMode, CloudEventFormatter formatter, string? topic)
        {
            CheckCloudEventArgument(cloudEvent, nameof(cloudEvent));
            CheckNotNull(formatter, nameof(formatter));

            return contentMode switch
            {
                ContentMode.Structured => new MqttApplicationMessage
                {
                    Topic = topic,
                    PayloadSegment = BinaryDataUtilities.AsArray(formatter.EncodeStructuredModeMessage(cloudEvent, out _))
                },
                _ => throw new ArgumentOutOfRangeException(nameof(contentMode), $"Unsupported content mode: {contentMode}"),
            };
        }

        private static void CheckNotNull<T>(T value, string? paramName) where T : class
        {
            if (value == null) { throw new ArgumentNullException(paramName); }
        }

        private static void CheckCloudEventArgument(CloudEvent cloudEvent, string? paramName)
        {
            CheckNotNull(cloudEvent, paramName);
            if (cloudEvent.IsValid)
            {
                return;
            }

            var missing = cloudEvent.SpecVersion.RequiredAttributes.Where(attr => cloudEvent[attr] is null).ToList();
            string joinedMissing = string.Join(", ", missing);
            throw new ArgumentException($"CloudEvent is missing required attributes: {joinedMissing}", paramName);
        }
    }
}