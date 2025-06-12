namespace JBDH.Models;

public class DataFlow
{
    public int Id { get; set; }
    public string? ContentType { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public int? UserId { get; set; }
    public int? ParentDataFlowId { get; set; }
    public DataFlow? ParentDataFlow { get; set; }
    // I-collection of related data flows
    public ICollection<DataFlow>? RelatedDataFlows { get; set; }

}
