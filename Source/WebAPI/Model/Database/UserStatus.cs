using System.Collections.Generic;

namespace WebAPI.Model.Database
{
    public class UserStatus
    {
        public UserStatus()
        {
            Users = new HashSet<User>();
        }

        public string Id { get; set; }

        public virtual ICollection<User> Users { get; set; }
    }
}