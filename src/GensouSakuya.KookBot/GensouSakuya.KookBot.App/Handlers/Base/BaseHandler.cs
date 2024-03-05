using GensouSakuya.KookBot.App.Interfaces;
using GensouSakuya.KookBot.App.Models;
using GensouSakuya.KookBot.App.Models.Enum;
using Microsoft.Extensions.Logging;

namespace GensouSakuya.KookBot.App.Handlers.Base
{
    internal abstract class BaseHandler
    {
        readonly IKookService _kookService;
        protected ILogger Logger;
        public BaseHandler(IKookService kookService, ILogger logger) 
        {
            _kookService = kookService;
            Logger = logger;
        }

        public abstract Task<bool> Check(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom);

        public bool IsHandled { get; private set; }

        protected void StopChain()
        {
            this.IsHandled = true;
        }

        public abstract Task NextAsync(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom);

        public Task SendBack(EnumKookMessageSource source, string? message, BaseKookChannel sendFrom)
        {
            if(source == EnumKookMessageSource.Guild)
            {
                return _kookService.SendToGuild(message, ((KookGuildChannel)sendFrom).Id);
            }
            else if(source == EnumKookMessageSource.Direct)
            {
                return _kookService.SendToUser(message, ((KookDMChannel)sendFrom).ChatCode);
            }
            return Task.CompletedTask;
        }

        protected List<string> MessageToCommand(string? originMessage)
        {
            if (string.IsNullOrWhiteSpace(originMessage))
                return Enumerable.Empty<string>().ToList();
            return originMessage.Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }

    internal abstract class BaseDataHandler<T> : BaseHandler where T:new()
    {
        readonly IDataService _dataService;

        protected BaseDataHandler(IDataService dataService, IKookService kookService, ILogger logger) : base(kookService, logger)
        {
            _dataService = dataService;
        }

        protected T GetData()
        {
            var keyName = this.GetType().Name;
            var data = _dataService.Get<T>(keyName);
            if(data == null)
            {
                data = new T();
                _dataService.Save(keyName, data, false);
            }
            return data;
        }

        protected void SaveData(T data, bool immediately = true)
        {
            var keyName = this.GetType().Name;
            _dataService.Save(keyName, data, immediately);
        }
    }

    internal class HandlerEngine
    {
        readonly ILogger _logger;

        public HandlerEngine(ILogger<HandlerEngine> logger)
        {
            _logger = logger;
        }

        public Dictionary<string, BaseHandler>? CommandHandlers { get; private set; }
        public List<BaseHandler>? FlowHandlers { get; private set; }

        public void SetCommandHandlers(Dictionary<string, BaseHandler> handlers)
        {
            CommandHandlers = handlers;
        }

        public void SetFlowHandlers(List<BaseHandler> handlers)
        {
            FlowHandlers = handlers;
        }

        public async Task<bool> ExecuteCommandAsync(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            var handled = false;
            try
            {
                if (CommandHandlers == null)
                    return handled;

                var command = originMessage?.Split(" ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()?.Substring(1);

                if(!string.IsNullOrWhiteSpace(command) && CommandHandlers.ContainsKey(command))
                {
                    var commandHandler = CommandHandlers[command];

                    if (!await commandHandler.Check(source, originMessage, sendBy, sendFrom))
                        return handled;

                    await commandHandler.NextAsync(source, originMessage, sendBy, sendFrom);
                    if (commandHandler.IsHandled)
                    {
                        handled = true;
                    }
                }
                return handled;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "execute command message error");
                return handled;
            }
        }

        public async Task<bool> ExecuteFlowAsync(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            var handled = false;
            try
            {
                if (FlowHandlers == null)
                    return handled;

                foreach (var commander in FlowHandlers)
                {
                    if (!await commander.Check(source, originMessage, sendBy, sendFrom))
                        continue;

                    await commander.NextAsync(source, originMessage, sendBy, sendFrom);

                    if (commander.IsHandled)
                    {
                        handled = true;
                        break;
                    }
                }
                return handled;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "execute flow message error");
                return handled;
            }
        }
    }
}
