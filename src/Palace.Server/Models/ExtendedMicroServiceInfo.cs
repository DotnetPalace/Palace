namespace Palace.Server.Models;

public class ExtendedMicroServiceInfo : RunningMicroserviceInfo
{
    public string HostName { get; set; } = null!;
    public DateTime CreationDate { get; set; } = DateTime.Now;
    public bool UIDisplayMore { get; set; } = false;
    public string? FailReason { get; set; }
    public string? Log { get; set; }

    public string Key => $"{HostName}-{ServiceName}".ToLower();

    public override string ToString()
    {
        var piList = GetType().GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var sb = new System.Text.StringBuilder();
        foreach (var item in piList)
        {
            sb.AppendLine($"{item.Name} = {item.GetValue(this)}");
        }
        return sb.ToString();
    }
}
