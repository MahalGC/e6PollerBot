using e6PollerBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace e6PollerBot.Services
{
    public class e6Service
    {
        private static Random _rand;
        private SemaphoreSlim _random_guard = new SemaphoreSlim(1);

        private SemaphoreSlim _mutex = new SemaphoreSlim(1);

        private readonly HttpClient _http;
        private static readonly string _random_limit = "100";

        public e6Service(HttpClient http)
        {
            _http = http;
            _http.DefaultRequestHeaders.Add("User-Agent", "Discord e6PollerBot/1.0 (by nekofelix on e621)");
            _rand = new Random();
        }
           

        public async Task<string> GetRandomPicture(string tags)
        {
            string query_string = $"https://e621.net/post/index.json?tags={tags}&limit={_random_limit}";
            await _mutex.WaitAsync().ConfigureAwait(false);
            HttpResponseMessage response;
            try
            {
                response = await _http.GetAsync(query_string);
            }
            finally
            {
                try
                {
                    await Task.Delay(1000);
                }
                finally
                {
                    _mutex.Release();
                }
            }

            string response_string = await response.Content.ReadAsStringAsync();
   
            string[] ids = parse_response(response_string);
            int random_index = await get_random(0, ids.Count());

            string return_string = $"https://e621.net/post/show/{ids[random_index]}";
            return return_string;
        }

        private string[] parse_response(string response_string)
        {
            List<e6Post> result = JsonConvert.DeserializeObject<List<e6Post>>(response_string);

            string[] ids = result.Select(x => x.id).ToArray();
            return ids;
        }

        private async Task<int> get_random(int minValue, int maxValue)
        {
            int num;
            await _mutex.WaitAsync().ConfigureAwait(false);
            try
            {
                num = _rand.Next(minValue, maxValue);
            }
            finally
            {
                _mutex.Release();
            }
            return num;
        }
    }
}
