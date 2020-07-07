using System.Text.Json.Serialization;

namespace WebAPI.Model
{
    public class ReCaptchaResponse
    {
        public bool Success { get; set; }

        [JsonPropertyName("challenge_ts")]
        public string ChallengeTs { get; set; }

        [JsonPropertyName("error-codes")]
        public string[] ErrorCodes { get; set; }
    }
}
