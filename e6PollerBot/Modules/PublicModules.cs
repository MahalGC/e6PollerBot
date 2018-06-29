using Discord.Commands;
using e6PollerBot.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace e6PollerBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        private static readonly int _max_reply_list_lines = 5;

        // Dependency Injection will fill this value in for us
        public e6Service e6Service { get; set; }

        [Command("random", RunMode = RunMode.Async)]
        [Alias("rand", "r")]
        public async Task Random([Remainder] string args = null)
        {
            string random_picture_data = await e6Service.GetRandomPicture(args);
            await ReplyAsync(random_picture_data);
        }

        [Command("Subscribe", RunMode = RunMode.Async)]
        [Alias("sub")]
        public async Task Subscribe([Remainder] string args = null)
        {
            bool isSuccessful;
            if (Context.IsPrivate)
            {
                isSuccessful = await e6Service.SubscribeIsPrivate(searchQuery: args, userId: Context.User.Id);
                
            }
            else
            {
                isSuccessful = await e6Service.SubscribeNotPrivate(searchQuery: args, userId: Context.User.Id, channelId: Context.Channel.Id, guildId: Context.Guild.Id);
            }
            
            if(isSuccessful)
            {
                await ReplyAsync($"<@!{Context.User.Id}>, you have successfully subscribed with the Search Query: [{args}]");
            }
            else
            {
                await ReplyAsync($"<@!{Context.User.Id}>, you have FAILED to subscribe with the Search Query: [{args}]");
            }
        }

        [Command("ListSubscriptions", RunMode = RunMode.Async)]
        [Alias("list", "ls")]
        public async Task ListSubscriptions([Remainder] string args = null)
        {
            List<string> subs = await e6Service.ListSubscriptions(Context.User.Id);

            string replyString = "";
            int counter = 0;
            foreach (string sub in subs)
            {
                replyString = $"{replyString}\n{sub}";
                counter++;
                if (counter >= _max_reply_list_lines)
                {
                    await ReplyAsync(replyString);
                    replyString = "";
                    counter = 0;
                }
            }

            if(!String.IsNullOrWhiteSpace(replyString))
            {
                await ReplyAsync(replyString);
            }

            if(subs.Count == 0)
            {
                await ReplyAsync("You have 0 Subscriptions.");
            }
        }

        [Command("DeleteSubscription", RunMode = RunMode.Async)]
        [Alias("unsub", "rm")]
        public async Task DeleteSubscription([Remainder] string args = null)
        {
            int sub_to_delete = 0;
            bool successful_conversion = true;
            try
            {
                sub_to_delete = Int32.Parse(args);
            }
            catch (Exception)
            {
                successful_conversion = false;
            }

            if (!successful_conversion)
            {
                await ReplyAsync($"The following Subscription Number could not be found or was empty: [{args}]");
                return;
            }
            
            bool isSuccessful = await e6Service.DeleteSubscription(sub_to_delete: sub_to_delete, userId: Context.User.Id);
            if (isSuccessful)
            {
                await ReplyAsync($"<@!{Context.User.Id}>, you have successfully deleted Subscription Number [{sub_to_delete}].");
            }
            else
            {
                await ReplyAsync($"<@!{Context.User.Id}>, you have FAILED to delete Subscription Number [{sub_to_delete}]");
            }
        }
    }
}
