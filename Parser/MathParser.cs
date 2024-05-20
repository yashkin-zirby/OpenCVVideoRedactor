using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace OpenCVVideoRedactor
{
    namespace Parser
    {
        public class MathParser
        {
            List<Function> functions = new List<Function> {new LogFunction(),
                                                   new LnFunction(),
                                                   new LgFunction(),
                                                   new TgFunction(),
                                                   new SinFunction(),
                                                   new CosFunction(),
                                                   new ExpFunction(),
                                                   new CtgFunction(),
                                                   new FloorFunction(),
                                                   new CeilFunction(),
                                                   new MinFunction(),
                                                   new MaxFunction(),
                                                   new RoundFunction()
            };
            List<Operator> operators = new List<Operator> {new DivisionOp(),new MultiplicationOp(),new AdditionOp(),
                                                    new ModOp(), new PercentOp(), new PowerOp(), new SqrtOp(), new SubtractionOp(), new NegativeOp()};
            List<Variable> variables = new List<Variable> {new Variable("pi",Math.PI,true), new Variable("e", Math.E, true) };
            public MathParser()
            {
            }
            public bool DefineVarriable(Variable var)
            {
                Variable? variable = variables.Where(n => n.ToString() == var.ToString()).FirstOrDefault();
                if (variable == null) { variables.Add(var); return true; }
                if (variable.isConstant) return false;
                variables.Remove(variable);
                variables.Add(var);
                return true;
            }
            public bool DefineOperator(Operator op)
            {
                Operator? @operator = operators.Where(n => n.getName == op.getName && n.OperatorType == op.OperatorType).FirstOrDefault();
                if (@operator == null) { operators.Add(op); return true; }
                return false;
            }
            public bool DefineFunction(Function func)
            {
                Function? variable = functions.Where(n => n.getName == func.getName && n.ArgCount == func.ArgCount).FirstOrDefault();
                if(variable != null)functions.Remove(variable);
                functions.Add(func);
                return true;
            }
            public string[] GetVarriables(MathExpression expression)
            {
                return getVarriables(expression).Except(variables.Where(n => n.isConstant).Select(n => n.ToString())).ToArray();
            }
            private string[] getVarriables(MathExpression expression)
            {
                if (expression is Value) return new string[0];
                if (expression is Variable) return new string[1] { $"{expression}"};
                Function? function = expression as Function;
                if (function != null)
                {
                    return function.Arguments.Select(n => getVarriables(n)).ToArray().Aggregate((prev,next)=>prev.Union(next).ToArray());
                }
                Operator? op = expression as Operator;
                if (op != null)
                {
                    return (op.left != null ? getVarriables(op.left) : new string[0])
                        .Union(op.right != null?getVarriables(op.right):new string[0]).ToArray();
                }
                return new string[0];
            }
            public MathExpression Parse(string expression)
            {
                string str = DropSpecialArguments(expression);
                (string newStr,List<Function> Functions) = FindFunctions(str);
                str = newStr;
                (string newStr2,List<MathExpression> Expressions) = FindExpressions(str);
                str = newStr2;
                Regex findFunc = new Regex(@"(func)");
                Regex findExpr = new Regex(@"(expr)");
                Regex findValR = new Regex(@"\(func\)|\(expr\)|(?<![a-zA-Z0-9а-яА-Я)])(([a-zA-Zа-яА-Я]+[0-9]*)|(((\d+(\.\d+)?)(i?))|i))(?![a-zA-Z0-9а-яА-Я(])");
                Regex findValL = new Regex(@"\(func\)|\(expr\)|(?<![a-zA-Z0-9а-яА-Я)])(([a-zA-Zа-яА-Я]+[0-9]*)|(((\d+(\.\d+)?)(i?))|i))(?![a-zA-Z0-9а-яА-Я(])", RegexOptions.RightToLeft);
                var operatorsGroups = operators.OrderBy((n) => -n.Priority).GroupBy(n => n.Priority).ToList();
                for (int i = 0; i < operatorsGroups.Count; i++)
                {
                    Match match = Match.Empty;
                    do
                    {
                        Operator? _operator = null;
                        foreach (var op in operatorsGroups[i])
                        {
                            if (_operator == null)
                            {
                                _operator = op;
                                match = op.Find(str);
                                continue;
                            }
                            var newMatch = op.Find(str);
                            if (newMatch.Success && newMatch.Index < match.Index || !match.Success)
                            {
                                _operator = op;
                                match = newMatch;
                            }
                        }
                        if (!match.Success || _operator == null) break;
                        switch (_operator.OperatorType)
                        {
                            case OperatorType.BinaryOperator: {
                                    int rStart = match.Index + match.Value.Length;
                                    string rightStr = str.Substring(rStart);
                                    string leftStr = str.Substring(0, match.Index);
                                    Match r = findValR.Match(rightStr);
                                    Match l = findValL.Match(leftStr);
                                    leftStr = leftStr.Substring(0, l.Index);
                                    rightStr = rightStr.Substring(r.Index + r.Value.Length);
                                    int countF = findFunc.Matches(leftStr).Count;
                                    int countE = findExpr.Matches(leftStr).Count;
                                    MathExpression left;
                                    MathExpression right;
                                    if (l.Value == "(func)") { left = Functions[countF]; Functions.Remove((Function)left); }
                                    else if (l.Value == "(expr)") { left = Expressions[countE]; Expressions.Remove(left); }
                                    else if (Value.isValue(l.Value)) { left = Value.ValueFromString(l.Value); }
                                    else if (Variable.isVarriable(l.Value)) { Variable? var = variables.FirstOrDefault(n => n.ToString() == l.Value); left = var ?? new Variable(l.Value); }
                                    else throw new Exception("Не известный тип аргумента оператора");
                                    if (r.Value == "(func)") { right = Functions[countF]; Functions.Remove((Function)right); }
                                    else if (r.Value == "(expr)") { right = Expressions[countE]; Expressions.Remove(right); }
                                    else if (Value.isValue(r.Value)) { right = Value.ValueFromString(r.Value); }
                                    else if (Variable.isVarriable(r.Value)) { Variable? var = variables.FirstOrDefault(n => n.ToString() == r.Value); right = var ?? new Variable(r.Value); }
                                    else throw new Exception("Не известный тип аргумента оператора");
                                    Expressions.Insert(countE, _operator.Clone(left, right));
                                    str = leftStr + "(expr)" + rightStr;
                                } break;
                            case OperatorType.LeftOperator: {
                                    int rStart = match.Index + match.Value.Length;
                                    string rightStr = str.Substring(rStart);
                                    string leftStr = str.Substring(0, match.Index);
                                    Match r = findValR.Match(rightStr);
                                    rightStr = rightStr.Substring(r.Index + r.Value.Length);
                                    int countF = findFunc.Matches(leftStr).Count;
                                    int countE = findExpr.Matches(leftStr).Count;
                                    MathExpression right;
                                    if (r.Value == "(func)") { right = Functions[countF]; Functions.Remove((Function)right); }
                                    else if (r.Value == "(expr)") { right = Expressions[countE]; Expressions.Remove(right); }
                                    else if (Value.isValue(r.Value)) { right = Value.ValueFromString(r.Value); }
                                    else if (Variable.isVarriable(r.Value)) { Variable? var = variables.FirstOrDefault(n => n.ToString() == r.Value); right = var ?? new Variable(r.Value); }
                                    else throw new Exception("Не известный тип аргумента оператора");
                                    Expressions.Insert(countE, _operator.Clone(null, right));
                                    str = leftStr + "(expr)" + rightStr;
                                } break;
                            case OperatorType.RightOperator: {
                                    int rStart = match.Index + match.Value.Length;
                                    string rightStr = str.Substring(rStart);
                                    string leftStr = str.Substring(0, match.Index);
                                    Match l = findValL.Match(leftStr);
                                    leftStr = leftStr.Substring(0, l.Index);
                                    int countF = findFunc.Matches(leftStr).Count;
                                    int countE = findExpr.Matches(leftStr).Count;
                                    MathExpression left;
                                    if (l.Value == "(func)") { left = Functions[countF]; Functions.Remove((Function)left); }
                                    else if (l.Value == "(expr)") { left = Expressions[countE]; Expressions.Remove(left); }
                                    else if (Value.isValue(l.Value)) { left = Value.ValueFromString(l.Value); }
                                    else if (Variable.isVarriable(l.Value)) { Variable? var = variables.FirstOrDefault(n => n.ToString() == l.Value); left = var ?? new Variable(l.Value); }
                                    else throw new Exception("Не известный тип аргумента оператора");
                                    Expressions.Insert(countE, _operator.Clone(left, null));
                                    str = leftStr + "(expr)" + rightStr;
                                } break;
                        }
                    } while (match != null && match.Success);
                }
                if (str == "(func)") return Functions[0];
                if (str == "(expr)") return Expressions[0];
                if (Value.isValue(str)) return Value.ValueFromString(str);
                if (Variable.isVarriable(str))
                {
                    Variable? variable = variables.FirstOrDefault(n => n.ToString() == str);
                    return variable ?? new Variable(str);
                }
                return new ErrorExpression();
            }
            private Function ParseFunction(string func)
            {
                string name = Function.GetFunctionName(func);
                string[] args = Function.GetFunctionArguments(func);
                Function? existingFunction = functions.FirstOrDefault((n) => n.ArgCount == args.Length && n.getName == name);
                if(existingFunction == null)
                {
                    existingFunction = new Function(name, args.Length, (double[] args) => { throw new Exception($"Функция {name} не определена"); });
                }
                return existingFunction.Clone(args.Select(n => Parse(n)).ToArray());
            }
            private MathExpression ParseExpression(string expression)
            {
                if(expression[0]=='(' && expression.Last() == ')')
                return Parse(expression.Substring(1,expression.Length-2));
                return Parse(expression);
            }
            private string DropSpecialArguments(string str)
            {
                Regex dropSpaces = new Regex(@"\s+");
                Regex func = new Regex(@"(?<![a-zA-Zа-яА-Я0-9])(\(func\))");
                Regex expr = new Regex(@"(?<![a-zA-Zа-яА-Я0-9])(\(expr\))");
                string result = func.Replace(dropSpaces.Replace(str," "), "func");
                result = expr.Replace(result, "expr");
                return result;
            }
            private (string result,List<Function> functions) FindFunctions(string str)
            {
                Regex regex = new Regex(@"(?<![0-9)])([a-zA-Zа-яА-Я]+[0-9]*\()");
                string val = str;
                var Result = new List<Function>();
                string resultString = "";
                var match = regex.Match(val);
                while (match.Success)
                {
                    resultString += val.Substring(0, match.Index);
                    int count = 0;
                    int endI = match.Index;
                    for (int i = match.Index + match.Value.Length - 1; i < val.Length; i++)
                    {
                        if (val[i] == '(')
                        {
                            count++;
                        }
                        if (val[i] == ')')
                        {
                            count--;
                        }
                        if (count == 0)
                        {
                            endI = i + 1;
                            break;
                        }
                    }
                    string func = val.Substring(match.Index, endI - match.Index);
                    resultString+="(func)";
                    Result.Add(ParseFunction(func));
                    val = val.Substring(endI);
                    match = regex.Match(val);
                }
                resultString += val;
                return (resultString, Result.ToList());
            }
            private (string result, List<MathExpression> expressions) FindExpressions(string str)
            {
                Regex regex = new Regex(@"(?<![a-zA-Z0-9а-яА-Я)])\((?!func\)|expr\))");
                string val = str;
                string resultString = "";
                var Result = new List<MathExpression>();
                var match = regex.Match(val);
                while (match.Success)
                {
                    resultString += val.Substring(0, match.Index);
                    int count = 0;
                    int endI = match.Index;
                    for (int i = match.Index + match.Value.Length - 1; i < val.Length; i++)
                    {
                        if (val[i] == '(')
                        {
                            count++;
                        }
                        if (val[i] == ')')
                        {
                            count--;
                        }
                        if (count == 0)
                        {
                            endI = i + 1;
                            break;
                        }
                    }
                    string expr = val.Substring(match.Index, endI - match.Index);
                    resultString += "(expr)";
                    Result.Add(ParseExpression(expr));
                    val = val.Substring(endI);
                    match = regex.Match(val);
                }
                resultString += val;
                return (resultString, Result.ToList());
            }
        }
    }
}
