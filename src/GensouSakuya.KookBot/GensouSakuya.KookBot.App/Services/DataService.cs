using GensouSakuya.KookBot.App.Interfaces;
using Newtonsoft.Json.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace GensouSakuya.KookBot.App.Services
{
    internal class DataService : IDataService
    {
        private readonly Subject<string> _observedLogList = new Subject<string>();
        const string FileName = "data.json";
        readonly JObject _dataObj;

        public DataService()
        {
            _observedLogList.Throttle(TimeSpan.FromMinutes(1))
                .Subscribe(p =>
                {
                    SaveToFile();
                });
            if (File.Exists(FileName))
            {
                _dataObj = JObject.Parse(File.ReadAllText(FileName));
            }
            else
            {
                _dataObj = new JObject();
                File.WriteAllText(FileName, _dataObj.ToString());
            }
        }

        public T Get<T>(string key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));
            if (!_dataObj.ContainsKey(key))
                return default;
            return _dataObj.GetValue(key).ToObject<T>();
        }

        public void Save<T>(string key,T data, bool immediately = true)
        {
            _dataObj[key] = JToken.FromObject(data);
            if (immediately)
            {
                SaveToFile();
            }
            else
            {
                _observedLogList.Next();
            }
        }

        private void SaveToFile()
        {
            File.WriteAllText(FileName, _dataObj.ToString());
        }
    }
}
