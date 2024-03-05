using GensouSakuya.KookBot.App.Handlers.Base;
using GensouSakuya.KookBot.App.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace GensouSakuya.KookBot.App
{
    internal class HandlerCenter
    {
        readonly ILogger _logger;
        readonly IKookService _kookService;
        readonly HandlerEngine _engine;
        public HandlerCenter(IKookService kookService, ILogger<HandlerCenter> logger, HandlerEngine engine) 
        {
            _kookService = kookService;
            _logger = logger;
            _engine = engine;
            _kookService.MessageReceived += InternalKookMessageReceived;
        }

        private async Task InternalKookMessageReceived(string? message, Models.Enum.EnumKookMessageSource arg2, Models.BaseKookUser arg3, Models.BaseKookChannel arg4)
        {
            var command = message?.Trim().Split(" ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

            if(command!=null && (command.StartsWith(".") || command.StartsWith("/")))
            {
                await _engine.ExecuteCommandAsync(arg2, message, arg3, arg4);
            }
            else
            {
                await _engine.ExecuteFlowAsync(arg2, message, arg3, arg4);
            }
        }

        public void ReloadHandlers()
        {
            _logger.LogDebug("ReloadHandlers");
            //只能拿到直接继承的派生类，有需要的时候再改
            var handlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(p => p.BaseType == typeof(BaseHandler) && !p.IsAbstract).ToList();
            _logger.LogDebug($"found {handlerTypes.Count} handlers");

            var allHandlers = handlerTypes.Select(p => new
            {
                Type = p,
                CommandList = p.GetCustomAttributes<CommandTriggerAttribute>().ToList(),
                FlowList = p.GetCustomAttributes<FlowTriggerAttribute>().ToList()
            });
            var commandHandlers = allHandlers.Where(p => p.CommandList.Any());
            var flowHandlers = allHandlers.Where(p => p.FlowList.Any());

            _engine.SetFlowHandlers(flowHandlers.Select(p =>
            {
                return (BaseHandler)Activator.CreateInstance(p.Type, new[] { _kookService })!;
            }).ToList());

            _engine.SetCommandHandlers(commandHandlers.Select(p =>
            {
                return (BaseHandler)Activator.CreateInstance(p.Type, new[] { _kookService })!;
            }).ToList());
        }
    }
}
