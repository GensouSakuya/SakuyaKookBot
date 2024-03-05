using GensouSakuya.KookBot.App.Interfaces;
using Newtonsoft.Json.Linq;

namespace GensouSakuya.KookBot.App
{
    internal class ConfigService : IConfigService
    {
        const string FileName = "config.json";
        readonly JObject _configObj;

        public ConfigService()
        {
            if(File.Exists(FileName))
            {
                _configObj = JObject.Parse(File.ReadAllText(FileName));
            }
            else
            {
                _configObj = new JObject();
            }
        }

        public T? Get<T>(string key)
        {
            if (key == null) 
                throw new ArgumentNullException(nameof(key));
            if (!_configObj.ContainsKey(key))
                return default;
            return _configObj.Value<T>(key);
        }
    }
}
