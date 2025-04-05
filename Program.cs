using LichessNET.API;
using System.Text;
using System.Text.Json;

namespace Lichess_Prediction
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            
            string? token;
            int refreshRate = 5000;
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
            using(var httpClient = new HttpClient())
            {
                while (games.Count > 0)
                {
                    string FEN = games[0].Fen;
                    double chance = await GetCurrentChances(httpClient, FEN);

                    Console.WriteLine($"White:{chance}, Black:{100d-chance}");
                    PrintChancesAsLine(chance);
                    Thread.Sleep(refreshRate);
                    games = await client.GetOngoingGamesAsync(1);
                }
            }
           
            Console.WriteLine("No game currently going, exit...");


        }

        
        static async Task<double> GetCurrentChances(HttpClient c, string fen)
        {
            double result = -1;
            var payloadObj = new
            {
                fen = fen,
            };
            var stringContent = new StringContent(JsonSerializer.Serialize(payloadObj),Encoding.UTF8, "application/json");
            var httpResponse = await c.PostAsync("https://chess-api.com/v1", stringContent);
            /*
             Winning chance: value 50 (50%) means that position is equal. Over 50 - white is winning. Below 50 - black is winning. 
             */
            string json = await httpResponse.Content.ReadAsStringAsync();
            var doc  = JsonDocument.Parse(json);
            result = doc.RootElement.GetProperty("winChance").GetDouble();
            return result;
        }

        static void PrintChancesAsLine(double chance)
        {
            int whiteChance = (int)chance / 10;

            
            var foreground = Console.ForegroundColor;
            
            Console.ForegroundColor = ConsoleColor.White;

            for (int i = 0; i < whiteChance; i++) { Console.Write('#'); }

            Console.ForegroundColor = ConsoleColor.Red;
            for (int i = 0; i < 10-whiteChance; i++) { Console.Write('#'); }

            Console.ForegroundColor = foreground;
            Console.WriteLine();
        }
    }
}
