using System;
using System.Collections.Generic;

namespace WebAPI.Model.Database
{
    public class Request
    {
        public Request()
        {
            RequestDetails = new HashSet<RequestDetail>();
        }

        public Guid Id { get; set; }
        public Guid RefNo { get; set; }
        public int IsCurrent { get; set; }

        public virtual User User { get; set; }
        public virtual RequestStatus RequestStatus { get; set; }
        public virtual ICollection<RequestDetail> RequestDetails { get; set; }
    }
}