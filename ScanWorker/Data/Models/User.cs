namespace ScanWorker.Data.Models;

public class User
{
    public string UserId { get; set; } = null!;
    
    public string CarrierId { get; set; } = string.Empty;
    
    public DateTime CreatedAt { get; set; }
    
    // Navigation property
    public List<Parcels>? Parcels { get; set; }
    
    public List<ScanEvents>? ScanEvents { get; set; }
}