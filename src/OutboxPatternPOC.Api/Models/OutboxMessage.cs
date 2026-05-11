namespace OutboxPatternPOC.Api.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool Processed { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
