using Discord;
using Discord.WebSocket;
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
        private static readonly int _max_search_query_size = 200;

        private static readonly string _e6_show_post_base_url = "https://e621.net/post/show";

        private DiscordSocketClient _client;

        private List<Subscription> _subscriptions = new List<Subscription>();

        public e6Service(HttpClient http, DiscordSocketClient client)
        {
            _http = http;
            _http.DefaultRequestHeaders.Add("User-Agent", "Discord e6PollerBot/1.0 (by nekofelix on e621)");
            _client = client;
        }

        public async Task<bool> SubscribeIsPrivate(string searchQuery, ulong userId)
        {
            if (searchQuery.Count() >= _max_search_query_size) return false;

            Subscription subscription = new Subscription();
            subscription.SearchQuery = searchQuery;
            subscription.IsPrivate = true;
            subscription.UserId = userId;

            _subscriptions.Add(subscription);
            return true;
        }

        public async Task<bool> SubscribeNotPrivate(string searchQuery, ulong userId, ulong channelId, ulong guildId)
        {
            if (searchQuery.Count() >= _max_search_query_size) return false;

            Subscription subscription = new Subscription();
            subscription.SearchQuery = searchQuery;
            subscription.IsPrivate = false;
            subscription.UserId = userId;
            subscription.ChannelId = channelId;
            subscription.GuildId = guildId;

            _subscriptions.Add(subscription);
            return true;
        }

        public async Task SendToSub(ulong userId)
        {
            foreach (Subscription subscription in _subscriptions)
            {
                if (userId == subscription.UserId)
                {
                    string replyString = $"Hello <@!{userId}>";
                    if (subscription.IsPrivate)
                    {
                        IDMChannel channel = await _client.GetUser(userId).GetOrCreateDMChannelAsync();
                        await channel.SendMessageAsync(replyString);
                    }
                    else
                    {
                        await _client.GetGuild(subscription.GuildId).GetTextChannel(subscription.ChannelId).SendMessageAsync(replyString);
                    }
                }
            }
        }

        public async Task<List<string>> ListSubscriptions(ulong userId)
        {
            ulong counter = 0;
            List<string> subs = new List<string>();
            List<Subscription> subsriptions = _subscriptions.Where(s => s.UserId == userId).ToList();

            foreach (Subscription subscription in subsriptions)
            {
                string subscriptionInfo = $"{counter}.";
                if (subscription.IsPrivate)
                {
                    subscriptionInfo = $"{subscriptionInfo} [PRIVATE MESSAGE]";
                }
                else
                {
                    string guildName = _client.GetGuild(subscription.GuildId).Name;
                    string channelName = _client.GetGuild(subscription.GuildId).GetTextChannel(subscription.ChannelId).Name;
                    subscriptionInfo = $"{subscriptionInfo} [{guildName} - {channelName}]";
                }
                subscriptionInfo = $"{subscriptionInfo} {subscription.SearchQuery}";
                subs.Add(subscriptionInfo);
                counter++;
            }
            return subs;
        }

        public async Task<bool> DeleteSubscription(int sub_to_delete, ulong userId)
        {
            List<Subscription> subs = _subscriptions.Where(s => s.UserId == userId).ToList();

            if (sub_to_delete >= subs.Count())
            {
                return false;
            }
            else
            {
                subs.RemoveAt(sub_to_delete);
                return true;
            }
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
