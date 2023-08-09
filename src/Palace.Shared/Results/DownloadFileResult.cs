using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Shared.Results
{
    public class DownloadFileResult
    {
        public string ZipFileName { get; set; } = null!;
        public string Version { get; set; } = null!;
        public string? FailReason { get; set; }
        public bool Success { get; set; } = false;
    }
}
