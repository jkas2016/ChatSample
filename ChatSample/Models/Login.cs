using System.ComponentModel.DataAnnotations;

namespace ChatSample.Models;

public class Login
{
    [Required] public string UserName { get; set; } = null!;
}