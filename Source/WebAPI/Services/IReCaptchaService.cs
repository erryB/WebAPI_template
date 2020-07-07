using System.Threading.Tasks;
using WebAPI.Model;

namespace WebAPI.Services
{
    /// <summary>
    /// ReCaptcha Service Interface.
    /// </summary>
    public interface IReCaptchaService
    {
        /// <summary>
        /// Validates that the ReCaptcha payload sent from the client is valid.
        /// </summary>
        /// <param name="response">Encrypted payload generated from the client side ReCaptcha widget.</param>
        /// <returns>Returns True if user is not a bot.</returns>
        Task<ReCaptchaResponse> ValidatePayload(string response);
    }
}