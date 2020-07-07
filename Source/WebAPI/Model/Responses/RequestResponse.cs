using System;
using System.Collections.Generic;

namespace WebAPI.Model.Responses
{
    public class RequestResponse
    {
        public Guid RefNo { get; set; }
        public string RequestStatus { get; set; }
        public string UserEmail { get; set; }
        public ICollection<RequestDetailResponse> RequestDetails { get; set; }
    }
}
