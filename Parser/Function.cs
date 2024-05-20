using FFMpegCore.Arguments;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenCVVideoRedactor
{
    namespace Parser
    {
        public class Function : IMathExpression
        {
            private protected string _name;
            public string getName { get { return _name; } }
            public int ArgCount { get { return Arguments.Length; } }
            public IMathExpression[] Arguments { get { return _arguments; } }
            private protected MathDelegate func;
            private protected IMathExpression[] _arguments { get; set; }
            public Function(string Name, int argCount, MathDelegate function)
            {
                _name = Name;
                _arguments = new IMathExpression[argCount];
                func = function;
            }

            public void SetArguments(IMathExpression[] args)
            {
                _arguments = args.Take(Arguments.Length).ToArray();
            }
            public void SetFunction(MathDelegate function)
            {
                func = function;
            }
            public virtual double Calculate()
            {
                if (func == null) throw new Exception("Функция "+_name+" не определена");
                return func(_arguments.Select((n)=>n.Calculate()).ToArray());
            }
            public virtual double Calculate(double[] args)
            {
                if (func == null) throw new Exception("Функция " + _name + " не определена");
                return func(args.Take(_arguments.Length).ToArray());
            }
            public static bool IsFunction(string function)
            {
                Regex regex = new Regex(@"(?<![0-9)])([a-zA-Zа-яА-Я]+[0-9]*\()");
                var match = regex.Match(function);
                if (match.Success)
                {
                    int count = 0;
                    int endI = match.Index;
                    if (endI != 0) return false;
                    for (int i = match.Index + match.Value.Length - 1; i < function.Length; i++)
                    {
                        if (function[i] == '(')
                        {
                            count++;
                        }
                        if (function[i] == ')')
                        {
                            count--;
                        }
                        if (count == 0)
                        {
                            endI = i + 1;
                            break;
                        }
                    }
                    if (endI == function.Length) return true;
                }
                return false;
            }
           
            public static string GetFunctionName(string function)
            {
                if (IsFunction(function))
                {
                    return function.Substring(0, function.IndexOf('('));
                }
                throw new Exception("Not Valid Function");
            }
            public static string[] GetFunctionArguments(string function)
            {
                if (IsFunction(function))
                {
                    int i = function.IndexOf('(') + 1;
                    string args = function.Substring(i,function.Length-i-1);
                    List<string> result = new List<string>();
                    int count = 0;
                    int lastk = 0;
                    for(int k = 0; k < args.Length; k++)
                    {
                        if (args[k] == '(')
                        {
                            count++;
                        }
                        if (args[k] == ')')
                        {
                            count--;
                        }
                        if (count == 0 && args[k] == ',')
                        {
                            result.Add(args.Substring(lastk,k-lastk));
                            lastk = k+1;
                        }
                        if(k == args.Length - 1)
                        {
                            result.Add(args.Substring(lastk, k + 1 - lastk));
                        }
                    }
                    return result.ToArray();
                }
                throw new Exception("Not Valid Function");
            }
            public Function Clone(IMathExpression[] args)
            {
                Function Func = new Function(this._name, this.ArgCount, this.func);
                Func.SetArguments(args);
                return Func;
            }
            public virtual Function Clone()
            {
                Function Func = new Function(this._name, this.ArgCount, this.func);
                Func.SetArguments(_arguments);
                return Func;
            }
            public override string ToString()
            {
                string result = _name + "(";
                if (_arguments.Length > 0) result += _arguments[0].ToString();
                for(int i = 1; i < _arguments.Length; i++)
                {
                    result += ","+_arguments[i].ToString();
                }
                return result+")";
            }

            public virtual bool SetVarriable(string name, double value)
            {
                bool result = false;
                foreach (var arg in _arguments)
                {
                    result = arg.SetVarriable(name, value) || result;
                }
                return result;
            }

            public virtual void SetFunction(string name, int argCount, MathDelegate func)
            {
                if(_name == name && argCount == ArgCount)
                {
                    this.func = func;
                }
                    foreach(var arg in _arguments)
                    {
                        arg.SetFunction(name, argCount, func);
                    }
            }
            public virtual List<string> GetVariables()
            {
                return Arguments.SelectMany(n => n.GetVariables()).ToList();
            }

            public virtual List<(string name, int argsCount)> GetFunctions()
            {
                var list = Arguments.SelectMany(n => n.GetFunctions()).ToList();
                list.Add((_name, ArgCount));
                return list;
            }
        }
        #region MathFunctions
        public class MaxFunction : Function
        {
            public MaxFunction(IMathExpression? argument1 = null, IMathExpression? argument2 = null) : base("max", 2, (double[] args) => { return args[0] >= args[1]? args[0]: args[1]; })
            {
                Arguments[0] = argument1 ?? new ErrorExpression();
                Arguments[1] = argument2 ?? new ErrorExpression();
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
            public override Function Clone()
            {
                return new MaxFunction(Arguments[0], Arguments[1]);
            }
        }
        public class MinFunction : Function
        {
            public MinFunction(IMathExpression? argument1 = null, IMathExpression? argument2 = null) : base("min", 2, (double[] args) => { return args[0] <= args[1] ? args[0] : args[1]; })
            {
                Arguments[0] = argument1 ?? new ErrorExpression();
                Arguments[1] = argument2 ?? new ErrorExpression();
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
            public override Function Clone()
            {
                return new MinFunction(Arguments[0], Arguments[1]);
            }
        }
        public class RoundFunction : Function
        {
            public RoundFunction(IMathExpression? argument = null) : base("round", 1, (double[] args) => { return Math.Round(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
            public override Function Clone()
            {
                return new RoundFunction(Arguments[0]);
            }
        }
        public class CeilFunction : Function
        {
            public CeilFunction(IMathExpression? argument = null) : base("ceil", 1, (double[] args) => { return Math.Ceiling(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
            public override Function Clone()
            {
                return new CeilFunction(Arguments[0]);
            }
        }
        public class FloorFunction : Function
        {
            public FloorFunction(IMathExpression? argument = null) : base("floor", 1, (double[] args) => { return Math.Floor(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
            public override Function Clone()
            {
                return new FloorFunction(Arguments[0]);
            }
        }
        public class CtgFunction : Function
        {
            public CtgFunction(IMathExpression? argument = null) : base("ctg", 1, (double[] args) => { return 1/Math.Tan(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new CtgFunction(Arguments[0]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        public class TgFunction : Function
        {
            public TgFunction(IMathExpression? argument = null) : base("tg", 1, (double[] args) => { return Math.Tan(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new TgFunction(Arguments[0]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        public class ExpFunction : Function
        {
            public ExpFunction(IMathExpression? argument = null) : base("exp", 1, (double[] args) => { return Math.Exp(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new ExpFunction(Arguments[0]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        public class CosFunction : Function
        {
            public CosFunction(IMathExpression? argument = null) : base("cos", 1, (double[] args) => { return Math.Cos(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new CosFunction(Arguments[0]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        public class SinFunction : Function
        {
            public SinFunction(IMathExpression? argument = null) : base("sin", 1, (double[] args) => { return Math.Sin(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new SinFunction(Arguments[0]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        public class LogFunction : Function
        {
            public LogFunction(IMathExpression? argument = null, IMathExpression? logBase = null) : base("log", 2, (double[] args) => { return Math.Log(args[0], args[1]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
                Arguments[1] = logBase ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new LogFunction(Arguments[0], Arguments[1]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        public class LgFunction : Function
        {
            public LgFunction(IMathExpression? argument = null) : base("lg", 1, (double[] args) => { return Math.Log10(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new LgFunction(Arguments[0]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        public class LnFunction : Function
        {
            public LnFunction(IMathExpression? argument = null) : base("ln", 1, (double[] args) => { return Math.Log(args[0]); })
            {
                Arguments[0] = argument ?? new ErrorExpression();
            }
            public override Function Clone()
            {
                return new LnFunction(Arguments[0]);
            }
            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                throw new Exception("Данная функция не может быть изменена");
            }
        }
        #endregion
    }
}