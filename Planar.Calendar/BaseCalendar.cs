using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz.Impl.Calendar;
using System;
using System.IO;

namespace Planar.Calendar
{
    public abstract class BaseCalendar<T> : BaseCalendar
    {
        private readonly ILogger _logger;

        public BaseCalendar(ILogger logger)
        {
            _logger = logger;
        }

        protected ILogger Logger
        {
            get
            {
                return _logger;
            }
        }

        protected TSettings LoadSettings<TSettings>()
        {
            var parts = GetType().FullName.Split('.');
            var name = parts[^1].Replace("Settings", string.Empty);
            var filename = Path.Combine(FolderConsts.BasePath, FolderConsts.Data, FolderConsts.Calendars, $"{parts[^2]}", $"{name}.json");

            if (File.Exists(filename))
            {
                var content = File.ReadAllText(filename);
                var settings = JsonConvert.DeserializeObject<TSettings>(content);
                return settings;
            }
            else
            {
                Logger.LogError("{Name} settings file '{Filename}' could not be found", name, filename);
                throw new ApplicationException($"{name} settings file '{filename}' could not be found");
            }
        }
    }
}