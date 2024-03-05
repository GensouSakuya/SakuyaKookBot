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
        readonly IDataService _dataService;
        readonly HandlerEngine _engine;
        readonly ILoggerFactory _loggerFactory;
        public HandlerCenter(IKookService kookService, IDataService dataService, ILogger<HandlerCenter> logger, ILoggerFactory loggerFactory, HandlerEngine engine) 
        {
            _kookService = kookService;
            _dataService = dataService;
            _loggerFactory = loggerFactory;
            _logger = logger;
            _engine = engine;
            _kookService.MessageReceived += InternalKookMessageReceived;
        }

        private async Task InternalKookMessageReceived(string? message, Models.Enum.EnumKookMessageSource arg2, Models.BaseKookUser arg3, Models.BaseKookChannel arg4)
        {
            if(message != null && (message.StartsWith(".") || message.StartsWith("/")))
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
            var handlerTypes = Assembly.GetExecutingAssembly().GetTypes()
                .Where(p => p.IsSubclassOf(typeof(BaseHandler)) && !p.IsAbstract).ToList();
            _logger.LogDebug("found {Count} handlers", handlerTypes.Count);

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
                return CreateInstance(p.Type);
            }).ToList());

            var commanderHandlerGroup = commandHandlers.Select(p =>
            {
                return new
                {
                    Commands = p.CommandList.Select(q => q.Command),
                    Handler = CreateInstance(p.Type)
            };
            });
            var commandDic = new Dictionary<string, BaseHandler>();
            foreach (var handler in commanderHandlerGroup)
            {
                foreach(var command in handler.Commands)
                {
                    commandDic[command] = handler.Handler;
                }
            }
            _engine.SetCommandHandlers(commandDic);
        }

        private BaseHandler CreateInstance(Type type)
        {
            var logger = _loggerFactory.CreateLogger(type.Name);
            if (type.BaseType == null)
                return null;
            if (type.BaseType == typeof(BaseHandler))
                return (BaseHandler)Activator.CreateInstance(type, new object[] { _kookService, logger })!;
            else if (type.BaseType.IsGenericType && type.BaseType?.GetGenericTypeDefinition() == typeof(BaseDataHandler<>))
                return (BaseHandler)Activator.CreateInstance(type, new object[] { _dataService, _kookService, logger })!;
            return null;
        }
    }
}
