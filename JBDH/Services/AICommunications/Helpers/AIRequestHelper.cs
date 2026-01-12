using System.Text;
using Newtonsoft.Json;

namespace JBDH.Services.AICommunications.Helpers
{
    public static class AIRequestHelper
    {
        public static StringContent BuildRequestBody(string model, string userMessage, string Language)
        {
            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "system", content = "You are to answer my questions in " + Language + " language." },
                    new { role = "user", content = userMessage }
                },
                stream = false
            };
            var requestJson = JsonConvert.SerializeObject(requestBody);
            return new StringContent(requestJson, Encoding.UTF8, "application/json");
        }
    }
}
