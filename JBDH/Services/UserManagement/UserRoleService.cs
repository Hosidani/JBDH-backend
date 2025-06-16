using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JBDH.Models;
using JBDH.Models.User;
using JBDH.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace JBDH.Services
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

            // Remove all roles except "User"
            var currentRoles = _context.UserRoleAssignments
                .Where(ura => ura.UserId == userId && ura.Role != "User");
            _context.UserRoleAssignments.RemoveRange(currentRoles);

            // Add new roles (except "User")
            foreach (var role in selectedRoles)
            {
                if (role != "User" && !_context.UserRoleAssignments.Any(ura => ura.UserId == userId && ura.Role == role))
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