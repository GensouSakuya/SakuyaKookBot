using GensouSakuya.KookBot.App.Handlers;
using GensouSakuya.KookBot.App.Interfaces;
using Kook;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Concurrent;
using System.Net;

namespace GensouSakuya.KookBot.App.BackgroundWorkers
{
    internal class DouyinWorker
    {
        readonly Func<ConcurrentDictionary<ulong, ConcurrentDictionary<string, DouyinSubscribeModel>>> _getSubscribeData;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly IKookService _kookService;
        readonly ILogger _logger;
        public DouyinWorker(IKookService kookService, ILogger logger, Func<ConcurrentDictionary<ulong, ConcurrentDictionary<string, DouyinSubscribeModel>>> getSubscribeData)
        {
            _kookService = kookService;
            _logger = logger;
            _notFireAgainList = new ConcurrentDictionary<ulong, ulong>();
            _cancellationTokenSource = new CancellationTokenSource();
            _getSubscribeData = getSubscribeData;
            Task.Run(() => LoopCheck(_cancellationTokenSource.Token));
        }

        private static TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        private ConcurrentDictionary<ulong, ulong> _notFireAgainList;
        private async Task LoopCheck(CancellationToken token)
        {
            try
            {
                await Task.Delay(5000);
                var loopSpan = new TimeSpan(0, 1, 0);
                var intervalSpan = new TimeSpan(0, 0, 10);
                var templateUrl = "https://live.douyin.com/webcast/room/web/enter/?aid=6383&app_name=douyin_web&live_id=1&device_platform=web&language=zh-CN&cookie_enabled=true&screen_width=2048&screen_height=1152&browser_language=zh-CN&browser_platform=Win32&browser_name=Edge&browser_version=119.0.0.0&web_rid={0}";
                RestResponse homeres = null;
                while (!token.IsCancellationRequested)
                {
                    if (_completionSource.Task.IsCompleted)
                        _completionSource = new TaskCompletionSource<bool>();
                    using (var client = new RestClient())
                    {
                        try
                        {
                            client.AddDefaultHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0");
                            //set cookie
                            homeres = await client.GetAsync(new RestRequest("https://live.douyin.com"));
                        }
                        catch (Exception e)
                        {
                            _logger.LogError(e, "douyin refresh cookies error");
                        }
                        var subscribers = _getSubscribeData();
                        foreach (var room in subscribers)
                        {
                            if (room.Value.Count <= 0)
                                continue;

                            try
                            {
                                var url = string.Format(templateUrl, room.Key);
                                var cookieContainer = new CookieContainer();
                                foreach(var cookie in homeres.Cookies)
                                {
                                    cookieContainer.Add((Cookie)cookie);
                                }
                                var res = await client.GetAsync(new RestRequest(url) { CookieContainer = cookieContainer });
                                if (!res.IsSuccessStatusCode)
                                {
                                    _logger.LogError(res.ErrorException, "get roominfo failed");
                                    continue;
                                }
                                var content = res.Content;
                                var jsonRes = Newtonsoft.Json.JsonConvert.DeserializeObject(content);
                                var jobj = JObject.FromObject(jsonRes);
                                var name = jobj["data"]["user"]["nickname"];
                                var jdata = jobj["data"]["data"];
                                string title = null;
                                var isStreaming = false;
                                StreamType type;
                                if (jdata.HasValues)
                                {
                                    var status = jobj["data"]["data"][0]["status"].Value<int>();
                                    title = jobj["data"]["data"][0]["title"].Value<string>();
                                    if (status == 2)
                                    {
                                        isStreaming = true;
                                    }
                                    type = StreamType.PC;
                                }
                                else
                                {
                                    //拿不到data的时候额外再请求一次，以确保真的是在进行电台直播而非数据异常
                                    _logger.LogInformation("data is empty, full response:{0}", content);
                                    var res2 = await client.GetAsync(new RestRequest(url));
                                    if (!res2.IsSuccessStatusCode)
                                    {
                                        _logger.LogError(res2.ErrorException, "get roominfo failed");
                                        continue;
                                    }
                                    var content2 = res2.Content;
                                    var jsonRes2 = Newtonsoft.Json.JsonConvert.DeserializeObject(content2);
                                    var jobj2 = JObject.FromObject(jsonRes2);
                                    if (!jobj["data"]["data"].HasValues)
                                    {
                                        isStreaming = true;
                                    }
                                    type = StreamType.Radio;
                                }
                                if (isStreaming)
                                {
                                    if (_notFireAgainList.ContainsKey(room.Key))
                                        continue;
                                    _notFireAgainList.TryAdd(room.Key, room.Key);
                                    _logger.LogInformation("douyin[{0}] start sending notice", room.Key);

                                    string msg;
                                    if (type == StreamType.Radio)
                                    {
                                        msg = $"【{name}】开始了电台直播，请使用手机APP观看";
                                    }
                                    else
                                    {
                                        msg = $"【{name}】开播了：[{title}](https://live.douyin.com/{room.Key})";
                                    }

                                    foreach (var sor in room.Value)
                                    {
                                        var sorModel = sor.Value;
                                        if (sorModel.Source == Models.Enum.EnumKookMessageSource.Guild)
                                            await _kookService.SendToGuild(msg, ulong.Parse(sorModel.SourceId));
                                        else if (sorModel.Source == Models.Enum.EnumKookMessageSource.Direct)
                                            await _kookService.SendToUser(msg, Guid.Parse(sorModel.SourceId));
                                        else
                                            continue;

                                        await Task.Delay(10000);
                                    }
                                }
                                else
                                {
                                    if (_notFireAgainList.TryRemove(room.Key, out _))
                                    {
                                        _logger.LogInformation("douyin[{0}] flag removed", room.Key);
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e, "douyin failed to send msg");
                            }
                            finally
                            {
                                await Task.Delay(intervalSpan);
                            }
                        }
                    }
                    await Task.WhenAny(Task.Delay(loopSpan), _completionSource.Task);
                }
                _logger.LogError("loop finished");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "douyin loop error");
            }
        }


        internal enum StreamType
        {
            PC = 0,
            Radio = 1
        }
    }
}
