namespace ScanWorker.Data.Models;

public class User
{
    public string UserId { get; set; }
    
    public string? CarrierId { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public virtual List<Parcel>? Parcels { get; set; }
    
    public virtual List<ScanEvent>? ScanEvents { get; set; }
}