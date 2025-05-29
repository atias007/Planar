using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Exceptions;
using Planar.Service.Model;
using System;
using System.Linq;

namespace Planar.Service.Validation
{
    public class MonitorActionValidator
    {
        private readonly ILogger? _logger;

        public MonitorActionValidator()
        {
        }

        public MonitorActionValidator(ILogger logger)
        {
            _logger = logger;
        }

        internal int[]? ValidateMonitorArguments(MonitorAction action)
        {
            var @event = (MonitorEvents)action.EventId;
            return ValidateMonitorArguments(@event, action.EventArgument);
        }

        internal int[]? ValidateMonitorArguments(AddMonitorRequest request)
        {
            var @event = Enum.Parse<MonitorEvents>(request.EventName ?? string.Empty);
            var arguments = request.EventArgument;
            return ValidateMonitorArguments(@event, arguments);
        }

        private int[]? ValidateMonitorArguments(MonitorEvents @event, string? arguments)
        {
            if (!MonitorEventsExtensions.IsMonitorEventHasArguments(@event)) { return null; }

            if (string.IsNullOrWhiteSpace(arguments))
            {
                _logger?.LogWarning("event argument is required with {Event} event type", @event);
                throw new RestValidationException("Event Argument", $"event argument is required with '{@event}' event type");
            }

            switch (@event)
            {
                case MonitorEvents.ExecutionFailxTimesInRow: // 200
                    var value1 = ValidateNumeric(arguments);
                    ValidateRange(value1, 2, 1000);
                    return [value1];

                case MonitorEvents.ExecutionFailxTimesInyHours: // 201
                    var values = ValidateNumericArray(arguments, 2);
                    ValidateRange(values[0], 2, 1000);
                    ValidateRange(values[1], 1, 72);
                    return values;

                case MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanx: // 202
                    var value3 = ValidateNumeric(arguments);
                    ValidateRange(value3, 0, int.MaxValue);
                    return [value3];

                case MonitorEvents.ExecutionEndWithEffectedRowsLessThanx: // 203
                    var value4 = ValidateNumeric(arguments);
                    ValidateRange(value4, 2, int.MaxValue);
                    return [value4];

                case MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanxInyHours: // 204
                    var values3 = ValidateNumericArray(arguments, 2);
                    ValidateRange(values3[0], 0, int.MaxValue);
                    ValidateRange(values3[1], 1, 72);
                    return values3;

                case MonitorEvents.ExecutionEndWithEffectedRowsLessThanxInyHours: // 205
                    var values2 = ValidateNumericArray(arguments, 2);
                    ValidateRange(values2[0], 1, int.MaxValue);
                    ValidateRange(values2[1], 1, 72);
                    return values2;

                case MonitorEvents.ExecutionDurationGreaterThanxMinutes: // 206
                    var value2 = ValidateNumeric(arguments);
                    ValidateRange(value2, 1, 1440);
                    return [value2];

                case MonitorEvents.ExecutionEndWithMoreThanxExceptions: // 207
                    var value5 = ValidateNumeric(arguments);
                    ValidateRange(value5, 1, 9999);
                    return [value5];

                default:
                    return null;
            }
        }

        private int ValidateNumeric(string? arguments)
        {
            if (string.IsNullOrEmpty(arguments) || !ValidationUtil.IsOnlyDigits(arguments))
            {
                _logger?.LogWarning("event argument {Arguments} is not valid numeric integer value", arguments);
                throw new RestValidationException("Event Argument", $"event argument '{arguments}' is not valid numeric integer value");
            }

            return int.Parse(arguments);
        }

        private void ValidateRange(int value, int min, int max)
        {
            if (value < min)
            {
                _logger?.LogWarning("event argument {Value} should be greater then or equals to {Min}", value, min);
                throw new RestValidationException("Event Argument", $"event argument '{value}' should be greater then or equals to {min}");
            }

            if (value > max)
            {
                _logger?.LogWarning("event argument {Value} should be less then or equals to {Max}", value, max);
                throw new RestValidationException("Event Argument", $"event argument '{value}' should be less then or equals to {max}");
            }
        }

        private int[] ValidateNumericArray(string arguments, int size)
        {
            var parts = arguments.Split(',').Select(p => p?.ToLower()).ToList();
            if (parts.Count != size)
            {
                _logger?.LogWarning("event argument {Arguments} should have {Size} numeric integers seperated by comma (,)", arguments, size);
                throw new RestValidationException("Event Argument", $"event argument '{arguments}' should have {size} numeric integer seperated by comma (,)");
            }

            foreach (var item in parts)
            {
                ValidateNumeric(item);
            }

            return [.. parts.Select(p => int.Parse(p ?? "0"))];
        }
    }
}