using LichessNET.API;

namespace Lichess_Prediction
{
    internal class Program
    {
       

        static async Task Main(string[] args)
        {
            UserSettings settings = new UserSettings();
            string? token = settings.LichessToken;
            int refreshRate = settings.RefreshRate;
            string default_path_to_engine = settings.PathToEngine;
            int depth = settings.LichessDepth;


            var client = new LichessApiClient();
            await client.SetToken(token);
            var email = await client.GetAccountEmail();
            Console.WriteLine($"connected to:{email}");

            var games = await client.GetOngoingGamesAsync(1);
            using(var engine = new EngineWrapper(default_path_to_engine, depth))
            {
                while (games.Count > 0)
                {
                    string FEN = games[0].Fen;
                    Console.WriteLine(FEN);
                    double chance = engine.GetChance(FEN);
                    engine.PrintChance(chance);
                    Thread.Sleep(refreshRate);
                    games = await client.GetOngoingGamesAsync(1);
                }
            }
            
            Console.WriteLine("No game currently going, exit...");


        }
       
    }
}
