﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Parser
{
    class ErrorExpression : IMathExpression
    {
        public double Calculate()
        {
            throw new NotImplementedException();
        }

        public List<string> GetVariables()
        {
            throw new NotImplementedException();
        }

        public bool SetVarriable(string name, double value)
        {
            throw new NotImplementedException();
        }
    }
}
