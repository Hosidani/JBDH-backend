using JBDH.Models.User;

namespace JBDH.Models.DTOs
{
    public class AccountDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string AccountName { get; set; } = string.Empty;
        public string AccountEmail { get; set; } = string.Empty;
        public string? AccountPhone { get; set; }
        public string PreferredLanguage { get; set; } = "en";
        public AccountType AccountType { get; set; } = AccountType.Basic;
        public bool IsTerminated { get; set; } = false;
        public DateTime? TerminatedAt { get; set; }
        public bool AcceptedTermsAndConditions { get; set; } = false;
        public DateTime? TermsAcceptedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Extended data for profile
        public List<string> Roles { get; set; } = new();
        public List<OrganisationDto> Organisations { get; set; } = new();
        
        // For updates - optional fields
        public List<string>? UpdateRoles { get; set; }
        public List<int>? JoinOrganisationIds { get; set; }
        public List<int>? LeaveOrganisationIds { get; set; }
    }
}