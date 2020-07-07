using System.Security.Claims;

namespace WebAPI.Helpers
{
    /// <summary>
    /// Extension methods for ClaimsIdentity.
    /// </summary>
    public static class ClaimsIdentityExtensions
    {
        /// <summary>
        /// Get the email from the Claims of a user identity.
        /// </summary>
        /// <param name="identity">User identity.</param>
        /// <returns>Email.</returns>
        public static string GetEmail(this ClaimsIdentity identity)
        {
            var email = identity.FindFirst("emails")?.Value; // B2C

            // Depending on the B2B the claim with the email could be different. These are some examples.
            email ??= identity.FindFirst(ClaimTypes.Email)?.Value; // B2B
            email ??= identity.FindFirst(ClaimTypes.Upn)?.Value; // B2B
            email ??= identity.FindFirst("preferred_username")?.Value; // B2B

            return email;
        }
    }
}
