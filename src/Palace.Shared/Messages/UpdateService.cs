namespace Palace.Shared.Messages;

public class UpdateService : IMessage
{
	public string HostName { get; set; } = null!;
	public MicroServiceSettings ServiceSettings { get; set; } = new();
	public string DownloadUrl { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
