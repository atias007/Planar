using System;

namespace Planar.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ActionPropertyAttribute : Attribute
    {
        public ActionPropertyAttribute()
        {
            ShortName = string.Empty;
            LongName = string.Empty;
        }

        public ActionPropertyAttribute(string shortName, string longName)
        {
            ShortName = shortName;
            LongName = longName;
        }

        public string? Name { get; set; }

        public string LongName { get; set; }

        public string ShortName { get; set; }

        public bool Default { get; set; }

        private int _defaultOrder;

        public int DefaultOrder
        {
            get { return _defaultOrder; }
            set
            {
                _defaultOrder = value;
                Default = true;
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(Name))
                {
                    return Name;
                }

                if (string.IsNullOrEmpty(LongName) && string.IsNullOrEmpty(ShortName))
                {
                    return string.Empty;
                }

                if (string.IsNullOrEmpty(LongName))
                {
                    return $"-{ShortName}";
                }

                if (string.IsNullOrEmpty(ShortName))
                {
                    return $"--{LongName}";
                }

                return $"-{ShortName}|--{LongName}";
            }
        }
    }
}