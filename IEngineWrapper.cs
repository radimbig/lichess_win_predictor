using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lichess_Prediction
{
    public interface IEngineWrapper
    {
        
        void PrintLocalChance(int x);

        void PrintLocalChance(string fen);

        int GetLocalCp(string fen);


    }
}
