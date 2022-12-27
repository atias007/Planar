using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class GroupData : BaseDataLayer
    {
        public GroupData(PlanarContext context) : base(context)
        {
        }

        public async Task AddGroup(Group group)
        {
            await _context.Groups.AddAsync(group);
            await _context.SaveChangesAsync();
        }

        internal async Task<List<EntityTitle>> GetUsersInGroup(int id)
        {
            var result = await _context.Users
                .Where(u => u.Groups.Any(g => g.Id == id))
                .Select(u => new EntityTitle(u.Id, u.FirstName, u.LastName))
                .ToListAsync();

            return result;
        }

        public async Task<Group> GetGroup(int id)
        {
            var result = await _context.Groups
                .Include(g => g.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            return result;
        }

        public async Task<List<GroupInfo>> GetGroups()
        {
            var result = await _context.Groups
                .Include(g => g.Users)
                .Include(g => g.Role)
                .Select(g => new GroupInfo
                {
                    Id = g.Id,
                    Name = g.Name,
                    UsersCount = g.Users.Count,
                    Role = g.Role.Name
                })
                .OrderBy(g => g.Name)
                .ToListAsync();

            return result;
        }

        public async Task<Dictionary<int, string>> GetGroupsName()
        {
            var result = await _context.Groups
                .Select(g => new { g.Id, g.Name })
                .OrderBy(g => g.Name)
                .ToDictionaryAsync(k => k.Id, v => v.Name);

            return result;
        }

        public async Task UpdateGroup(Group group)
        {
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveGroup(Group group)
        {
            _context.Groups.Remove(group);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsGroupHasMonitors(int groupId)
        {
            var result = await _context.MonitorActions.AnyAsync(m => m.Group.Id == groupId);
            return result;
        }

        public async Task<bool> IsGroupHasUsers(int groupId)
        {
            var result = await _context.Groups.AnyAsync(g => g.Id == groupId && g.Users.Any());
            return result;
        }

        public async Task AddUserToGroup(int userId, int groupId)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null) { return; }

            var group = _context.Groups.FirstOrDefault(x => x.Id == groupId);
            if (group == null) { return; }

            group.Users.Add(user);
            await _context.SaveChangesAsync();
        }

        public async Task RemoveUserFromGroup(int userId, int groupId)
        {
            var user = _context.Users.FirstOrDefault(x => x.Id == userId);
            if (user == null) { return; }

            var group = _context.Groups.Include(g => g.Users).FirstOrDefault(x => x.Id == groupId);
            if (group == null) { return; }

            group.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> IsGroupNameExists(string name, int id)
        {
            return await _context.Groups.AnyAsync(u => u.Id != id && u.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> IsUserExistsInGroup(int userId, int groupId)
        {
            return await _context.Groups.AnyAsync(g => g.Id == groupId && g.Users.Any(u => u.Id == userId));
        }

        public async Task<bool> IsGroupExists(int groupId)
        {
            return await _context.Groups.AnyAsync(g => g.Id == groupId);
        }
    }
}