using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Plugin._2FA;

internal class _2FAConfiguration
{
    public string DefaultAdminEmail { get; set; } = null!;
    public string DefaultAdminKey { get; set; } = null!;
}
