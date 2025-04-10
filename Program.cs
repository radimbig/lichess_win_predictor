using LichessNET.API;
using LichessNET.Entities.Game;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Reflection;

namespace Lichess_Prediction
{
    internal class Program
    {
       

        static async Task Main(string[] args)
        {
            UserSettings settings = new UserSettings();
            string? token = settings.LichessToken;
            int refreshRate = settings.RefreshRate;
            string default_path_to_engine = settings.PathToEngine ?? "stockfish.exe";
            int depth = settings.LichessDepth;
            string? apiUrl = settings.EngineApiURL;


            var lichessApi = new LichessApiClient();
            await lichessApi.SetToken(token);


            // turning off logging
            typeof(LichessApiClient)
            .GetField("_logger", BindingFlags.NonPublic | BindingFlags.Instance)
             ?.SetValue(lichessApi, NullLogger.Instance);

            var email = await lichessApi.GetAccountEmail();
            Console.WriteLine($"connected to:{email}");


            
            
            
            var games = await lichessApi.GetOngoingGamesAsync(1);
            if (games.Count == 0)
            {
                Console.WriteLine("No games found");
                return;
            }
            
            
            using (var engine = new EngineWrapper(default_path_to_engine, depth))
            {
                using(var c = new HttpClient())
                {
                    await engine.PrintAllChancesAsync(games[0].Fen, c, lichessApi, apiUrl);
                    
                    string lastFen = games[0].Fen;

                    while (games.Count > 0) 
                    {
                        Thread.Sleep(refreshRate);
                        games = await lichessApi.GetOngoingGamesAsync(1);
                        if (games.Count < 1) return;
                        if (games[0].Fen == lastFen)
                        {
                            continue;
                        }
                        lastFen = games[0].Fen;
                        await engine.PrintAllChancesAsync(lastFen, c, lichessApi, apiUrl);
                    }
                }
            }

            
        }
    }
}
