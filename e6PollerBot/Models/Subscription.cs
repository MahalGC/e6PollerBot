namespace e6PollerBot.Models
{
    public class Subscription
    {
        public bool IsPrivate { get; set; }

        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }

        public string SearchQuery { get; set; }
    }
}
