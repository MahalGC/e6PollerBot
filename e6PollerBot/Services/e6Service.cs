using e6PollerBot.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace e6PollerBot.Services
{
    public class e6Service
    {
        private SemaphoreSlim _e6_throttle_guard = new SemaphoreSlim(1);

        private readonly HttpClient _http;
        private static readonly int _random_limit = 300;
        private static readonly int _throttle_timing = 1000; // Miliseconds to wait between e6 API calls.

        private static readonly string _e6_show_post_base_url = "https://e621.net/post/show";

        public e6Service(HttpClient http)
        {
            _http = http;
            _http.DefaultRequestHeaders.Add("User-Agent", "Discord e6PollerBot/1.0 (by nekofelix on e621)");
        }

        // Get a random picture from e6.
        public async Task<string> GetRandomPicture(string tags)
        {
            List<e6Post> e6Posts = await Querye6ByTag(tags, _random_limit);
            List<string> ids = e6Posts.Select(x => x.id).ToList();
            if (ids.Count() == 0) return "No posts matched your search.";

            int random_index = RandomThreadSafe.Next(0, ids.Count());
            string return_string = $"{_e6_show_post_base_url}/{ids[random_index]}";
            return return_string;
        }

        // Query the GET e6 Posts List API
        private async Task<List<e6Post>> Querye6ByTag(string tags, int limit)
        {
            string query_string = $"https://e621.net/post/index.json?tags={tags}&limit={_random_limit}";
            List<e6Post> e6Posts = await Gete6(query_string);
            return e6Posts;
        }

        // GET from e6 API.
        private async Task<List<e6Post>> Gete6(string url)
        {
            await e6Throttle();

            List<e6Post> e6Posts;
            using (Stream s = await _http.GetStreamAsync(url))
            using (StreamReader sr = new StreamReader(s))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = new JsonSerializer();
                e6Posts = await Task.Run(() => serializer.Deserialize<List<e6Post>>(reader));
            }
            return e6Posts;
        }

        // Any calls to the e6 API should be throttled.
        private async Task e6Throttle()
        {
            await _e6_throttle_guard.WaitAsync().ConfigureAwait(false);
            try
            {
                await Task.Delay(_throttle_timing);
            }
            finally
            {
                _e6_throttle_guard.Release();
            }
        }
    }
}
