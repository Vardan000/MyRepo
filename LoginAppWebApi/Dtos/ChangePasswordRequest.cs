using System.ComponentModel.DataAnnotations;

namespace LoginAppWebApi.Dtos.Auth
{
    public class ChangePasswordRequest
    {
        [Required]
        [MinLength(4)]
        [MaxLength(30)]
        public string Login { get; set; } = "";

        [Required]
        [MinLength(1)]
        public string CurrentPassword { get; set; } = "";

        [Required]
        [MinLength(8)]
        public string NewPassword { get; set; } = "";
    }
}