using System.Diagnostics;
using System.Text;

namespace Palace.Host;
public class ProcessStartingLogger(ILogger logger, string hostName, ManualResetEvent mre)
{
    private readonly StringBuilder _report = new StringBuilder();

    public bool HasError { get; private set; }
    public string Report => _report.ToString();

    public void ErrorDataReceived(object sender, DataReceivedEventArgs e)
    {
        HasError = true;
        if (e.Data is null)
        {
            return;
        }
        logger.LogDebug("{HostName} {Row}", hostName, e.Data);
        _report.AppendLine(e.Data);
    }

    public void OutputDataReceived(object sender, DataReceivedEventArgs e)
    {
        if (e.Data is null)
        {
            return;
        }
        logger.LogDebug("{HostName} {Row}", hostName, e.Data);
        _report.AppendLine(e.Data);
        if (e.Data == "Palace service is started")
        {
            mre.Set();
        }
    }
}
