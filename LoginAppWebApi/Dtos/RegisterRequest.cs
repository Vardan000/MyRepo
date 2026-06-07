namespace LoginAppWebApi.Dtos.Auth
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = "";
        public string Login { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}