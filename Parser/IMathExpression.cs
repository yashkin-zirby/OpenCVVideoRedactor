using System.Collections.Generic;

namespace OpenCVVideoRedactor.Parser
{
    public delegate double MathDelegate(double[] args);
    public interface IMathExpression
    {
            public double Calculate();
            public bool SetVarriable(string name, double value);
			public List<string> GetVariables();
    }
}
