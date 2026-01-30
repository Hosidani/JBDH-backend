using System.ComponentModel.DataAnnotations;

namespace JBDH.Models.DTOs
{
    /// <summary>
    /// DTO för att spara jobb-AI-analys som JSON i JobAIResponse.ResponseText
    /// Matchar AI template [1] från AIMessageTemplates + extra fält för V2-sökning
    /// </summary>
    public class JobAIAnalysisDto
    {
        // DIREKTA FÄLT FRÅN AI (Template [1])
        public List<string> JobTitle { get; set; } = new(); // Max 10 relevanta jobbtitlar
        public List<string> HardRequirements { get; set; } = new(); // Max 10 must-have krav
        public List<string> GoodToHaves { get; set; } = new(); // Max 10 nice-to-have
        public List<string> Experiences { get; set; } = new(); // Max 10 relevanta erfarenheter
        public List<string> EducationAndCertifications { get; set; } = new(); // Max 10 utbildningar
        public string JobSummary { get; set; } = string.Empty; // Kort sammanfattning av jobbet
        
        // UTÖKADE FÄLT FÖR V2-SÖKNING
        public List<string> WorkArea { get; set; } = new(); // Arbetsområden (ex: "IT", "Healthcare", "Finance")
        public List<string> Roles { get; set; } = new(); // Roller/ansvarsområden (ex: "Team Lead", "Individual Contributor")
    }
}