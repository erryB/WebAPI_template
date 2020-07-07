using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Text;
using WebAPI.Constants;

namespace WebAPI.Tests.Common
{
    public static class ErrorHelpers
    {
        public static ObjectResult ExpectedErrorResult(int statusCode, ErrorCode innerErrorCode, string innerErrorDetails = null)
        {
            var expectedResult = new ObjectResult(new ProblemDetails() { Status = statusCode }) { StatusCode = statusCode };
            var extensions = ((ProblemDetails)expectedResult.Value).Extensions;
            extensions.Add("detail_code", innerErrorCode);
            if (innerErrorDetails != null)
            {
                extensions.Add("inner_detail", innerErrorDetails);
            }

            return expectedResult;
        }
    }
}
