using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username or Email is required.")]
        [MaxLength(50, ErrorMessage = "Max 50 characters allowed.")]
        [DisplayName("Username of Email")]
        public string UserNameOrEmail { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MaxLength(20, ErrorMessage = "Max 20 characters allowed.")]
        public string Password { get; set; }
    }
}
