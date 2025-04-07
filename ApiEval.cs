using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lichess_Prediction
{
    public class ApiEval
    {
        public int Depth = 0;
        public double WinChance = 0;
        public int Cp = 0;
        public ApiEval(int depth, double winChance, int cp)
        {
            Depth = depth;
            WinChance = winChance;
            Cp = cp;
        }
        public ApiEval() { }
    }
}
