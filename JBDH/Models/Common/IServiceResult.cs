namespace JBDH.Models.Common
{
    public interface IServiceResult
    {
        bool Success { get; }
        string ErrorMessage { get; }
        string SuccessMessage { get; }
    }
}