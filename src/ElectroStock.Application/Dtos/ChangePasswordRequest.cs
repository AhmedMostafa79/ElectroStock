using System.ComponentModel.DataAnnotations;

namespace ElectroStock.Application.Dtos
{
    public class ChangePasswordRequest
    {
        [Required]
        public string OldPassword { get; set; }
        [Required]
        [MinLength(8, ErrorMessage = "New password must be at least 8 characters.")]
        public string NewPassword { get; set; }
    }
}
