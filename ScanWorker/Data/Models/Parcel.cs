namespace ScanWorker.Data.Models;

public class Parcel
{
    public int ParcelId  { get; set; }
    
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual List<ScanEvent>? ScanEvents { get; set; }
}