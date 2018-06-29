using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace e6PollerBot.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        public bool IsPrivate { get; set; }

        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }

        public string SearchQuery { get; set; }

        public ICollection<e6PostSubscription> e6PostSubscriptions { get; set; }

        public Subscription()
        {
            this.e6PostSubscriptions = new HashSet<e6PostSubscription>();
        }
    }
}
