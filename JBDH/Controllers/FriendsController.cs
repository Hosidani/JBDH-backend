using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using JBDH.Services;

namespace JBDH.Controllers
{
    public class FriendshipController : Controller
    {
        private readonly IUserPropertiesService _IUserPropertiesService;
        private readonly IFriendService _friendService;

        public FriendshipController(IUserPropertiesService userPropertiesService, IFriendService friendService)
        {
            _IUserPropertiesService = userPropertiesService;
            _friendService = friendService;
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string addresseeId)
        {
            var currentUser = await _IUserPropertiesService.GetCurrentUserAsync();
            if (currentUser == null)
                return View("ErrorPage", "Could not find current user.");

            var addresseeIdInt = int.Parse(addresseeId);
            var result = await _friendService.SendFriendRequestAsync(currentUser.Id, addresseeIdInt);

            if (result.Success)
                return RedirectToAction("SuccessPage", "Settings", new { message = "Friend request sent!" });
            else
                return View("ErrorPage", result.ErrorMessage ?? "Could not send friend request.");
        }
    }
}
