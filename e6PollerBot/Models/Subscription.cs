using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace e6PollerBot.Models
{
    public class Subscription
    {
        [Key]
        public int SubscriptionId { get; set; }

        public bool IsActive { get; set; }
        public bool IsNew { get; set; }

        public bool IsPrivate { get; set; }

        public ulong UserId { get; set; }
        public ulong ChannelId { get; set; }
        public ulong GuildId { get; set; }

        public string SearchQuery { get; set; }

        [NotMapped]
        public HashSet<int> e6Posts { get; set; } = new HashSet<int>();

        [Obsolete("Do not query this. Query e6Posts instead.")]
        public string e6PostMetaData
        {
            get
            {
                if (e6Posts == null || !e6Posts.Any())
                {
                    return null;
                }
                else
                {
                    return JsonConvert.SerializeObject(e6Posts);
                }
            }

            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    e6Posts.Clear();
                }
                else
                {
                    e6Posts = JsonConvert.DeserializeObject<HashSet<int>>(value);
                }
            }
        }
    }
}
