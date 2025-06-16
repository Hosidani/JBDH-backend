public class LoginResult
{
    public bool IsSuccess { get; set; }
    public bool IsBanned { get; set; }
    public bool UserExists { get; set; }
    public bool PasswordCorrect { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}
