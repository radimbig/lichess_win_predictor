using LichessNET.API;
using LichessNET.Entities.Account.Performance;
using LichessNET.Entities.Analysis;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
namespace Lichess_Prediction
{
    public class EngineWrapper : IEngineWrapper, IDisposable
    {
        public string LastFen { get; set; } = string.Empty;

        public string PathToEngine { get; set; }

        public int LastChance { get; set; } = 0;

        private Process? process;

        public int depth = 10;
        public void StartEngine()
        {
            process = new Process()
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = PathToEngine,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    CreateNoWindow = false,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }
            };
            process.Start();

            var writer = process?.StandardInput;
            var reader = process?.StandardOutput;

            writer.WriteLine("uci");
            ReadUntil(reader, "uciok");

            writer.WriteLine("isready");
            ReadUntil(reader, "readyok");

        }
        public EngineWrapper(string pathToEngine)
        {
            PathToEngine = pathToEngine;
            StartEngine();
        }
        public EngineWrapper(string pathToEngine, int depth) : this(pathToEngine)
        {
            depth = this.depth;
        }
        public int GetLocalCp(string fen)
        {
            if(fen == LastFen)
            {
                return LastChance;
            }
            LastFen = fen;
            var writer = process?.StandardInput;
            var reader = process?.StandardOutput;

            writer.WriteLine("ucinewgame"); // clears hash table

            writer.WriteLine($"position fen {fen}");

            writer.WriteLine($"go depth {depth}");

            string line = ReadUntil(reader, $"info depth {depth}");

            // index of number in raw line

            int result = Convert.ToInt32(line.Split(' ')[9]);
            LastChance = result;


            return result;
        }

        public void PrintLocalChance(int x)
        {
            Console.Clear();
            Console.WriteLine($"cp is {x}");
            double chances = 50 + 50 * (2 / (1 + Math.Exp(-0.004 * x)) - 1);
            Console.WriteLine($"winning chance probably:{chances}");
        }

        public void PrintLocalChance(string fen)
        {
            PrintLocalChance(GetLocalCp(fen));
        }

        public void Dispose()
        {
            process?.Kill();
        }


        static string ReadUntil(StreamReader reader, string keyword)
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                Console.WriteLine(line);
                if (line.Contains(keyword))
                    break;
            }
            return line;
        }
        
        static async Task<int> GetLichessCpAsync(string fen, LichessApiClient client)
        {
            var eval = await client.GetCloudEvaluationAsync(fen);

            int result = 0;
            foreach(var pv in eval.Pvs)
            {
                result += pv.Cp;
            }
            return result/eval.Pvs.Count;
        }
        static async Task<int> GetApiCpAsync(string fen, HttpClient client, string? apiUrl)
        {
            var apiEval = await GetApiEvalAsync(fen, client, apiUrl);
            return apiEval.Cp;
        }

        // implement your method to correctly parse response from your api
        static async Task<ApiEval> GetApiEvalAsync(string fen, HttpClient client, string? apiUrl)
        {
            if(string.IsNullOrEmpty(apiUrl))
            {
                return new ApiEval();
            }
            var payloadObj = new
            {
                fen = fen,
            };
            var stringContent = new StringContent(JsonSerializer.Serialize(payloadObj), Encoding.UTF8, "application/json");
            var httpResponse = await client.PostAsync(apiUrl,stringContent);

            string json = await httpResponse.Content.ReadAsStringAsync();
            var doc = JsonDocument.Parse(json);
            double winChance = doc.RootElement.GetProperty("winChance").GetDouble();
            int depth = doc.RootElement.GetProperty("depth").GetInt32();
            int cp;
            try
            {
                cp = doc.RootElement.GetProperty("centipawns").GetInt32();
            }
            catch (Exception e)
            {
                cp = Convert.ToInt32(doc.RootElement.GetProperty("centipawns").GetString());
            }
            return new ApiEval(depth, winChance, cp);
        }

        async Task<int> GetAllMethodsEvalAverageAsync(string fen, HttpClient client, LichessApiClient lichess_client, string apiUrl)
        {
            int sum = 0;
            sum += await GetLichessCpAsync(fen, lichess_client);
            sum += GetLocalCp(fen);
            sum += await GetApiCpAsync(fen, client, apiUrl);
            return sum / 3;
        }
        public async Task PrintAllChancesAsync(string fen, HttpClient client, LichessApiClient lichessApi, string? apiUrl = null) 
        {

            PositionEvaluation lichess_eval;
            int lichessCp = 0;
            
            var evaluationTask =  lichessApi.GetCloudEvaluationAsync(fen);

            try
            {
                evaluationTask.Wait();
            }
            catch { }
            
            if (evaluationTask.IsFaulted)
            {
                lichess_eval = new PositionEvaluation();
                lichess_eval.Depth = 0;
                lichessCp = 0;
                Console.WriteLine("lichess does not have evaluation for this combination");
            }
            else
            {
                lichess_eval = evaluationTask.Result;
                lichessCp = 0;
                foreach (var pv in lichess_eval.Pvs)
                {
                    lichessCp += pv.Cp;
                }
                lichessCp = lichessCp / lichess_eval.Pvs.Count;
                
            }

            int localCp = GetLocalCp(fen);
            
            var apiEval = await GetApiEvalAsync(fen, client, apiUrl);


            Console.Clear();
            Console.WriteLine($"fen:{fen}");
            Console.WriteLine($"lichess:{lichessCp}, winchance{CPtoChance(lichessCp)}, depth:{lichess_eval.Depth}");
            Console.WriteLine($"local engine:{GetLocalCp(fen)}, winchance:{CPtoChance(localCp)}, depth:{depth}");
            Console.WriteLine($"api:{apiEval.Cp}, winchance:{apiEval.WinChance}, depth:{apiEval.Depth}");
            Console.WriteLine($"Average cp:{(apiEval.Cp + lichessCp + localCp)/3}, Average winchance{(CPtoChance(lichessCp) + CPtoChance(localCp) + apiEval.WinChance) /3}");
            LastFen = fen;
            return;
        }
        public double CPtoChance(int x)
        {
            double chances = 50 + 50 * (2 / (1 + Math.Exp(-0.004 * x)) - 1);
            return chances;
        }
    }
}
