using System.Threading.Tasks;
using JBDH.Models.User;

namespace JBDH.Services.AICommunications
{
    public interface IAIUserCourseProposalService
    {
        Task GenerateAndSaveCourseProposalAsync(User user);
    }
}