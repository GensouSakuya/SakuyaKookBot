namespace GensouSakuya.KookBot.App.Models
{
    internal class KookDMChannel: BaseKookChannel
    {
        public Guid ChatCode { get; set; }

        public KookDMChannel(Guid chatCode)
        {
            ChatCode = chatCode;
        }
    }
    internal static class KookDMChannelExtension
    {
        public static KookDMChannel ToDMChannel(this BaseKookChannel kc)
        {
            return (KookDMChannel)kc;
        }
    }
}
