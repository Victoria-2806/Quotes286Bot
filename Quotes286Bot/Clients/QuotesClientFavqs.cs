using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Quotes286Bot.Models;

namespace Quotes286Bot.Clients
{
    public class QuotesClientFavqs
    {
        private HttpClient httpClient;
        private static string? addressFavqs;
        private static string? apikeyFavqs;

        public QuotesClientFavqs()
        {
            addressFavqs = Constants.addressFavqs;
            apikeyFavqs = Constants.apikeyFavqs;
            httpClient = new HttpClient();
            httpClient.BaseAddress = new Uri(addressFavqs);
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Token token=\"{apikeyFavqs}\"");
        }

        public async Task<List<QuoteFavqs>> GetAllQuotes()
        {
            List<QuoteFavqs> quotes = new List<QuoteFavqs>();
            HttpResponseMessage response = await httpClient.GetAsync("api/quotes");
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                quotes = result["quotes"].ToObject<List<QuoteFavqs>>();
                quotes = quotes.Take(5).ToList();
            }
            else
            {
                Console.WriteLine($"Помилка запиту до API: {response.StatusCode}");
            }
            return quotes;
        }

        public async Task<List<QuoteFavqs>> GetQuotesByAuthor(string authorName)
        {
            List<QuoteFavqs> quotes = new List<QuoteFavqs>();
            HttpResponseMessage response = await httpClient.GetAsync($"api/quotes/?filter={Uri.EscapeDataString(authorName)}&type=author");
            if (response.IsSuccessStatusCode)
            {
                string jsonResponse = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<JObject>(jsonResponse);
                quotes = result["quotes"].ToObject<List<QuoteFavqs>>();
                //quotes = quotes.Take(5).ToList();
            }
            else
            {
                Console.WriteLine($"Помилка запиту до API: {response.StatusCode}");
            }
            return quotes;
        }
    }
}
