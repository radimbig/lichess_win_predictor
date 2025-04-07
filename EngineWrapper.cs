using System.Diagnostics;
namespace Lichess_Prediction
{
    public class EngineWrapper : IEngineWrapper, IDisposable
    {
        public string LastFen { get; set; } = string.Empty;

        public string PathToEngine { get; set; }

        public double LastChance { get; set; } = 0.0;

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
        public double GetChance(string fen)
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

            double result = Convert.ToInt32(line.Split(' ')[9]);
            LastChance = result;


            return result;
        }

        public void PrintChance(double x)
        {
            Console.Clear();
            Console.WriteLine($"cp is {x}");
            double chances = 50 + 50 * (2 / (1 + Math.Exp(-0.004 * x)) - 1);
            Console.WriteLine($"winning chance probably:{chances}");
        }

        public void PrintChance(string fen)
        {
            PrintChance(GetChance(fen));
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
        
    }
}
