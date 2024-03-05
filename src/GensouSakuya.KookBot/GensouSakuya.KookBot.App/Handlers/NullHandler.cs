using GensouSakuya.KookBot.App.Handlers.Base;
using GensouSakuya.KookBot.App.Interfaces;
using GensouSakuya.KookBot.App.Models;
using GensouSakuya.KookBot.App.Models.Enum;

namespace GensouSakuya.KookBot.App.Handlers
{
    [CommandTrigger("null")]
    internal class NullHandler : BaseHandler
    {
        public NullHandler(IKookService kookService) : base(kookService)
        {
        }

        public override Task<bool> Check(EnumKookMessageSource source, string originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            return Task.FromResult(true);
        }

        public override async Task NextAsync(EnumKookMessageSource source, string originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            await SendBack(source, "略略略😝", sendFrom);
            StopChain();
        }
    }
}
