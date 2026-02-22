namespace ScanWorker.Data.Models;

public class EventProcessingState
{
    public int Id { get; set; }
    
    public long LastProcessedEventId  { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}