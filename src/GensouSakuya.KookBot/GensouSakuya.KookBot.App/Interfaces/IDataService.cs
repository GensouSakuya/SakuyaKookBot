namespace GensouSakuya.KookBot.App.Interfaces
{
    internal interface IDataService
    {
        T Get<T>(string key);

        void Save<T>(string key, T data, bool immediately = true);
    }
}
