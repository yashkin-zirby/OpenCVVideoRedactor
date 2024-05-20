using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Parser
{
    class ErrorExpression : MathExpression
    {
        public override double Calculate()
        {
            throw new NotImplementedException();
        }

        public override List<(string name, int argsCount)> GetFunctions()
        {
            throw new NotImplementedException();
        }

        public override List<string> GetVariables()
        {
            throw new NotImplementedException();
        }

        public override void SetFunction(string name, int argCount, MathDelegate func)
        {
            throw new NotImplementedException();
        }

        public override bool SetVarriable(string name, double value)
        {
            throw new NotImplementedException();
        }
    }
}
