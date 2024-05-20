using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OpenCVVideoRedactor
{
    namespace Parser
    {
        public enum OperatorType { BinaryOperator, LeftOperator, RightOperator };
        public class Operator : MathExpression
        {
            private protected string operatorName;
            public string getName { get { return operatorName; } }
            private protected MathDelegate operation;
            private protected OperatorType type = OperatorType.BinaryOperator;
            private protected bool isSymbolOp;
            public OperatorType OperatorType { get { return type; } }
            public MathExpression? left;
            public MathExpression? right;
            private protected int _priority;
            public int Priority { get { return _priority; } }
            public Operator(string Name, MathDelegate Operation, int priority,OperatorType operatorType = OperatorType.BinaryOperator)
            {
                Regex isSymbol = new Regex(@"(?<=([a-zA-Zа-яА-Я0-9)]*)(\s?))([^0-9a-zA-Zа-яА-Я().,\s])(?=(\s?)([0-9a-zA-Zа-яА-Я(]*))");
                Regex isNotSymbol = new Regex(@"([a-zA-Z]+)");
                if (isSymbol.IsMatch(Name)) { isSymbolOp = true; this.operatorName = Name; }
                else if (isNotSymbol.IsMatch(Name)) { isSymbolOp = false; this.operatorName = Name; }
                else throw new ArgumentException("Не корректное наименование оператора");
                if (!isSymbolOp && operatorType != OperatorType.BinaryOperator) throw new ArgumentException("Строковый оператор может быть только бинарным");
                this.operation = Operation;
                type = operatorType;
                _priority = priority%100+(int)type * 100;
            }
            public override double Calculate()
            {
                if(type == OperatorType.BinaryOperator)
                    return operation(new double[2] { left!.Calculate(), right!.Calculate() });
                if (type == OperatorType.RightOperator)
                    return operation(new double[1] { left!.Calculate()});
                return operation(new double[1] {  right!.Calculate() });
            }
            public virtual Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                Operator o = new Operator(operatorName, operation, _priority, type);
                o.left = leftExpr;
                o.right = rightExpr;
                return o;
            }
            public virtual Match Find(string str)
            {
                Regex findPoss;
                if (isSymbolOp)
                {
                    string name = operatorName == "^" || operatorName == @"\" ? @"\" + operatorName:operatorName;
                    if (type == OperatorType.BinaryOperator)
                    {
                        findPoss = new Regex(@"(?<=([a-zA-Zа-яА-Я0-9)]+)(\s?))([" + name + @"])(?=(\s?)([0-9a-zA-Zа-яА-Я(]))");
                        return findPoss.Match(str);
                    }
                    if (type == OperatorType.LeftOperator)
                    {
                        findPoss = new Regex(@"(?<!([0-9a-zA-Zа-яА-Я().,\s]+(\s?)|\s[a-zA-Z]+\s))([" + name + @"])(?=(\s?)([0-9a-zA-Zа-яА-Я(]+))");
                        return findPoss.Match(str);
                    }
                    if (type == OperatorType.RightOperator)
                    {
                        findPoss = new Regex(@"(?<=([a-zA-Zа-яА-Я0-9)]+)(\s?))(["+name+@"])(?=([^0-9a-zA-Zа-яА-Я().,\s]*(\s?)|\s[a-zA-Z]+\s))");
                        return findPoss.Match(str);
                    }
                }
                findPoss = new Regex(@"(?<=[a-zA-Zа-яА-Я0-9)]+)(\s"+operatorName+@"\s)(?=[0-9a-zA-Zа-яА-Я(]+)");
                return findPoss.Match(str);
            }
            public override string ToString()
            {
                string result = "";
                if (type == OperatorType.BinaryOperator)
                {
                    if(left is Operator && ((Operator)left).Priority < _priority)
                    {
                        result += "(" + left.ToString() + ")";
                    }
                    else
                    {
                        result += left!.ToString();
                    }
                    result += isSymbolOp? operatorName:" "+operatorName+" ";
                    if (right is Operator && ((Operator)right).Priority <= _priority)
                    {
                        result += "(" + right.ToString() + ")";
                    }
                    else
                    {
                        result += right!.ToString();
                    }
                    return result;
                }
                if (type == OperatorType.LeftOperator)
                {
                    result += operatorName;
                    if (right is Operator && ((Operator)right).Priority < _priority)
                    {
                        result += "(" + right.ToString() + ")";
                    }
                    else
                    {
                        result += right!.ToString();
                    }
                    return result;
                }
                if (type == OperatorType.RightOperator)
                {
                    if (left is Operator && ((Operator)left).Priority < _priority)
                    {
                        result += "(" + left.ToString() + ")";
                    }
                    else
                    {
                        result += left!.ToString();
                    }
                    result += operatorName;
                    return result;
                }
                return "";
            }

            public override bool SetVarriable(string name, double value)
            {
                bool lResult = false;
                bool rResult = false;
                if(left != null)lResult = left.SetVarriable(name, value);
                if(right != null)rResult = right.SetVarriable(name, value);
                return lResult || rResult;
            }

            public override void SetFunction(string name, int argCount, MathDelegate func)
            {
                left?.SetFunction(name, argCount, func);
                right?.SetFunction(name, argCount, func);
            }
            public virtual bool IsEqualOperator(Operator op)
            {
                return op.getName == getName && op.OperatorType == OperatorType;
            }

            public override List<string> GetVariables()
            {
                List<string> variables = new List<string>();
                if(left != null) variables.AddRange(left.GetVariables());
                if (right != null) variables.AddRange(right.GetVariables());
                return variables;
            }

            public override List<(string name, int argsCount)> GetFunctions()
            {
                List<(string name, int argsCount)> functions = new List<(string name, int argsCount)>();
                if (left != null) functions.AddRange(left.GetFunctions());
                if (right != null) functions.AddRange(right.GetFunctions());
                return functions;
            }
        }
        #region MathExpressionOperators
        public class MultiplicationOp : Operator
        {
            public MultiplicationOp(MathExpression? L = null, MathExpression? R = null) : base("*", (double[] args) => { return args[0] * args[1]; }, 3)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new MultiplicationOp(leftExpr, rightExpr);
            }
        }
        public class DivisionOp : Operator
        {
            public DivisionOp(MathExpression? L = null, MathExpression? R = null) : base("/", (double[] args) => { return args[0] / args[1]; }, 3)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new DivisionOp(leftExpr, rightExpr);
            }
        }
        public class AdditionOp : Operator
        {
            public AdditionOp(MathExpression? L = null, MathExpression? R = null) : base("+", (double[] args) => { return args[0] + args[1]; }, 0)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new AdditionOp(leftExpr, rightExpr);
            }
        }
        public class SubtractionOp : Operator
        {
            public SubtractionOp(MathExpression? L = null, MathExpression? R = null) : base("-", (double[] args) => { return args[0] - args[1]; }, 0)
            {
                left = L;
                right = R;
            }
            
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new SubtractionOp(leftExpr,rightExpr);
            }
        }
        public class ModOp : Operator
        {
            public ModOp(MathExpression? L = null, MathExpression? R = null) : base("mod", (double[] args) => {
                return args[0] % args[1];
            }, 1)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new ModOp(leftExpr,rightExpr);
            }
        }
        public class PowerOp : Operator
        {
            public PowerOp(MathExpression? L = null, MathExpression? R = null) : base("^", (double[] args) => { return Math.Pow(args[0], args[1]); }, 5)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new PowerOp(leftExpr, rightExpr);
            }
        }
        public class SqrtOp : Operator
        {
            public SqrtOp(MathExpression? R = null) : base("√", (double[] args) => { return Math.Sqrt(args[0]); }, 5, OperatorType.LeftOperator)
            {
                right = R;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new SqrtOp(rightExpr);
            }
            public override Match Find(string str)
            {
                Regex findPoss = new Regex(@"(?<!([0-9a-zA-Zа-яА-Я().,\s]+(\s?)|\s[a-zA-Z]+\s))(\\|√)(?=(\s?)([0-9a-zA-Zа-яА-Я(]+))");
                return findPoss.Match(str);
            }
        }
        public class PercentOp : Operator
        {
            public PercentOp(MathExpression? L = null) : base("%", (double[] args) => { return args[0] / 100; }, 4, OperatorType.RightOperator)
            {
                left = L;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new PercentOp(leftExpr);
            }
        }
        public class NegativeOp : Operator
        {
            public NegativeOp(MathExpression? R = null) : base("-", (double[] args) => { return -args[0]; }, 5, OperatorType.LeftOperator)
            {
                right = R;
            }
            public override Operator Clone(MathExpression? leftExpr, MathExpression? rightExpr)
            {
                return new NegativeOp(rightExpr);
            }
        }
        #endregion
    }
}
