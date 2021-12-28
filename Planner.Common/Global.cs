using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace Planner.Common
{
    public static class Global
    {
        public static Dictionary<string, string> Parameters { get; set; }

        private static string _environment;

        public static string Environment
        {
            get
            {
                return string.IsNullOrEmpty(_environment) ? Consts.ProductionEnvironment : _environment;
            }
            set
            {
                _environment = value;
            }
        }

        public static IServiceProvider ServiceProvider { get; set; }

        public static ILogger<T> GetLogger<T>()
        {
            var logger = ServiceProvider.GetService(typeof(ILogger<T>)) as ILogger<T>;
            return logger;
        }

        public static ILogger GetLogger(Type type)
        {
            Type generic = typeof(ILogger<>);
            var loggerType = generic.MakeGenericType(type);
            var logger = ServiceProvider.GetService(loggerType) as ILogger;
            return logger;
        }

        public static void Clear()
        {
            Parameters = null;
            Environment = null;
            ServiceProvider = null;
        }
    }
}