using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Planner.Common;
using Quartz.Impl.Calendar;
using System;
using System.Collections.Generic;
using System.IO;

namespace Planner.Service.Calendars
{
    public abstract class BaseCalendar<T> : BaseCalendar
    {
        private readonly Singleton<ILogger<T>> _logger = new(GetLogger);
        private readonly Dictionary<long, bool> _cache = new();

        private static ILogger<T> GetLogger()
        {
            return Global.ServiceProvider?.GetService(typeof(ILogger<T>)) as ILogger<T>;
        }

        protected ILogger<T> Logger
        {
            get
            {
                return _logger.Instance;
            }
        }

        protected TSettings LoadSettings<TSettings>()
        {
            var parts = GetType().FullName.Split('.');
            var name = parts[^1].Replace("Settings", string.Empty);
            var file = $@"Data\Calendars\{parts[^2]}\{name}.json";
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var filename = Path.Combine(path, file);

            if (File.Exists(filename))
            {
                var content = File.ReadAllText(filename);
                var settings = JsonConvert.DeserializeObject<TSettings>(content);
                return settings;
            }
            else
            {
                Logger.LogError("{@name} settings file '{@filename}' could not be found", name, filename);
                throw new ApplicationException($"{name} settings file '{filename}' could not be found");
            }
        }

        protected void AddCache(DateTimeOffset date, bool result)
        {
            _cache.TryAdd(date.Ticks, result);
        }

        protected bool? GetCache(DateTimeOffset date)
        {
            if (_cache.TryGetValue(date.Ticks, out var result))
            {
                return result;
            }
            else
            {
                return null;
            }
        }
    }
}