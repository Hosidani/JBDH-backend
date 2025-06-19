namespace JBDH.Services.Authentication
{
    public interface ILoginService
    {
        Task<LoginResult> ValidateUserAsync(string username, string password);
        Task SignInUserAsync(string username);
    }
}