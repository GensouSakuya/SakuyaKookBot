using GensouSakuya.KookBot.App.Handlers;
using GensouSakuya.KookBot.App.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.Collections.Concurrent;
using System.Net;
using System.Text.RegularExpressions;

namespace GensouSakuya.KookBot.App.BackgroundWorkers
{
    internal class WeiboWorker
    {
        readonly Func<ConcurrentDictionary<string, ConcurrentDictionary<string, WeiboSubscribeModel>>> _getSubscribeData;
        readonly CancellationTokenSource _cancellationTokenSource;
        readonly IKookService _kookService;
        readonly ILogger _logger;
        public WeiboWorker(IKookService kookService, ILogger logger, Func<ConcurrentDictionary<string, ConcurrentDictionary<string, WeiboSubscribeModel>>> getSubscribeData)
        {
            _kookService = kookService;
            _logger = logger;
            _lastWeiboId = new ConcurrentDictionary<string, string>();
            _cancellationTokenSource = new CancellationTokenSource();
            _getSubscribeData = getSubscribeData;
            Task.Run(() => LoopCheck(_cancellationTokenSource.Token));
        }

        //正则参考rsshub：https://github.com/DIYgod/RSSHub/blob/master/lib/routes/weibo/utils.ts
        static Regex _faceRegex = new Regex("<span class=[\"']url-icon[\"']><img\\s[^>]*?alt=[\"']?([^>]+?)[\"']?\\s[^>]*?\\/?><\\/span>");
        static Regex _newLineRegex = new Regex("<br\\s/>");
        static Regex _fullTextRegex = new Regex("<a href=\"(.*?)\">全文<\\/a>");
        static Regex _repostRegex = new Regex("<a href='\\/n\\/(.*?)'>(.*?)<\\/a>");
        private static TaskCompletionSource<bool> _completionSource = new TaskCompletionSource<bool>();
        private ConcurrentDictionary<string, string> _lastWeiboId;
        private async Task LoopCheck(CancellationToken token)
        {
            try
            {
                await Task.Delay(5000);
                var loopSpan = new TimeSpan(0, 10, 0);
                var intervalSpan = new TimeSpan(0, 0, 10);
                var templateUrl = "https://m.weibo.cn/api/container/getIndex?type=uid&value={0}";
                RestResponse homeres = null;
                var cookieContainer = new CookieContainer();
                while (!token.IsCancellationRequested)
                {
                    if (_completionSource.Task.IsCompleted)
                        _completionSource = new TaskCompletionSource<bool>();
                    var option = new RestClientOptions()
                    {
                        ConfigureMessageHandler = h =>
                        {
                            var handler = (HttpClientHandler)h;
                            handler.CookieContainer = cookieContainer;
                            handler.UseCookies = true;
                            return handler;
                        }
                    };
                    using (var client = new RestClient(option))
                    {
                        //try
                        //{
                        //    client.AddDefaultHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36 Edg/119.0.0.0");
                        //    //set cookie
                        //    homeres = await client.GetAsync(new RestRequest("https://live.douyin.com"));
                        //}
                        //catch (Exception e)
                        //{
                        //    _logger.LogError(e, "douyin refresh cookies error");
                        //}
                        var subscribers = _getSubscribeData();
                        foreach (var room in subscribers)
                        {
                            if (room.Value.Count <= 0)
                                continue;

                            try
                            {
                                var url = string.Format(templateUrl, room.Key);
                                var res = await client.GetAsync(new RestRequest(url));
                                if (!res.IsSuccessStatusCode)
                                {
                                    _logger.LogError(res.ErrorException, "get roominfo failed");
                                    continue;
                                }
                                var content = res.Content;
                                var jobj = JObject.Parse(content);
                                var name = jobj["data"]["userInfo"]["screen_name"];
                                var containerid = jobj["data"]["tabsInfo"]["tabs"][1]["containerid"];
                                res = await client.ExecuteGetAsync(new RestRequest(url + "&containerid=" + containerid));
                                content = res.Content;
                                jobj = JObject.Parse(content);
                                var weibos = jobj["data"]["cards"];
                                var newest = weibos[0];
                                var id = newest["mblog"]["id"].ToString();
                                var text = newest["mblog"]["text"].ToString();
                                var images = newest["mblog"]["pic_ids"].ToArray();
                                var retweeted = newest["mblog"]["retweeted_status"];
                                if (!_lastWeiboId.ContainsKey(room.Key))
                                {
                                    _lastWeiboId[room.Key] = id;
                                    continue;
                                }
                                else if (_lastWeiboId[room.Key] == id)
                                {
                                    continue;
                                }

                                _lastWeiboId[room.Key] = id;
                                _logger.LogInformation("weibo[{0}] start sending notice", room.Key);

                                var isRepost = retweeted != null;
                                text = _faceRegex.Replace(text, "$1");
                                text = _newLineRegex.Replace(text, Environment.NewLine);
                                text = _fullTextRegex.Replace(text, "[完整内容见原微博](https://m.weibo.cn$1)");
                                text = _repostRegex.Replace(text, "$2");

                                if(images?.Any()?? false)
                                {
                                    for(var index =0;index < images.Length;index++)
                                    {
                                        var image = images[index];
                                        text += $"{Environment.NewLine}[配图{index+1}](https://image.baidu.com/search/down?url=https://wx1.sinaimg.cn/large/{image}.jpg)";
                                    }
                                }

                                var msgBody = $"{Environment.NewLine}{text}";

                                var msg = "";
                                if (!isRepost)
                                {
                                    msg = $"【{name}】发布了微博：{msgBody}";
                                }
                                else
                                {
                                    msg = $"【{name}】转发了微博：{msgBody}{Environment.NewLine}原微博：{Environment.NewLine}@{retweeted["user"]["screen_name"]}：{retweeted["text"]}";
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
                            catch (Exception e)
                            {
                                _logger.LogError(e, "weibo failed to send msg");
                            }
                            finally
                            {
                                await Task.Delay(intervalSpan);
                            }
                        }
                    }
                    await Task.WhenAny(Task.Delay(loopSpan), _completionSource.Task);
                }
                _logger.LogInformation("loop finished");
            }
            catch (Exception e)
            {
                _logger.LogError(e, "weibo loop error");
            }

        }


        internal enum StreamType
        {
            PC = 0,
            Radio = 1
        }
    }
}
