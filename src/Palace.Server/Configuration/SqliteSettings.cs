namespace Palace.Server.Configuration;

public class SqliteSettings 
{
    public int DayCountOfRentention { get; set; } = 7;
    public string ConnectionString { get; set; } = null!;
}
