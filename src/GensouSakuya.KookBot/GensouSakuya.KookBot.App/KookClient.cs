using GensouSakuya.KookBot.App.Interfaces;
using Kook.Rest;
using Kook.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GensouSakuya.KookBot.App
{
    internal class KookClient
    {
        readonly KookRestClient _restClient;
        readonly KookSocketClient _socketClient;
        readonly IConfigService _configService;

        public KookClient(IConfigService configService)
        {
            _configService = configService;
            _socketClient = new KookSocketClient();
            _restClient = new KookRestClient();
        }

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

        public async Task TestAsync()
        {
            //var guilds = await _restClient.GetGuildsAsync();
            //var guild = guilds.FirstOrDefault();
            //var channels = await guild.GetTextChannelsAsync();
            //var channel = channels.FirstOrDefault();
            //await channel.SendTextAsync("test");
        }
    }
}
