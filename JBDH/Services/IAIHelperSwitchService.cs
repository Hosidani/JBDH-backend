using JBDH.Models;

namespace JBDH.Services
{
    public interface IAIHelperSwitchService
    {
        Task<StringContent>? HelperSwitch(User user, int commandId);
    }
}