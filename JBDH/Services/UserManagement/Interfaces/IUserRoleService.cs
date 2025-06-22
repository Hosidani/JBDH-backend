using System.Collections.Generic;
using System.Threading.Tasks;

namespace JBDH.Services.UserManagement
{
    public interface IUserRoleService
    {
        Task<bool> UpdateUserRolesAsync(int userId, List<string> selectedRoles);
    }
}