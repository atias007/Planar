﻿using CloudNative.CloudEvents;
using CloudNative.CloudEvents.Core;
using MQTTnet;
using System;
using System.Collections.Generic;

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
            Validation.CheckNotNull(formatter, nameof(formatter));
            Validation.CheckNotNull(message, nameof(message));

            return formatter.DecodeStructuredModeMessage(message.PayloadSegment, contentType: null, extensionAttributes);
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
            Validation.CheckCloudEventArgument(cloudEvent, nameof(cloudEvent));
            Validation.CheckNotNull(formatter, nameof(formatter));

            switch (contentMode)
            {
                case ContentMode.Structured:
                    return new MqttApplicationMessage
                    {
                        Topic = topic,
                        PayloadSegment = BinaryDataUtilities.AsArray(formatter.EncodeStructuredModeMessage(cloudEvent, out _))
                    };

                default:
                    throw new ArgumentOutOfRangeException(nameof(contentMode), $"Unsupported content mode: {contentMode}");
            }
        }
    }
}