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
        // Dependency Injection will fill this value in for us
        public e6Service e6Service { get; set; }

        [Command("random", RunMode = RunMode.Async)]
        public async Task Random([Remainder] string args = null)
        {
            string random_picture_data = await e6Service.GetRandomPicture(args);
            await ReplyAsync(random_picture_data);
        }

        [Command("Subscribe")]
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
                await ReplyAsync($"You have successfully subscribed with: user - <@!{Context.User.Id}>");
            }
            else
            {
                await ReplyAsync($"FAILED TO SUBSCRIBE with: user - <@!{Context.User.Id}>");
            }
        }

        [Command("SendToSub")]
        public async Task PingSub([Remainder] string arg = null)
        {
            await e6Service.SendToSub(Context.User.Id);
        }

        [Command("ListSubscriptions")]
        public async Task ListSubscriptions([Remainder] string args = null)
        {
            List<string> subs = await e6Service.ListSubscriptions(Context.User.Id);

            string replyString = "";
            int counter = 0;
            foreach (string sub in subs)
            {
                replyString = $"{replyString}\n{sub}";
                counter++;
                if (counter >= 5)
                {
                    await ReplyAsync(replyString);
                    replyString = "";
                }
            }

            if(!String.IsNullOrWhiteSpace(replyString))
            {
                await ReplyAsync(replyString);
            }
        }

        [Command("DeleteSubscription")]
        public async Task DeleteSubscription([Remainder] string args = null)
        {
            int sub_to_delete = 0;
            bool successful_conversion = true;
            try
            {
                sub_to_delete = Int32.Parse(args);
            }
            catch(Exception e)
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
                await ReplyAsync($"You have successfully deleted with index {sub_to_delete}: user - <@!{Context.User.Id}>");
            }
            else
            {
                await ReplyAsync($"FAILED TO DELETE with index {sub_to_delete}: user - <@!{Context.User.Id}>");
            }
        }
    }
}
