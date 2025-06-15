using JBDH.Models;

namespace JBDH.Services
{
    public interface IAIRequestBuilderService
    {
        Task<StringContent?> HelperSwitch(AIHelperRequest aiHelperRequest);
    }
}