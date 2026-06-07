namespace LoginAppWebApi.Models
{
    public class ServiceResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }

        public static ServiceResult Ok()
        {
            return new ServiceResult { Success = true };
        }

        public static ServiceResult Fail(string message)
        {
            return new ServiceResult
            {
                Success = false,
                ErrorMessage = message
            };
        }
    }
}
