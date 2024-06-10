using Planar.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Planar.Common;

public interface IGroupDataLayer
{
    Task<IEnumerable<UserForReport>> GetGroupUsers(string name);
}