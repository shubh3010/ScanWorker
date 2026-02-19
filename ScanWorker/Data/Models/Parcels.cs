using ScanWorker.Enums;

namespace ScanWorker.Data.Models;

public class Parcels
{
    public int ParcelId  { get; set; }
    
    public long LastEventId { get; set; }
    
    public EventType LastEventType { get; set; }
    
    public StatusCode? LastEventStatusCode { get; set; }
    
    public DateTime LastEventCreatedDateTimeUtc { get; set; }
    
    public string? LastRunId { get; set; }
    
    public DateTime? PickupDateTimeUtc { get; set; }
    
    public DateTime? DeliveryDateTimeUtc { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public string UserId { get; set; } = null!;
    
    // Navigation property
    public List<ScanEvents>? ScanEvents { get; set; }
    
    public User? User { get; set; }
}