using System;

namespace Planar.CLI.Attributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public class ActionPropertyAttribute : Attribute
    {
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
    }
}