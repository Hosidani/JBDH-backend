using System;

namespace JBDH.Models.DTOs
{
    public class UserAIResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string ResponseText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public object? Analysis { get; set; } // Parsed object
    }
}