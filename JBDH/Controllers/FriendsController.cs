using Microsoft.AspNetCore.Mvc;
using JBDH.Services.UserManagement;
using JBDH.Services.Friendship;
using JBDH.Helpers;

namespace JBDH.Controllers
{
    public class FriendsController : Controller
    {
        private readonly IUserPropertiesService _IUserPropertiesService;
        private readonly IFriendService _friendService;

        public FriendsController(IUserPropertiesService userPropertiesService, IFriendService friendService)
        {
            _IUserPropertiesService = userPropertiesService;
            _friendService = friendService;
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string addresseeId)
        {
            var currentUser = await _IUserPropertiesService.GetCurrentUserAsync();
            if (currentUser == null)
                return this.ErrorPage("Could not find current user.");

            if (!int.TryParse(addresseeId, out int addresseeIdInt))
                return this.ErrorPage("Invalid addressee ID.");

            var result = await _friendService.SendFriendRequestAsync(currentUser.Id, addresseeIdInt);
            if (result.Success)
                return this.SetSuccessAndRedirect("Friend request sent!", "SuccessPage", new { controller = "Settings", message = "Friend request sent!" });
            else
                return this.ErrorPage(result.ErrorMessage ?? "Could not send friend request.");
        }
    }
}
