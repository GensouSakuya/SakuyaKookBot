using GensouSakuya.KookBot.App.BackgroundWorkers;
using GensouSakuya.KookBot.App.Handlers.Base;
using GensouSakuya.KookBot.App.Interfaces;
using GensouSakuya.KookBot.App.Models;
using GensouSakuya.KookBot.App.Models.Enum;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GensouSakuya.KookBot.App.Handlers
{
    [CommandTrigger("weibo")]
    internal class WeiboHandler : BaseDataHandler<ConcurrentDictionary<string, ConcurrentDictionary<string, WeiboSubscribeModel>>>
    {
        private WeiboWorker _worker;
        public WeiboHandler(IDataService dataService, IKookService kookService, ILogger logger) : base(dataService,kookService, logger)
        {
            _worker = new WeiboWorker(kookService, logger, GetData);
        }

        public override Task<bool> Check(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            return Task.FromResult(true);
        }

        public override async Task NextAsync(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            WeiboSubscribeModel sbm;
            if (source == EnumKookMessageSource.Guild)
            {
                //if (sendBy != admin)
                //{
                //    MessageManager.SendToSource(source, "目前只有机器人管理员可以配置该功能哦");
                //    return;
                //}

                sbm = new WeiboSubscribeModel
                {
                    Source = EnumKookMessageSource.Guild,
                    SourceId = sendFrom.ToGuildChannel().Id.ToString()
                };
            }
            else if(source == EnumKookMessageSource.Direct)
            {
                sbm = new WeiboSubscribeModel
                {
                    Source = EnumKookMessageSource.Direct,
                    SourceId = sendFrom.ToDMChannel().ChatCode.ToString()
                };
            }
            else
            {
                await SendBack(source, "懒得支持！", sendFrom);
                return;
            }

            var commands = MessageToCommand(originMessage);
            if (commands.Count < 3)
            {
                return;
            }

            var first = commands[1];
            var roomId = commands[2];

            var subscribers = GetData();
            if (first == "subscribe")
            {
                var sub = subscribers.GetOrAdd(roomId, new ConcurrentDictionary<string, WeiboSubscribeModel>());
                if (sub.ContainsKey(sbm.ToString()))
                {
                    await SendBack(source, "该微博已订阅", sendFrom);
                    return;
                }

                sub[sbm.ToString()] = sbm;
                await SendBack(source, "订阅成功！", sendFrom);
                SaveData(subscribers);
            }
            else if (first == "unsubscribe")
            {
                if (!subscribers.TryGetValue(roomId, out var sub))
                {
                    return;
                }
                if(sub == null)
                {
                    subscribers.TryRemove(roomId, out _);
                    SaveData(subscribers);
                    return;
                }
                if (sub.Remove(sbm.ToString()!, out _))
                {
                    await SendBack(source, "取消订阅成功！", sendFrom);
                    SaveData(subscribers);
                }
                return;
            }

            return;
        }
    }

    internal class WeiboSubscribeModel
    {
        public EnumKookMessageSource Source { get; set; }
        public required string SourceId { get; set; }

        public override bool Equals(object obj)
        {
            return obj is WeiboSubscribeModel model &&
                   Source == model.Source &&
                   SourceId == model.SourceId;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Source, SourceId);
        }

        public override string ToString()
        {
            return $"{Source}:{SourceId}";
        }
    }
}
