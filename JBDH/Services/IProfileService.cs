using JBDH.Models;

public interface IProfileService
{
    Task<User> GetUserByIdAsync(int userId);
}
