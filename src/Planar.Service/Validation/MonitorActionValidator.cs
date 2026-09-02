using Microsoft.Extensions.Logging;
using Planar.API.Common.Entities;
using Planar.Common;
using Planar.Service.Exceptions;
using Planar.Service.Model;

namespace Planar.Service.Validation;

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
        return ValidateMonitorArguments(@event, action.GetEventArguments());
    }

    internal int[]? ValidateMonitorArguments(MonitorRequest request)
    {
        var @event = MonitorEventsParser.Parse(request.Event) ?? MonitorEvents.CustomEvent1;
        var arguments = request.EventArguments;
        return ValidateMonitorArguments(@event, arguments);
    }

    private int[]? ValidateMonitorArguments(MonitorEvents @event, MonitorEventArguments? arguments)
    {
        if (!MonitorEventsExtensions.IsMonitorEventHasArguments(@event)) { return null; }

        if (arguments == null || arguments.IsEmpty())
        {
            _logger?.LogWarning("event arguments is required with {Event} event type", @event.GetEnumDescription());
            throw new RestValidationException("Event Arguments", $"event arguments is required with '{@event.GetEnumDescription()}' event type");
        }

        switch (@event)
        {
            case MonitorEvents.ExecutionFailxTimesInRow: // 200
                ValidateRange(arguments.X, 2, 1000, "X");
                ValidateEmpty(arguments.Y, "Y");
                return [arguments.X.GetValueOrDefault()];

            case MonitorEvents.ExecutionFailxTimesInyHours: // 201
                ValidateRange(arguments.X, 2, 1000, "X");
                ValidateRange(arguments.Y, 1, 72, "Y");
                return [arguments.X.GetValueOrDefault(), arguments.Y.GetValueOrDefault()];

            case MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanx: // 202
                ValidateRange(arguments.X, 0, int.MaxValue, "X");
                ValidateEmpty(arguments.Y, "Y");
                return [arguments.X.GetValueOrDefault()];

            case MonitorEvents.ExecutionEndWithEffectedRowsLessThanx: // 203
                ValidateRange(arguments.X, 2, int.MaxValue, "X");
                ValidateEmpty(arguments.Y, "Y");
                return [arguments.X.GetValueOrDefault()];

            case MonitorEvents.ExecutionEndWithEffectedRowsGreaterThanxInyHours: // 204
                ValidateRange(arguments.X, 0, int.MaxValue, "X");
                ValidateRange(arguments.Y, 1, 72, "Y");
                return [arguments.X.GetValueOrDefault(), arguments.Y.GetValueOrDefault()];

            case MonitorEvents.ExecutionEndWithEffectedRowsLessThanxInyHours: // 205
                ValidateRange(arguments.X, 1, int.MaxValue, "X");
                ValidateRange(arguments.Y, 1, 72, "Y");
                return [arguments.X.GetValueOrDefault(), arguments.Y.GetValueOrDefault()];

            case MonitorEvents.ExecutionDurationGreaterThanxMinutes: // 206
                ValidateRange(arguments.X, 1, 1440, "X");
                ValidateEmpty(arguments.Y, "Y");
                return [arguments.X.GetValueOrDefault()];

            case MonitorEvents.ExecutionEndWithMoreThanxExceptions: // 207
                ValidateRange(arguments.X, 1, 9999, "X");
                ValidateEmpty(arguments.Y, "Y");
                return [arguments.X.GetValueOrDefault()];

            default:
                return null;
        }
    }

    private void ValidateEmpty(int? value, string name)
    {
        if (value != null)
        {
            name = name.ToLower();
            _logger?.LogWarning("event argument {Name} must be empty", name);
            throw new RestValidationException("Event Argument", $"event argument '{name}' must be empty");
        }
    }

    private void ValidateRange(int? value, int min, int max, string name)
    {
        if (value == null)
        {
            name = name.ToLower();
            _logger?.LogWarning("event argument {Name} is required", name);
            throw new RestValidationException("Event Argument", $"event argument '{name}' is required");
        }

        if (value < min)
        {
            name = name.ToLower();
            _logger?.LogWarning("event argument {Name} with value {Value} should be greater then or equals to {Min}", name, value, min);
            throw new RestValidationException("Event Argument", $"event argument '{name}' with value '{value}' should be greater then or equals to {min}");
        }

        if (value > max)
        {
            name = name.ToLower();
            _logger?.LogWarning("event argument {Name} with value {Value} should be less then or equals to {Max}", name, value, max);
            throw new RestValidationException("Event Argument", $"event argument '{name}' with value '{value}' should be less then or equals to {max}");
        }
    }
}