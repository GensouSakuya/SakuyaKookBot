namespace GensouSakuya.KookBot.App.Interfaces
{
    internal interface IConfigService
    {
        T? Get<T>(string key);

        //void Set<T>(string key, T value);
    }
}
