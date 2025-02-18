﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace OpenCVVideoRedactor
{
    namespace Parser
    {
        public enum OperatorType { BinaryOperator, LeftOperator, RightOperator };
        public class Operator : IMathExpression
        {
            private protected string operatorName;
            public string getName { get { return operatorName; } }
            private protected MathDelegate operation;
            private protected OperatorType type = OperatorType.BinaryOperator;
            private protected bool isSymbolOp;
            public OperatorType OperatorType { get { return type; } }
            public IMathExpression? left;
            public IMathExpression? right;
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
            public virtual double Calculate()
            {
                if(type == OperatorType.BinaryOperator)
                    return operation(new double[2] { left!.Calculate(), right!.Calculate() });
                if (type == OperatorType.RightOperator)
                    return operation(new double[1] { left!.Calculate()});
                return operation(new double[1] {  right!.Calculate() });
            }
            public virtual Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                Operator o = new Operator(operatorName, operation, _priority, type);
                o.left = leftExpr;
                o.right = rightExpr;
                return o;
            }
            public virtual Match Find(string str)
            {
                if (isSymbolOp)
                {
                    string name = operatorName == "^" || operatorName == @"\" ? @"\" + operatorName:operatorName;
                    switch (type)
                    {
                        case OperatorType.BinaryOperator:
                            return new Regex(@"(?<=([a-zA-Zа-яА-Я0-9)]+)(\s?))([" + name + @"])(?=(\s?)([0-9a-zA-Zа-яА-Я(]))").Match(str);
                        case OperatorType.LeftOperator:
                            return new Regex(@"(?<!([0-9a-zA-Zа-яА-Я().,\s]+(\s?)|\s[a-zA-Z]+\s))([" + name + @"])(?=(\s?)([0-9a-zA-Zа-яА-Я(]+))").Match(str);
                        case OperatorType.RightOperator:
                            return new Regex(@"(?<=([a-zA-Zа-яА-Я0-9)]+)(\s?))([" + name + @"])(?=([^0-9a-zA-Zа-яА-Я().,\s]*(\s?)|\s[a-zA-Z]+\s))").Match(str);
                    }
                }
                return new Regex(@"(?<=[a-zA-Zа-яА-Я0-9)]+)(\s"+operatorName+@"\s)(?=[0-9a-zA-Zа-яА-Я(]+)").Match(str);
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

            public virtual bool SetVarriable(string name, double value)
            {
                bool lResult = false;
                bool rResult = false;
                if(left != null)lResult = left.SetVarriable(name, value);
                if(right != null)rResult = right.SetVarriable(name, value);
                return lResult || rResult;
            }

            public virtual bool IsEqualOperator(Operator op)
            {
                return op.getName == getName && op.OperatorType == OperatorType;
            }

            public virtual List<string> GetVariables()
            {
                List<string> variables = new List<string>();
                if(left != null) variables.AddRange(left.GetVariables());
                if (right != null) variables.AddRange(right.GetVariables());
                return variables;
            }
        }
        #region MathExpressionOperators
        public class MultiplicationOp : Operator
        {
            public MultiplicationOp(IMathExpression? L = null, IMathExpression? R = null) : base("*", (double[] args) => { return args[0] * args[1]; }, 3)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new MultiplicationOp(leftExpr, rightExpr);
            }
        }
        public class DivisionOp : Operator
        {
            public DivisionOp(IMathExpression? L = null, IMathExpression? R = null) : base("/", (double[] args) => { return args[0] / args[1]; }, 3)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new DivisionOp(leftExpr, rightExpr);
            }
        }
        public class AdditionOp : Operator
        {
            public AdditionOp(IMathExpression? L = null, IMathExpression? R = null) : base("+", (double[] args) => { return args[0] + args[1]; }, 0)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new AdditionOp(leftExpr, rightExpr);
            }
        }
        public class SubtractionOp : Operator
        {
            public SubtractionOp(IMathExpression? L = null, IMathExpression? R = null) : base("-", (double[] args) => { return args[0] - args[1]; }, 0)
            {
                left = L;
                right = R;
            }
            
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new SubtractionOp(leftExpr,rightExpr);
            }
        }
        public class ModOp : Operator
        {
            public ModOp(IMathExpression? L = null, IMathExpression? R = null) : base("mod", (double[] args) => {
                return args[0] % args[1];
            }, 1)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new ModOp(leftExpr,rightExpr);
            }
        }
        public class PowerOp : Operator
        {
            public PowerOp(IMathExpression? L = null, IMathExpression? R = null) : base("^", (double[] args) => { return Math.Pow(args[0], args[1]); }, 5)
            {
                left = L;
                right = R;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new PowerOp(leftExpr, rightExpr);
            }
        }
        public class SqrtOp : Operator
        {
            public SqrtOp(IMathExpression? R = null) : base("√", (double[] args) => { return Math.Sqrt(args[0]); }, 5, OperatorType.LeftOperator)
            {
                right = R;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
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
            public PercentOp(IMathExpression? L = null) : base("%", (double[] args) => { return args[0] / 100; }, 4, OperatorType.RightOperator)
            {
                left = L;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new PercentOp(leftExpr);
            }
        }
        public class NegativeOp : Operator
        {
            public NegativeOp(IMathExpression? R = null) : base("-", (double[] args) => { return -args[0]; }, 5, OperatorType.LeftOperator)
            {
                right = R;
            }
            public override Operator Clone(IMathExpression? leftExpr, IMathExpression? rightExpr)
            {
                return new NegativeOp(rightExpr);
            }
        }
        #endregion
    }
}
