using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using WebAPI.Model;

namespace WebAPI.Services
{
    /// <summary>
    /// ReCaptcha Service.
    /// </summary>
    public class ReCaptchaService : IReCaptchaService
    {
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ILogger<ReCaptchaService> logger;
        private readonly string endpoint = "https://www.google.com/recaptcha/api/siteverify";
        private readonly string secretKey;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReCaptchaService"/> class.
        /// </summary>
        /// <param name="configuration">Configuration.</param>
        /// <param name="httpClientFactory">Http Client Factory.</param>
        /// <param name="logger">Logger.</param>
        public ReCaptchaService(IConfiguration configuration, IHttpClientFactory httpClientFactory, ILogger<ReCaptchaService> logger)
        {
            this.httpClientFactory = httpClientFactory;
            this.logger = logger;

            secretKey = configuration.GetValue<string>("reCAPTCHAServerKey");
        }

        /// <summary>
        /// Validates that the ReCaptcha payload sent from the client is valid.
        /// </summary>
        /// <param name="clientPayload">Encrypted payload generated from the client side ReCaptcha widget.</param>
        /// <returns>Returns True if user is not a bot.</returns>
        public async Task<ReCaptchaResponse> ValidatePayload(string clientPayload)
        {
            var client = httpClientFactory.CreateClient();

            Dictionary<string, string> parameters = new Dictionary<string, string>()
            {
               { "secret", secretKey },
               { "response", clientPayload },
            };

            var encodedContent = new FormUrlEncodedContent(parameters);

            var response = await client.PostAsync(endpoint, encodedContent);

            response.EnsureSuccessStatusCode();

            var recaptchaResponse = await response.Content.ReadFromJsonAsync<ReCaptchaResponse>();
            return recaptchaResponse;
        }
    }
}