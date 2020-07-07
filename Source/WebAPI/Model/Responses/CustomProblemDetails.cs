using Newtonsoft.Json;
using WebAPI.Constants;

namespace WebAPI.Model.Responses
{
    public class CustomProblemDetails
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
        public string Detail { get; set; }
        public ErrorCode DetailCode { get; set; }
        public string InnerDetail { get; set; }

        [JsonProperty("traceId")]
        public string TraceId { get; set; }
    }
}
