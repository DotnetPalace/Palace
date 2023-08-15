namespace Palace.Server.Models;

public class PerformanceCounter
{
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public long Value { get; set; }
}
