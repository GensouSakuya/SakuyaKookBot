using GensouSakuya.KookBot.App.Interfaces;
using GensouSakuya.KookBot.App.Models;
using GensouSakuya.KookBot.App.Models.Enum;
using Microsoft.Extensions.Logging;

namespace GensouSakuya.KookBot.App.Handlers.Base
{
    internal abstract class BaseHandler
    {
        readonly IKookService _kookService;
        public BaseHandler(IKookService kookService) 
        {
            _kookService = kookService;
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
    }

    internal class HandlerEngine
    {
        readonly ILogger _logger;

        public HandlerEngine(ILogger<HandlerEngine> logger)
        {
            _logger = logger;
        }

        public List<BaseHandler>? CommandHandlers { get; private set; }
        public List<BaseHandler>? FlowHandlers { get; private set; }

        public void SetCommandHandlers(List<BaseHandler> handlers)
        {
            CommandHandlers = handlers;
        }

        public void SetFlowHandlers(List<BaseHandler> handlers)
        {
            FlowHandlers = handlers;
        }

        public Task<bool> ExecuteCommandAsync(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            return ExecuteAsync(CommandHandlers, source, originMessage, sendBy, sendFrom);
        }

        public Task<bool> ExecuteFlowAsync(EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            return ExecuteAsync(FlowHandlers,source, originMessage,sendBy, sendFrom);
        }

        private async Task<bool> ExecuteAsync(List<BaseHandler>? handlers, EnumKookMessageSource source, string? originMessage, BaseKookUser sendBy, BaseKookChannel sendFrom)
        {
            var handled = false;
            try
            {
                if (handlers == null)
                    return handled;

                foreach (var commander in handlers)
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
                _logger.LogError(e, "execute message error");
                return handled;
            }
        }
    }
}
