using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenCVVideoRedactor
{
    namespace Parser
    {
        public class Value : IMathExpression
        {
            private double _value;
            public Value(double value)
            {
                _value = value;
            }
            public static Value ValueFromString(string val)
            {
                Regex regex = new Regex(@"^(-?)(\d+(\.\d+)?)$");
                if (regex.IsMatch(val))
                {
                    double dVal = Convert.ToDouble(val.Replace(".", ","));
                    return new Value(dVal);
                }
                throw new Exception("Не корректная строка");
            }
            public double Calculate()
            {
                return _value;
            }
            public static bool isValue(string str)
            {
                Regex regex = new Regex(@"^(-?)((\d+(\.\d+)?i?)|i)$");
                return regex.Match(str).Success;
            }
            public static string[] FindValues(string str)
            {
                Regex regex = new Regex(@"(?<![a-zA-Z0-9а-яА-Я)])(((\d+(\.\d+)?)(i?))|i)(?![a-zA-Z0-9а-яА-Я(])");
                return regex.Matches(str).Select((n) => n.Value).ToArray();
            }
            public override string ToString()
            {
                return _value.ToString();
            }

            public bool SetVarriable(string name, double value)
            {
                return false;
            }

            public void SetFunction(string name, int argCount, MathDelegate func)
            {
                return;
            }

            public List<string> GetVariables()
            {
                return new List<string>();
            }

            public List<(string name, int argsCount)> GetFunctions()
            {
               return new List<(string name, int argsCount)>();
            }
        }
    }
}
