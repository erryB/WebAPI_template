using System;
using System.Collections.Generic;

namespace WebAPI.Model.Database
{
    /// <summary>
    /// Represents the User table in the database.
    /// </summary>
    public class User
    {
        public User()
        {
            Request = new HashSet<Request>();
        }

        public Guid Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        public virtual Role Role { get; set; }
        public virtual UserStatus UserStatus { get; set; }
        public virtual ICollection<Request> Request { get; set; }
    }
}
