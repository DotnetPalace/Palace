using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Palace.Plugin._2FA.Models;

internal class LoginForm
{
    public string Step { get; set; } = "email";
    public string Email { get; set; } = null!;
    public string? Digicode { get; set; }

}
