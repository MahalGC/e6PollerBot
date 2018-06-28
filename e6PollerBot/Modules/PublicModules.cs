using Discord.Commands;
using e6PollerBot.Services;
using System.Threading.Tasks;

namespace e6PollerBot.Modules
{
    // Modules must be public and inherit from an IModuleBase
    public class PublicModule : ModuleBase<SocketCommandContext>
    {
        // Dependency Injection will fill this value in for us
        public e6Service e6Service { get; set; }

        [Command("random")]
        public async Task Random([Remainder] string arg = null)
        {
            string random_url = await e6Service.GetRandomPicture(arg);
            await ReplyAsync(random_url);
        }
    }
}
