namespace JBDH.Models.AI
{
    /// <summary>
    /// Provides user message templates for AI command IDs.
    /// </summary>
    public static class AIMessageTemplates
    {
        public static readonly Dictionary<int, string> UserMessageTemplates = new()
        {
            [0] = "Analyse this CV and tell me which job is the best fit for this candidate. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"BestJobTitlesForUser\" (a short list, maximum 10 job titles), " +
                  "\"ReasoningBehindJobTitles\" (brief), " +
                  "\"HardSkills\" (a short list, maximum 10 skills), " +
                  "\"SoftSkills\" (a short list, maximum 10 skills), " +
                  "\"Experiences\" (a list of objects, each with \"title\" (string) and \"duration\" (string, e.g. \"3.5 years\")), and " +
                  "\"EducationAndCertifications\" (a short list, maximum 10 items, brief). " +
                  "\n\nCV:\n{CVText}",

            [1] = "Analyze the following job advertisement. " +
                  "Extract and summarize the key requirements and characteristics of the ideal candidate. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"JobTitle\" (a short list, maximum 10 relevant job titles which are suitable to the job annons.), " +
                  "\"HardRequirements\" (a short list, maximum 10 must-have requirements or skills), " +
                  "\"GoodToHaves\" (a short list, maximum 10 nice-to-have skills or traits), " +
                  "\"Experiences\" (a short list, maximum 10 relevant experiences), " +
                  "\"EducationAndCertifications\" (a short list, maximum 10 items, brief), " +
                  "\"JobSummary\" (a brief summary of the job). " +
                  "\n\nJob Ad:\n{JobAdText}",

            [2] = "You are given a job advertisement and a candidate's CV. " +
                  "Compare the candidate's qualifications, skills, and experiences to the requirements in the job ad. " +
                  "Be explicit about where the candidate is a strong fit and where there are gaps or missing requirements. " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"FitSummary\" (brief summary of fit), " +
                  "\"MissingRequirements\" (list of gaps or missing requirements), " +
                  "\"MatchingSkills\" (list of skills from the CV that match the job ad), " +
                  "\"RelevantExperiences\" (list of relevant experiences from the CV), " +
                  "\"EducationAndCertifications\" (from the CV, relevant to the job ad). " +
                  "\n\nJob Ad:\n{JobAdText}\n\nCV:\n{CVText}",

            [3] = "You are given two AI-generated JSON objects: one is a summary of a user's CV, the other is a summary of a job ad. " +
                "Compare the user's qualifications, skills, experiences, and education/certifications to the requirements in the job ad. " +
                "For each skill, experience, and education/certification, check if there is a direct or close match. " +
                "Return ONLY a valid JSON object with ALL of the following fields (do not omit any): " +
                "\"Percentage\" (a number between 0 and 100 representing the match, based on actual overlap), " +
                "\"FitSummary\" (brief summary of fit), " +
                "\"MissingRequirements\" (list of gaps or missing requirements), " +
                "\"MatchingSkills\" (list of skills from the user that match the job ad), " +
                "\"RelevantExperiences\" (list of relevant experiences from the user that match the job ad), " +
                "\"EducationAndCertifications\" (from the user, relevant to the job ad), " +
                "\"Reasoning\" (explain how you calculated the percentage and why). " +
                "Example: { \"Percentage\": 85, ... }" +
                "\n\nUserAIResponse:\n{UserAIResponse}\n\nJobAIResponse:\n{JobAIResponse}",

            [4] = "Can you give me a proposal for courses that would help this user improve their skills? " +
                  "Return ONLY a valid JSON object with the following fields: " +
                  "\"Courses\" (a short list, maximum 10 courses, with titles and brief descriptions). " +
                  "\n\nCV:\n{CVText}",

            [5] = "Analyze the CV against this job advertisement and provide structured feedback. " +
                  "Return ONLY a valid JSON object with this structure: " +
                  "{" +
                  "  \"matchScore\": 0-100," +
                  "  \"fitAssessment\": {" +
                  "    \"overall\": string," +
                  "    \"strengths\": [string]," +
                  "    \"concerns\": [string]" +
                  "  }," +
                  "  \"detailedAnalysis\": {" +
                  "    \"technicalSkills\": {\"matchLevel\": \"high|medium|low\", \"matches\": [string], \"gaps\": [string]}," +
                  "    \"experience\": {\"matchLevel\": \"high|medium|low\", \"matches\": [string], \"gaps\": [string]}," +
                  "    \"education\": {\"matchLevel\": \"high|medium|low\", \"details\": [string]}," +
                  "    \"softSkills\": {\"matchLevel\": \"high|medium|low\", \"evidence\": [string]}" +
                  "  }," +
                  "  \"actionableRecommendations\": {" +
                  "    \"cvImprovements\": [string]," +
                  "    \"skillDevelopment\": [string]," +
                  "    \"interviewPreparation\": [string]" +
                  "  }," +
                  "  \"nextSteps\": [string]" +
                  "}" +
                  "\n\nJob Ad:\n{JobAdText}\n\nCV:\n{CVText}",

            [6] = "Uppgift: Extrahera huvudkrav och önskade kvalifikationer från följande jobbannons. För varje krav, returnera: " +
                  "- name (kort text) " +
                  "- category (ex: skill, experience, degree, certification, language) " +
                  "- importance (1-5, där 5 = must-have / avgörande, 1 = nice-to-have) " +
                  "Returnera SÅ ENKEL JSON-LISTA som möjligt, t.ex: " +
                  "[{\"name\":\"Python\",\"category\":\"skill\",\"importance\":5},{\"name\":\"SQL\",\"category\":\"skill\",\"importance\":4}] " +
                  "Job ad: " +
                  "---BEGIN--- " +
                  "{JobAdText} " +
                  "---END---",
            [7] = "Uppgift: Analysera följande CV och extrahera kandidatens profession och alla kompetenser. " +
                  "Identifiera profession med tre nivåer: main, sub och specialty baserat på kandidatens bakgrund och erfarenheter. " +
                  "Till exempel: 'Engineering > Software > Backend', 'Healthcare > Surgery > Cardiac', 'Business > Marketing > Digital'. " +
                  "Extrahera även alla kompetenser från CV:t (både hårda och mjuka skills): " +
                  "- name (kort text) " +
                  "- category (skill, experience, degree, certification, language, soft-skill) " +
                  "- importance (1-5 baserat på erfarenhetsnivå: 1=nybörjare, 2=grundläggande, 3=medel, 4=avancerad, 5=expert) " +
                  "Returnera JSON-format: " +
                  "{\"requirements\": [{\"name\":\"Python\",\"category\":\"skill\",\"importance\":4}, {\"name\":\"Communication\",\"category\":\"soft-skill\",\"importance\":3}], \"profession\": {\"main\":\"Engineering\", \"sub\":\"Software\", \"specialty\":\"Backend\"}} " +
                  "CV: " +
                  "---BEGIN--- " +
                  "{JobAdText} " +
                  "---END---",
            [8] = "Uppgift: Extrahera huvudkrav och önskade kvalifikationer från följande jobbannons. För varje krav, returnera: " +
                  "- name (kort text) " +
                  "- category (ex: skill, experience, degree, certification, language) " +
                  "- importance (1-5, där 5 = must-have / avgörande, 1 = nice-to-have) " +
                  "Identifiera även profession för jobbet med tre nivåer: main, sub och specialty. Om inget tydligt framgår, hittar du lämpliga nivåer baserade på textenTill exempel: 'Engineering > Software > Backend', 'Healthcare > Surgery > Cardiac', 'Logistics > Supply Chain > Procurement'. " +
                  "Returnera SÅ ENKEL JSON-LISTA som möjligt: " +
                  "{\"requirements\": [{\"name\":\"Python\",\"category\":\"skill\",\"importance\":5}], \"profession\": {\"main\":\"Engineering\", \"sub\":\"Software\", \"specialty\":\"Backend\"}} " +
                  "Job ad: " +
                  "---BEGIN--- " +
                  "{JobAdText} " +
                  "---END---"
        };
    }
}









        