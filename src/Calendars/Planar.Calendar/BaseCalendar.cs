using Newtonsoft.Json;
using Quartz.Impl.Calendar;
using System;
using System.IO;

namespace Planar.Calendar
{
    public abstract class PlanarBaseCalendar : BaseCalendar
    {
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
                Console.WriteLine($"ERROR: {name} settings file '{filename}' could not be found");
                throw new PlanarCalendarException($"{name} settings file '{filename}' could not be found");
            }
        }
    }
}