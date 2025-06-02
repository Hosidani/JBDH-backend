using JBDH.Models;
using Microsoft.EntityFrameworkCore;

public class ProfileService : IProfileService
{
    private readonly ApplicationDbContext _context;

    public ProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User> GetUserByIdAsync(int userId)
    {
        return await _context.Set<JBDH.Models.User>()
                .Include(u => u.AIResponses)
                .FirstOrDefaultAsync(u => u.Id == userId);
    }
}
