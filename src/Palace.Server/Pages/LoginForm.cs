using System.ComponentModel.DataAnnotations;

namespace Palace.Server.Pages;

public class LoginForm
{
    [Required]
    public string Key { get; set; } = null!;
}
