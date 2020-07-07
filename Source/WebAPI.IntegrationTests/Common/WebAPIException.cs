using System;
using WebAPI.Model.Responses;

namespace WebAPI.IntegrationTests.Common
{
    public class WebAPIException : Exception
    {
        public CustomProblemDetails CustomProblemDetails { get; set; }

        public WebAPIException(string message) : base(message) { }
    }
}
