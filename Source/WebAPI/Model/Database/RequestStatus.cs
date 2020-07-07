using System.Collections.Generic;

namespace WebAPI.Model.Database
{
    public class RequestStatus
    {
        public RequestStatus()
        {
            Requests = new HashSet<Request>();
        }

        public string Id { get; set; }

        public virtual ICollection<Request> Requests { get; set; }
    }
}