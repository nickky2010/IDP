using System;

namespace EFDemo.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = default!;
    public string Payload { get; set; } = default!;
    public bool IsProcessed { get; set; }
    public DateTime? ProcessedOn { get; set; }
} 