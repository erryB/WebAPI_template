using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Diagnostics;
using System.Threading.Tasks;

namespace WebAPI.IntegrationTests.Common
{
    public static class SerializationExtensions
    {
        private static readonly DefaultContractResolver contractResolver = new DefaultContractResolver
        {
            NamingStrategy = new SnakeCaseNamingStrategy()
        };

        public static async Task<T> DeserializeAsync<T>(this Task<string> content)
            => JsonConvert.DeserializeObject<T>(await content, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Error = (sender, args) =>
                {
                    Debug.WriteLine($"JsonConvert.DeserializeObject error: {args.ErrorContext.Error}");
                    args.ErrorContext.Handled = true;
                }
            });

        public static string Serialize(this object content)
            => JsonConvert.SerializeObject(content, new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                Error = (sender, args) =>
                {
                    Debug.WriteLine($"JsonConvert.SerializeObject error: {args.ErrorContext.Error}");
                    args.ErrorContext.Handled = true;
                }
            });


    }
}
