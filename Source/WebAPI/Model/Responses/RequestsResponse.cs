using System.Collections.Generic;

namespace WebAPI.Model.Responses
{
    public class RequestsResponse
    {
        public IEnumerable<RequestResponse> Requests { get; set; }
    }
}
