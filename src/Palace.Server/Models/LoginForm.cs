using System.ComponentModel.DataAnnotations;

namespace Palace.Server.Models;

public class LoginForm
{
    [Required]
    public string Key { get; set; } = null!;
}
