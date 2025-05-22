namespace JBDH.Services
{
    public interface ILoginService
    {
        Task<bool> ValidateUserAsync(string username, string password);
        Task SignInUserAsync(string username);
    }
}