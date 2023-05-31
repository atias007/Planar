using CommonJob;
using Microsoft.EntityFrameworkCore;
using Planar.Service.Model;
using Planar.Service.Model.DataObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Planar.Service.Data
{
    public class UserData : BaseDataLayer
    {
        public UserData(PlanarContext context) : base(context)
        {
        }

        public async Task<User> AddUser(User user)
        {
            var result = _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return result.Entity;
        }

        public async Task<User?> GetUser(int id, bool withTracking = false)
        {
            IQueryable<User> query = _context.Users;
            if (!withTracking)
            {
                query = query.AsNoTracking();
            }

            var result = await query.SingleOrDefaultAsync(u => u.Id == id);
            return result;
        }

        public async Task<User?> GetUser(string username, bool withTracking = false)
        {
            IQueryable<User> query = _context.Users;
            if (!withTracking)
            {
                query = query.AsNoTracking();
            }

            var result = await query.SingleOrDefaultAsync(u => u.Username == username);
            return result;
        }

        public async Task<UserIdentity?> GetUserIdentity(string username)
        {
            var result = await _context.Users
                .Where(u => u.Username.ToLower() == username.ToLower())
                .Select(u => new UserIdentity
                {
                    Id = u.Id,
                    Surename = u.FirstName,
                    GivenName = u.LastName,
                    Username = u.Username,
                    Password = u.Password,
                    Salt = u.Salt,
                })
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<int> GetUserRole(int id)
        {
            var result = await _context.Groups
                .Where(g => g.Users.Any(u => u.Id == id))
                .Select(g => g.RoleId)
                .OrderByDescending(g => g)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<int> GetUserRole(string username)
        {
            var result = await _context.Groups
                .Where(g => g.Users.Any(u => u.Username.ToLower() == username.ToLower()))
                .Select(g => g.RoleId)
                .OrderByDescending(g => g)
                .FirstOrDefaultAsync();

            return result;
        }

        public async Task<List<EntityTitle>> GetGroupsForUser(int id)
        {
            var result = await _context.Groups
                    .Where(g => g.Users.Any(u => u.Id == id))
                    .Select(g => new EntityTitle(g.Name))
                    .ToListAsync();

            return result;
        }

        public async Task UpdateUser(User user)
        {
            _context.Users.Update(user);
            _context.Entry(user).Property(u => u.Password).IsModified = false;
            _context.Entry(user).Property(u => u.Salt).IsModified = false;
            await _context.SaveChangesAsync();
        }

        public async Task<List<UserRow>> GetUsers()
        {
            var result = await _context.Users
                .Select(u => new UserRow
                {
                    EmailAddress1 = u.EmailAddress1,
                    FirstName = u.FirstName,
                    Id = u.Id,
                    LastName = u.LastName,
                    PhoneNumber1 = u.PhoneNumber1,
                    Username = u.Username
                })
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .ToListAsync();

            return result;
        }

        public async Task<int> RemoveUser(string username)
        {
            var result = await _context.Users.Where(u => u.Username.ToLower() == username.ToLower()).ExecuteDeleteAsync();
            return result;
        }

        public async Task<bool> IsUserExists(int userId)
        {
            return await _context.Users.AnyAsync(u => u.Id == userId);
        }

        public async Task<int> GetUserId(string username)
        {
            return await _context.Users
                .Where(u => u.Username.ToLower() == username.ToLower())
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> IsUsernameExists(string? username)
        {
            if (username == null) { return false; }
            return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public async Task<bool> IsUsernameExists(string? username, string ignoreUsername)
        {
            if (username == null) { return false; }
            return await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower() && u.Username.ToLower() != ignoreUsername.ToLower());
        }
    }
}