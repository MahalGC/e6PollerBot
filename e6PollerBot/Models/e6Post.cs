using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace e6PollerBot.Models
{
    public class e6Post
    {
        // This is id, rather than e6PostId, because it's deserialized from a JSON, and this is the key they use.
        [Key]
        public int id { get; set; }

        public ICollection<e6PostSubscription> e6PostSubscriptions { get; set; } 

        public e6Post()
        {
            this.e6PostSubscriptions = new HashSet<e6PostSubscription>();
        }
    }
}
