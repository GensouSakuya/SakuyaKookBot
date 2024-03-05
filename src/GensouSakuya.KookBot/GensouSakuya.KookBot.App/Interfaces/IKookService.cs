using GensouSakuya.KookBot.App.Models;
using GensouSakuya.KookBot.App.Models.Enum;

namespace GensouSakuya.KookBot.App.Interfaces
{
    internal interface IKookService
    {
        event Func<string, EnumKookMessageSource, BaseKookUser, BaseKookChannel, Task> MessageReceived;

        Task LoginAsync();
        Task StartAsync();

        Task SendToGuild(string? message, ulong id);

        Task SendToUser(string? message, Guid chatCode);
    }
}
