using GensouSakuya.KookBot.App.Interfaces;
using GensouSakuya.KookBot.App.Models;
using GensouSakuya.KookBot.App.Models.Enum;
using Kook;
using Kook.Rest;
using Kook.WebSocket;
using Microsoft.Extensions.Logging;

namespace GensouSakuya.KookBot.App.Services.KookService
{
    internal class KookService : IKookService
    {
        readonly ILogger<HandlerCenter> _logger;
        readonly KookRestClient _restClient;
        readonly KookSocketClient _socketClient;
        readonly IConfigService _configService;

        public KookService(IConfigService configService, ILogger<HandlerCenter> logger)
        {
            _logger = logger;
            _configService = configService;
            _socketClient = new KookSocketClient();
            _socketClient.MessageReceived += InternalSocketMessageReceived;
            _socketClient.DirectMessageReceived += InternalSocketDirectMessageReceived;
            _restClient = new KookRestClient();
        }

        public event Func<string, EnumKookMessageSource, BaseKookUser, BaseKookChannel, Task>? MessageReceived;

        public async Task LoginAsync()
        {
            var token = _configService.Get<string>("token");
            await _restClient.LoginAsync(Kook.TokenType.Bot, token);
            await _socketClient.LoginAsync(Kook.TokenType.Bot, token);
        }

        public Task StartAsync()
        {
            return _socketClient.StartAsync();
        }

        public async Task SendToGuild(string? message, ulong id)
        {
            var channel = await _restClient.GetChannelAsync(id);
            if (channel?.GetChannelType() != ChannelType.Text || channel is not ITextChannel tc)
                return;
            await tc.SendTextAsync(message);
        }

        public async Task SendToUser(string? message, Guid chatCode)
        {
            var channel = await _restClient.GetDMChannelAsync(chatCode);
            await channel.SendTextAsync(message);
        }

        private Task InternalSocketMessageReceived(SocketMessage arg1, SocketGuildUser arg2, SocketTextChannel arg3)
        {
            _logger.LogDebug("message received:{0}", arg1);
            if (MessageReceived == null)
                return Task.CompletedTask;
            return MessageReceived.Invoke(arg1.RawContent, EnumKookMessageSource.Guild, new BaseKookUser(), new KookGuildChannel(arg3.Id));
        }

        private Task InternalSocketDirectMessageReceived(SocketMessage arg1, SocketUser arg2, SocketDMChannel arg3)
        {
            _logger.LogDebug("direct message received:{0}", arg1);
            if (MessageReceived == null)
                return Task.CompletedTask;
            return MessageReceived.Invoke(arg1.RawContent, EnumKookMessageSource.Direct, new BaseKookUser(), new KookDMChannel(arg3.ChatCode));
        }
    }
}
