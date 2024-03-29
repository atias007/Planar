﻿using Microsoft.EntityFrameworkCore;
using Planar.API.Common.Entities;
using Planar.Service.Model.DataObjects;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Group = Planar.Service.Model.Group;

namespace Planar.Service.Data
{
    public class GroupData : BaseDataLayer
    {
        public GroupData(PlanarContext context) : base(context)
        {
        }

        public async Task AddGroup(Group group)
        {
            _context.Groups.Add(group);
            await _context.SaveChangesAsync();
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

        public async Task<Group?> GetGroup(string name)
        {
            var result = await _context.Groups
                .Include(g => g.Role)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Name.ToLower() == name.ToLower());

            return result;
        }

        public async Task<int> GetGroupRole(string name)
        {
            var result = await _context.Groups
                .AsNoTracking()
                .Where(g => g.Name.ToLower() == name.ToLower())
                .Select(g => g.RoleId)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<int> GetGroupId(string name)
        {
            return await _context.Groups
                .AsNoTracking()
                .Where(g => g.Name.ToLower() == name.ToLower())
                .Select(g => g.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<PagingResponse<GroupInfo>> GetGroups(IPagingRequest request)
        {
            var result = await _context.Groups
                .Include(g => g.Users)
                .Include(g => g.Role)
                .Select(g => new GroupInfo
                {
                    Name = g.Name,
                    UsersCount = g.Users.Count,
                    Role = g.Role.Name
                })
                .OrderBy(g => g.Name)
                .ToPagingListAsync(request);

            return result;
        }

        public async Task<Group?> GetGroupWithUsers(int id)
        {
            var result = await _context.Groups
                .Include(g => g.Users)
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.Id == id);

            return result;
        }

        public async Task<bool> IsGroupExists(int groupId)
        {
            return await _context.Groups.AsNoTracking().AnyAsync(g => g.Id == groupId);
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

        public async Task<bool> IsGroupNameExists(string? name, int id)
        {
            if (name == null) { return false; }
            return await _context.Groups.AsNoTracking().AnyAsync(u => u.Id != id && u.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> IsGroupNameExists(string? name)
        {
            if (name == null) { return false; }
            return await _context.Groups.AsNoTracking().AnyAsync(u => u.Name.ToLower() == name.ToLower());
        }

        public async Task<bool> IsUserExistsInGroup(int userId, int groupId)
        {
            return await _context.Groups.AsNoTracking().AnyAsync(g => g.Id == groupId && g.Users.Any(u => u.Id == userId));
        }

        public async Task<int> RemoveGroup(int id)
        {
            return await _context.Groups.Where(g => g.Id == id).ExecuteDeleteAsync();
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

        public async Task SetRoleToGroup(int groupId, int roleId)
        {
            var group = new Model.Group { Id = groupId, RoleId = roleId };
            _context.Entry(group).Property(g => g.RoleId).IsModified = true;
            await _context.SaveChangesAsync();
        }

        public async Task UpdateGroup(Group group)
        {
            _context.Groups.Update(group);
            await _context.SaveChangesAsync();
        }

        internal async Task<List<EntityTitle>> GetUsersInGroup(int id)
        {
            var result = await _context.Users
                .Where(u => u.Groups.Any(g => g.Id == id))
                .Select(u => new EntityTitle(u.Username, u.FirstName, u.LastName))
                .ToListAsync();

            return result;
        }
    }
}