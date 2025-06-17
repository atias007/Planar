using System.Collections.Generic;
using System.Linq;

namespace Planar.Hook
{
    internal abstract class BaseGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
#if NETSTANDARD2_0
        public string AdditionalField1 { get; set; }
        public string AdditionalField2 { get; set; }
        public string AdditionalField3 { get; set; }
        public string AdditionalField4 { get; set; }
        public string AdditionalField5 { get; set; }
#else
        public string? AdditionalField1 { get; set; }

        public string? AdditionalField2 { get; set; }

        public string? AdditionalField3 { get; set; }

        public string? AdditionalField4 { get; set; }

        public string? AdditionalField5 { get; set; }
#endif
    }

    internal class TempGroup : BaseGroup
    {
        public List<User> Users { get; set; } = new List<User>();
    }

    internal class Group : BaseGroup, IMonitorGroup
    {
        public Group()
        {
        }

        public Group(TempGroup tempGroup)
        {
            Id = tempGroup.Id;
            Name = tempGroup.Name;
            AdditionalField1 = tempGroup.AdditionalField1;
            AdditionalField2 = tempGroup.AdditionalField2;
            AdditionalField3 = tempGroup.AdditionalField3;
            AdditionalField4 = tempGroup.AdditionalField4;
            AdditionalField5 = tempGroup.AdditionalField5;
            foreach (var user in tempGroup.Users)
            {
                AddUser(user);
            }
        }

        private List<IMonitorUser> _users = new List<IMonitorUser>();

        public IEnumerable<IMonitorUser> Users
        {
            get { return _users; }
            set { _users = value.ToList(); }
        }

        public void AddUser(IMonitorUser user)
        {
            _users.Add(user);
        }
    }
}