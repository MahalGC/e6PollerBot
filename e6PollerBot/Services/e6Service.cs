using Discord;
using Discord.WebSocket;
using e6PollerBot.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage;
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
        private static readonly int _max_reply_list_lines = 5; // Max number of lines in one post.
        private static readonly int _query_limit = 300; // Maximum number of e6 Posts to search for in one call.
        private static readonly int _throttle_timing = 1000; // Miliseconds to wait between e6 API calls.
        private static readonly int _max_search_query_character_size = 200; // Maximum character limit for Searches.
        private static readonly int _poller_thread_latency = 1000; // Miliseconds to wait before trying again with the Poller Thread.

        private static readonly string _e6_show_post_base_url = "https://e621.net/post/show";

        private readonly HttpClient _http;
        private DiscordSocketClient _client;
        private Task _pollerThread;

        private SemaphoreSlim _e6_throttle_guard = new SemaphoreSlim(1);

        public e6Service(HttpClient http, DiscordSocketClient client)
        {
            _http = http;
            _http.DefaultRequestHeaders.Add("User-Agent", Environment.GetEnvironmentVariable("USER_AGENT_HEADER"));
            _client = client;

            _pollerThread = Task.Factory.StartNew(() => this.PollerThread(), TaskCreationOptions.LongRunning);
        }

        // Subscribe via Private Message / Direct Message (PM/DM)
        public async Task<bool> SubscribeIsPrivate(string searchQuery, ulong userId)
        {
            if (searchQuery.Count() > _max_search_query_character_size) return false;

            Subscription subscription = new Subscription();
            subscription.IsNew = true;
            subscription.IsActive = true;
            subscription.SearchQuery = searchQuery;
            subscription.IsPrivate = true;
            subscription.UserId = userId;

            using (PollerBotDbContext dbcontext = new PollerBotDbContext())
            {
                await dbcontext.AddAsync<Subscription>(subscription);
                await dbcontext.SaveChangesAsync();
            }
            return true;
        }

        // Subscribe via Guild Channel / Server Channel
        public async Task<bool> SubscribeNotPrivate(string searchQuery, ulong userId, ulong channelId, ulong guildId)
        {
            if (searchQuery.Count() > _max_search_query_character_size) return false;

            Subscription subscription = new Subscription();
            subscription.IsNew = true;
            subscription.IsActive = true;
            subscription.SearchQuery = searchQuery;
            subscription.IsPrivate = false;
            subscription.UserId = userId;
            subscription.ChannelId = channelId;
            subscription.GuildId = guildId;

            using (PollerBotDbContext dbcontext = new PollerBotDbContext())
            {
                await dbcontext.AddAsync<Subscription>(subscription);
                await dbcontext.SaveChangesAsync();
            }
            return true;
        }

        // Get all Subscriptions for a user.
        public async Task<List<string>> ListSubscriptions(ulong userId)
        {
            ulong counter = 1;
            List<string> subs = new List<string>();
            List<Subscription> subscriptions;
            using (PollerBotDbContext dbcontext = new PollerBotDbContext())
            {
                subscriptions = await dbcontext.Subscriptions.Where(s => s.UserId == userId).Where(s => s.IsActive == true).OrderBy(s => s.SubscriptionId).ToListAsync();
            }

            foreach (Subscription subscription in subscriptions)
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

        // Delete a Subscription.
        public async Task<bool> DeleteSubscription(int sub_to_delete, ulong userId)
        {
            if (sub_to_delete <= 0) return false;
            sub_to_delete -= 1;
            bool isSuccessful = true;
            List<Subscription> subscriptions;
            using (PollerBotDbContext dbcontext = new PollerBotDbContext())
            {
                subscriptions = await dbcontext.Subscriptions.Where(s => s.UserId == userId).Where(s => s.IsActive == true).OrderBy(s => s.SubscriptionId).ToListAsync();
                if (sub_to_delete >= subscriptions.Count())
                {
                    isSuccessful = false;
                }
                else
                {
                    try
                    {
                        using (IDbContextTransaction dbContextTransaction = await dbcontext.Database.BeginTransactionAsync())
                        {
                            try
                            {
                                subscriptions[sub_to_delete].IsActive = false;
                                dbcontext.Subscriptions.Update(subscriptions[sub_to_delete]);
                                await dbcontext.SaveChangesAsync();
                                dbContextTransaction.Commit();
                            }
                            catch(Exception e)
                            {
                                Console.WriteLine(e);
                                dbContextTransaction.Rollback();
                            }
                        }
                    }
                    catch (Exception)
                    {
                        isSuccessful = false;
                    }
                    isSuccessful = true;
                }
            }
            return isSuccessful;
        }

        // Get a random picture from e6.
        public async Task<string> GetRandomPicture(string tags)
        {
            if (tags.Count() > _max_search_query_character_size) return $"Character limit of {_max_search_query_character_size} exceeded! You have input ${tags.Count()} characters.";

            List<e6Post> e6Posts = await Querye6ByTag(tags, _query_limit);
            List<int> ids = e6Posts.Select(x => x.id).ToList();
            if (ids.Count() == 0) return "No posts matched your search.";

            int random_index = RandomThreadSafe.Next(0, ids.Count());
            string return_string = $"{_e6_show_post_base_url}/{ids[random_index]}";
            return return_string;
        }

        // Query the GET e6 Posts List API
        private async Task<List<e6Post>> Querye6ByTag(string tags, int limit)
        {
            string query_string = $"https://e621.net/post/index.json?tags={tags}&limit={_query_limit}";
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

        // Poll e6 Periodically.
        private async Task PollerThread()
        {
            while (true)
            {
                try
                {
                    using (PollerBotDbContext dbcontext = new PollerBotDbContext())
                    {
                        List<Subscription> subscriptions = await dbcontext.Subscriptions.Where(s => s.IsActive == true).ToListAsync();

                        foreach(Subscription subscription in subscriptions)
                        {
                            if (subscription.IsNew)
                            {
                                List<e6Post> e6PostsRaw = await Querye6ByTag(subscription.SearchQuery, _query_limit);
                                HashSet<int> e6PostIds = e6PostsRaw.Select(x => x.id).ToHashSet();

                                using (IDbContextTransaction dbContextTransaction = await dbcontext.Database.BeginTransactionAsync())
                                {
                                    try
                                    {
                                        Subscription tempSub = await dbcontext.Subscriptions.SingleOrDefaultAsync(s => s.SubscriptionId == subscription.SubscriptionId);
                                        PropertyValues propertyValues = await dbcontext.Entry(tempSub).GetDatabaseValuesAsync();
                                        tempSub.IsActive = (bool)propertyValues["IsActive"];
                                        tempSub.e6Posts = e6PostIds;
                                        subscription.IsNew = false;
                                        dbcontext.Subscriptions.Update(tempSub);
                                        await dbcontext.SaveChangesAsync();
                                        dbContextTransaction.Commit();
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        dbContextTransaction.Rollback();
                                    }
                                }
                            }
                            else
                            {
                                List<e6Post> e6PostsRaw = await Querye6ByTag(subscription.SearchQuery, _query_limit);
                                HashSet<int> e6PostIds = e6PostsRaw.Select(x => x.id).ToHashSet();
                                IEnumerable<int> newIds = e6PostIds.Except(subscription.e6Posts);

                                await SendUpdates(subscription: subscription, newIds: newIds);

                                using (IDbContextTransaction dbContextTransaction = await dbcontext.Database.BeginTransactionAsync())
                                {
                                    try
                                    {
                                        Subscription tempSub = await dbcontext.Subscriptions.SingleOrDefaultAsync(s => s.SubscriptionId == subscription.SubscriptionId);
                                        PropertyValues propertyValues = await dbcontext.Entry(tempSub).GetDatabaseValuesAsync();
                                        tempSub.IsActive = (bool) propertyValues["IsActive"];
                                        tempSub.e6Posts.UnionWith(newIds);
                                        dbcontext.Subscriptions.Update(tempSub);
                                        await dbcontext.SaveChangesAsync();
                                        dbContextTransaction.Commit();
                                    }
                                    catch (Exception e)
                                    {
                                        Console.WriteLine(e);
                                        dbContextTransaction.Rollback();
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                finally
                {
                    Thread.Sleep(_poller_thread_latency);
                }
            }
        }

        private async Task SendUpdates(Subscription subscription, IEnumerable<int> newIds)
        {
            try
            {
                if (!newIds.Any()) return;
                string replyString = $"Hello <@!{subscription.UserId}>! You have an Update for [{subscription.SearchQuery}]!";
                int counter = 1;
                foreach (int id in newIds)
                {
                    replyString = $"{replyString}\n{_e6_show_post_base_url}/{id}";
                    counter++;
                    if (counter >= _max_reply_list_lines)
                    {
                        if (subscription.IsPrivate)
                        {
                            IDMChannel channel = await _client.GetUser(subscription.UserId).GetOrCreateDMChannelAsync();
                            await channel.SendMessageAsync(replyString);
                        }
                        else
                        {
                            await _client.GetGuild(subscription.GuildId).GetTextChannel(subscription.ChannelId).SendMessageAsync(replyString);
                        }
                        replyString = "";
                        counter = 0;
                    }
                }

                if (!String.IsNullOrWhiteSpace(replyString))
                {
                    if (subscription.IsPrivate)
                    {
                        IDMChannel channel = await _client.GetUser(subscription.UserId).GetOrCreateDMChannelAsync();
                        await channel.SendMessageAsync(replyString);
                    }
                    else
                    {
                        await _client.GetGuild(subscription.GuildId).GetTextChannel(subscription.ChannelId).SendMessageAsync(replyString);
                    }
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}
