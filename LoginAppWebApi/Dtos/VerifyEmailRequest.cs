using System.ComponentModel.DataAnnotations;

namespace LoginAppWebApi.Dtos.Auth
{
    public class VerifyEmailRequest
    {
        
        
        public string Email { get; set; } = "";
        public string Code { get; set; } = "";
    }
}