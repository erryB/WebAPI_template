using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using WebAPI.Constants;
using WebAPI.Helpers;

namespace WebAPI.Controllers.Helpers
{
    /// <summary>
    /// Helper methods to use in controllers.
    /// </summary>
    public static class ControllerHelpers
    {
        /// <summary>
        /// Checks if any parameter is null or empty.
        /// </summary>
        /// <param name="parameters">Parameters to check.</param>
        /// <returns>True if any of the parameters is null or empty.</returns>
        public static bool AnyMissing(params object[] parameters)
            => parameters.Any(p =>
            {
                return p switch
                {
                    Guid g => g == Guid.Empty,
                    string s => string.IsNullOrWhiteSpace(s),
                    IEnumerable<object> e => e.Count() == 0,
                    object o => false,
                    null => true,
                };
            });

        /// <summary>
        /// Creates an ObjectResult that produces a ProblemDetails response with additional error information.
        /// </summary>
        /// <param name="controller">Controller.</param>
        /// <param name="title">The value for ProblemDetails.Title.</param>
        /// <param name="statusCode">The value for ProblemDetails.Status.</param>
        /// <param name="detailCode">The value for ProblemDetails.ErrorCode.</param>
        /// <param name="innerDetail">The value for ProblemDetails.ErrorDetails.</param>
        /// <returns>ObjectResult with ProblemDetails.</returns>
        public static ObjectResult Problem(this ControllerBase controller, string title, int statusCode, ErrorCode detailCode, string innerDetail = null)
        {
            var result = controller.Problem(detailCode.GetStringValue(), null, statusCode, title, null);
            var extensions = ((ProblemDetails)result.Value).Extensions;
            extensions.Add("detail_code", detailCode);
            if (innerDetail != null)
            {
                extensions.Add("inner_detail", innerDetail);
            }

            return result;
        }

        /// <summary>
        /// Returns the email of the user who calls the controller.
        /// </summary>
        /// <param name="controller">controller called by the user.</param>
        /// <returns>email.</returns>
        public static string GetUserEmail(this ControllerBase controller)
            => controller.User.Identities.FirstOrDefault()?.GetEmail();
    }
}
