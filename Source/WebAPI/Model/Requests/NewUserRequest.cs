namespace WebAPI.Model.Requests
{
    public class NewUserRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string RecaptchaPayload { get; set; }
    }
}
