namespace ScanWorker.Data.Models;

public class Parcel
{
    public int ParcelId  { get; set; }

    public long LastEventId { get; set; }

    public string LastEventType { get; set; } = null!;
    
    public string? LastEventStatusCode { get; set; }
    
    public DateTime LastEventCreatedDateTimeUtc { get; set; }
    
    public string? LastRunId { get; set; }
    
    public DateTime? PickupDateTimeUtc { get; set; }
    
    public DateTime? DeliveryDateTimeUtc { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public string UserId { get; set; } = null!;
    
    // Navigation property
    public virtual List<ScanEvent>? ScanEvents { get; set; }
    
    public virtual User? User { get; set; }
}