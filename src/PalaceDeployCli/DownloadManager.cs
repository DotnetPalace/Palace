using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PalaceDeployCli
{
	public class DownloadManager
	{
		public DownloadManager(PalaceDeployCliSettings settings,
            ILogger<DownloadManager> logger,
            IHttpClientFactory httpClientFactory)
		{
			this.Settings = settings;
            this.Logger = logger;
            this.HttpClientFactory = httpClientFactory;
		}

		protected PalaceDeployCliSettings Settings { get; }
        protected ILogger Logger { get; }
        protected IHttpClientFactory HttpClientFactory { get; }

        public async Task<string> DownloadPackage(string packageUrl)
        {
            var httpClient = HttpClientFactory.CreateClient("Downloader");

            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.GetAsync(packageUrl);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, ex.Message);
                return null;
            }

            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                Logger.LogWarning("response fail for download {0}", packageUrl);
                return null;
            }

            var zipFileName = System.IO.Path.Combine(Settings.DownloadDirectory, System.IO.Path.GetFileName(packageUrl));

            if (File.Exists(zipFileName))
            {
                File.Delete(zipFileName);
            }
            using (var fs = new System.IO.FileStream(zipFileName, System.IO.FileMode.Create))
            {
                var stream = response.Content.ReadAsStreamAsync().Result;
                int bufferSize = 1024;
                byte[] buffer = new byte[bufferSize];
                int pos = 0;
                while ((pos = stream.Read(buffer, 0, bufferSize)) > 0)
                {
                    fs.Write(buffer, 0, pos);
                }
                fs.Close();
            }

            return zipFileName;
        }

    }
}
