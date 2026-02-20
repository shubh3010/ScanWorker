namespace ScanWorker.Dtos;

public record ScanEventUserDto
{
    public required string UserId { get; init; }
    public string? RunId { get; init; }
    public string? CarrierId { get; init; }
}

