using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using WebAPI.Model.Responses;

namespace WebAPI.IntegrationTests.Common
{
    public static class HttpHelpers
    {
        public static async Task<T> ExecuteGetRequestAsync<T>(WebApplicationFactory<Startup> factory, string api, string bearerToken = null)
        {
            using var httpClient = factory.CreateClient();
            if (bearerToken != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            using var response = await httpClient.GetAsync(api);

            var output = await response
                .Content
                .ReadAsStringAsync()
                .DeserializeAsync<T>();

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch(HttpRequestException ex)
            {
                throw new WebAPIException(ex.Message) { CustomProblemDetails = output as CustomProblemDetails };
            }

            return output;
        }

        public static async Task<T> ExecutePostRequestAsync<T>(WebApplicationFactory<Startup> factory, string api, object body, string bearerToken = null)
        {
            using var httpClient = factory.CreateClient();
            if (bearerToken != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var content = new StringContent(body.Serialize(), Encoding.UTF8, "application/json");
            using var response = await httpClient.PostAsync(api, content);

            var output = await response
                .Content
                .ReadAsStringAsync()
                .DeserializeAsync<T>();

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new WebAPIException(ex.Message) { CustomProblemDetails = output as CustomProblemDetails };
            }

            return output;
        }

        public static async Task<T> ExecuteDeleteRequestAsync<T>(WebApplicationFactory<Startup> factory, string api, string bearerToken = null)        
        {
            using var httpClient = factory.CreateClient();
            if (bearerToken != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }
            using var response = await httpClient.DeleteAsync(api);

            var output = await response
                .Content
                .ReadAsStringAsync()
                .DeserializeAsync<T>();

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new WebAPIException(ex.Message) { CustomProblemDetails = output as CustomProblemDetails };
            }

            return output;
        }

        public static async Task<T> ExecutePatchRequestAsync<T>(WebApplicationFactory<Startup> factory, string api, object body, string bearerToken = null)
        {
            using var httpClient = factory.CreateClient();
            if (bearerToken != null)
            {
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            }

            var content = new StringContent(body.Serialize(), Encoding.UTF8, "application/json");
            using var response = await httpClient.PatchAsync(api, content);

            var output = await response
                 .Content
                 .ReadAsStringAsync()
                 .DeserializeAsync<T>();

            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch (HttpRequestException ex)
            {
                throw new WebAPIException(ex.Message) { CustomProblemDetails = output as CustomProblemDetails };
            }

            return output;
        }
    }
}
