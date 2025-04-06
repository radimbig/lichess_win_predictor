using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lichess_Prediction
{
    public interface IEngineWrapper
    {
        
        void PrintChance(double x);

        void PrintChance(string fen);

        double GetChance(string fen);


    }
}
