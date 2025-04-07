using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Lichess_Prediction
{
    public class UserSettings
    {
        public string LichessToken;
        public int RefreshRate;
        public int LichessDepth;
        public string PathToEngine;

        public UserSettings()
        {
            string text = File.ReadAllText("settings.json");
            var doc = JsonDocument.Parse(text);
            LichessToken = doc.RootElement.GetProperty("token").GetString();
            RefreshRate = doc.RootElement.GetProperty("refreshRate").GetInt32();
            LichessDepth = doc.RootElement.GetProperty("engine_depth").GetInt32();
            PathToEngine = doc.RootElement.GetProperty("pathToEngine").GetString();
        }
    }
}
