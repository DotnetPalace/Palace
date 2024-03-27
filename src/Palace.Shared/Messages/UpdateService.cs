namespace Palace.Shared.Messages;

public class UpdateService : IMessage
{
	public string HostName { get; set; } = null!;
	public MicroServiceSettings ServiceSettings { get; set; } = new();
	public string DownloadUrl { get; set; } = null!;
    public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(60);
}
