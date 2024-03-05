namespace GensouSakuya.KookBot.App.Handlers.Base
{
    internal class CommandTriggerAttribute : Attribute
    {
        public string Command { get; }
        public CommandTriggerAttribute(string command)
        {
            Command = command;
        }
    }
}
