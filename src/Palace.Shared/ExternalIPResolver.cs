namespace Palace.Shared;

public class ExternalIPResolver
{
    public static async Task<string> GetIP()
    {
        var httpClient = new HttpClient();
        string? ip = null;
        try
        {
            var response = await httpClient.GetAsync("https://api.ipify.org");
            ip = await response.Content.ReadAsStringAsync();
        }
        catch (Exception) 
        {
            ip = System.Net.Dns.GetHostAddresses(System.Net.Dns.GetHostName()).ToList().First().MapToIPv4().ToString();
        }
        return ip;
    }
}
