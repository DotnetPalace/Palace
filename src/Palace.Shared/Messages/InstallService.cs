using System.Xml;

namespace Palace.Shared.Messages;

public class InstallService : IMessage
{
    public required Guid ActionId { get; set; }
    public required string HostName { get; set; } = null!;
    public required  MicroServiceSettings ServiceSettings { get; set; } = new();
    public required string DownloadUrl { get; set; } = null!;
    public string Trigger { get; set; } = null!;
    public DateTime Timeout => DateTime.Now.AddSeconds(15);
}
