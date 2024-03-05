namespace GensouSakuya.KookBot.App.Models
{
    internal class KookGuildChannel: BaseKookChannel
    {
        public ulong Id { get; set; }

        public KookGuildChannel(ulong id)
        {
            Id = id;
        }
    }
}
