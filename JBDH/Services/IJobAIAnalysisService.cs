using System.Net.Http;
using System.Threading.Tasks;

public interface IJobAIAnalysisService
{
    Task<StringContent> AnalyzeJobAsync(string jobAdUrl);
}