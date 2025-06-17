namespace JBDH.Models.ViewModels
{
    public class DataFlowViewModel
    {
        public int Id { get; set; }
        public int? UserId { get; set; }
        public string? Username { get; set; }
        public string? ContentType { get; set; } // Type of content (e.g., text, image, video)
        public string? Content { get; set; } // The actual content of the data flow entry
        public int? ParentDataFlowId { get; set; } // Optional, for replies
        public List<DataFlowViewModel>? Replies { get; set; } // For nested comments/replies

    }
}