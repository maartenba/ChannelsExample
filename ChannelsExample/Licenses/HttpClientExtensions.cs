using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ChannelsExample.Licenses
{
    public static class HttpClientExtensions
    {
        public static async Task<string?> GetStringOrNullAsync(this HttpClient httpClient, string requestUri)
        {
            try
            {
                using var response = await httpClient.GetAsync(requestUri);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception)
            {
                // this-is-fine.gif
            }

            return null;
        }
    }
}