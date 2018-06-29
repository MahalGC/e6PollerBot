namespace e6PollerBot.Models
{
    public class e6PostSubscription
    {
        public int e6PostId { get; set; }
        public e6Post e6Post { get; set; }

        public int SubscriptionId { get; set; }
        public Subscription Subscription { get; set; }
    }
}
