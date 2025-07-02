using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models.User;
using JBDH.Models.Core;
using JBDH.Models.Services;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services.UserManagement
{
    public class UserRoleService : IUserRoleService
    {
        private readonly ApplicationDbContext _context;

        public UserRoleService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> UpdateUserRolesAsync(int userId, List<string> selectedRoles)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
                return false;

            // Handle null selectedRoles
            selectedRoles ??= new List<string>();

            // Remove all existing roles for this user
            var currentRoles = _context.UserRoleAssignments
                .Where(ura => ura.UserId == userId);
            _context.UserRoleAssignments.RemoveRange(currentRoles);

            // Always add "User" role first
            _context.UserRoleAssignments.Add(new UserRoleAssignment
            {
                UserId = userId,
                User = user,
                Role = "User"
            });

            // Add new roles (excluding "User" to avoid duplicates, but allow empty/whitespace)
            foreach (var role in selectedRoles.Distinct())
            {
                if (role != "User") // Allow empty and whitespace roles as tests expect
                {
                    _context.UserRoleAssignments.Add(new UserRoleAssignment
                    {
                        UserId = userId,
                        User = user,
                        Role = role
                    });
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}