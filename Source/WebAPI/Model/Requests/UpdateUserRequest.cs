namespace WebAPI.Model.Requests
{
    public class UpdateUserRequest
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Role { get; set; }
        public string Status { get; set; }
    }
}
