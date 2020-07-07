using System.Collections.Generic;

namespace WebAPI.Model.Responses
{
    public class UsersResponse
    {
        public IEnumerable<UserResponse> Users { get; set; }
    }
}
