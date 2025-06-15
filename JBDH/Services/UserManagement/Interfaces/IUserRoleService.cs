using System.Collections.Generic;
using System.Threading.Tasks;

namespace JBDH.Services.Interfaces
{
    public interface IUserRoleService
    {
        Task<bool> UpdateUserRolesAsync(int userId, List<string> selectedRoles);
    }
}