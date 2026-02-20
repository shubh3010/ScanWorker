using ScanWorker.Enums;

namespace ScanWorker.Data.Models;

public class ScanEvents
{
    public long EventId { get; set; }
    
    public int ParcelId { get; set; }
    
    public EventType Type { get; set; }
    
    public DateTime CreatedDateTimeUtc { get; set; }
    
    public StatusCode? StatusCode { get; set; }
    
    public string? RunId { get; set; }

    public string UserId { get; set; } = null!;
    
    public int? DeviceId { get; set; }
    
    public string? DeviceTransactionId { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation property
    public User? User { get; set; }
    
    public Parcels? Parcel { get; set; }
}