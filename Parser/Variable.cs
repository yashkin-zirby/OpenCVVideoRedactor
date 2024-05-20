using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenCVVideoRedactor
{
    namespace Parser
    {
        public class Variable : IMathExpression
        {
            private double? _value = null;
            private string _name;
            private bool isConst = false;
            public Variable(string name, double value,bool isConstant)
            {
                Regex regex = new Regex(@"^([a-zA-Zа-яА-Я]+[0-9]*[a-zA-Zа-яА-Я]*)$");
                if (regex.IsMatch(name))
                {
                    isConst = isConstant;
                    _name = name;
                    _value = value;
                    return;
                }
                throw new Exception("Не корректное наименование переменной");
            }
            public bool isConstant { get { return isConst; } }
            public string Name { get { return _name; } }
            public Variable(string name)
            {
                Regex regex = new Regex(@"^([a-zA-Zа-яА-Я]+[0-9]*[a-zA-Zа-яА-Я]*)$");
                if (regex.IsMatch(name))
                {
                    _name = name;
                    return;
                }
                throw new Exception("Не корректное имя переменной");
            }
            public void SetValue(double value)
            {
                if (isConst) throw new Exception("Нельзя изменить значение константы");
                _value = value;
            }

            public double Calculate()
            {
                if (_value == null) throw new ArgumentNullException("Значение переменной "+_name+" не определено");
                return _value.Value;
            }
            public static bool isVarriable(string str)
            {
                Regex regex = new Regex(@"^([a-zA-Zа-яА-Я]+[0-9]*[a-zA-Zа-яА-Я]*)$");
                return regex.Match(str).Success;
            }
            public override string ToString()
            {
                return _name;
            }

            public bool SetVarriable(string name, double value)
            {
                if(name == this._name && !isConst)
                {
                    _value = value;
                    return true;
                }
                return false;
            }

            public void SetFunction(string name, int argCount, MathDelegate func)
            {
                return;
            }

            public List<string> GetVariables()
            {
                if (isConst)return new List<string>();
                return new List<string>() {_name};
            }

            public List<(string name, int argsCount)> GetFunctions()
            {
                return new List<(string name, int argsCount)>();
            }
        }
    }
}
