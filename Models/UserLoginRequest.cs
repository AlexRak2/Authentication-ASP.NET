using System.ComponentModel.DataAnnotations;

namespace Authentication.Models
{
    public class UserLoginRequest
    {
        [Required(ErrorMessage = "Username or Email is required.")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required, MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
