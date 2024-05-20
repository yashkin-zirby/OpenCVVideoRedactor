using System.Collections.Generic;

namespace OpenCVVideoRedactor.Parser
{
    public delegate double MathDelegate(double[] args);
    public abstract class MathExpression
        {
            public abstract double Calculate();
            public abstract bool SetVarriable(string name, double value);
			public abstract List<string> GetVariables();
            public abstract void SetFunction(string name, int argCount, MathDelegate func);
			public abstract List<(string name, int argsCount)> GetFunctions();
    }
}
