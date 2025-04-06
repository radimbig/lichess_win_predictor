using LichessNET.API;

namespace Lichess_Prediction
{
    internal class Program
    {
       

        static async Task Main(string[] args)
        {
            
            string? token;
            int refreshRate = 5000;
            string default_path_to_engine = "stockfish.exe";
            


            using (var r = new StreamReader("token.txt"))
            {
                token = r.ReadLine();
                if (!r.EndOfStream)
                {
                    try
                    {
                        string? tempLine = r.ReadLine();
                        refreshRate = Convert.ToInt32(tempLine);
                    }
                    catch (Exception ex)
                    {
                            Console.WriteLine(ex.Message);
                            Console.WriteLine($"Problems with reading refresh rate, setting to {refreshRate}");
                    }
                }
            }
            if(String.IsNullOrEmpty(token))
            {
                throw new Exception("No token found in token.txt");
            }
            var client = new LichessApiClient();
            await client.SetToken(token);
            var email = await client.GetAccountEmail();
            Console.WriteLine($"connected to:{email}");

            var games = await client.GetOngoingGamesAsync(1);
            using(var engine = new EngineWrapper(default_path_to_engine))
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
