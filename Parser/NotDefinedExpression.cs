using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Parser
{
    class NotDefinedExpression : IMathExpression
    {
        private string _expression;
        public NotDefinedExpression(string expression = "") { _expression = expression; }
        public double Calculate()
        {
            throw new Exception($"Выражение {_expression} не определено");
        }

        public List<string> GetVariables()
        {
            throw new Exception($"Выражение {_expression} не определено");
        }

        public bool SetVarriable(string name, double value)
        {
            throw new Exception($"Выражение {_expression} не определено");
        }
        public override string ToString()
        {
            return _expression;
        }
    }
}
