using System.ComponentModel.DataAnnotations;

namespace TaskManagement.Models
{
    public class RegistrationViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [MaxLength(20, ErrorMessage = "Max 20 characters allowed.")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required.")]
        [DataType(DataType.EmailAddress)]
        [MaxLength(100, ErrorMessage = "Max 100 characters allowed.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [MaxLength(20, ErrorMessage = "Max 20 characters allowed.")]
        public string Password { get; set; }


        [Compare("Password", ErrorMessage = "Please confirm password.")]
        [DataType(DataType.Password)]

        public string ConfirmPassword { get; set; }
    }
}
