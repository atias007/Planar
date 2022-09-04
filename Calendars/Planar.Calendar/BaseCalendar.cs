using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Quartz.Impl.Calendar;
using System;
using System.IO;

namespace Planar.Calendar
{
    public abstract class PlanarBaseCalendar : BaseCalendar
    {
        private readonly ILogger _logger;

        protected PlanarBaseCalendar(ILogger logger)
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
            var filename = FolderConsts.GetSpecialFilePath(PlanarSpecialFolder.Calendars, $"{parts[^2]}", $"{name}.json");

            if (File.Exists(filename))
            {
                var content = File.ReadAllText(filename);
                var settings = JsonConvert.DeserializeObject<TSettings>(content);
                return settings;
            }
            else
            {
                Logger.LogError("{Name} settings file '{Filename}' could not be found", name, filename);
                throw new PlanarCalendarException($"{name} settings file '{filename}' could not be found");
            }
        }
    }
}