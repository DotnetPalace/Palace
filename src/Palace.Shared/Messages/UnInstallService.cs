using System.Xml;

namespace Palace.Shared.Messages;

public class UnInstallService : IMessage
{
    public required Guid ActionId { get; set; }
    public required string HostName { get; set; } = default!;
    public required MicroServiceSettings ServiceSettings { get; set; } = default!;
    public DateTime Timeout { get; set; } = DateTime.Now.AddSeconds(15);
}
