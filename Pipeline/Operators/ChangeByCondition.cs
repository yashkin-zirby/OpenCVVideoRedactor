using OpenCVVideoRedactor.Parser;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class ChangeByCondition : IFrameOperation
    {
        public string Name { get { return nameof(ChangeByCondition); } }
        private string _variable = "";
        private MathExpression _expression;
        private MathExpression _leftExpression;
        private MathExpression _rightExpression;
        private string _operator = "=";
        public ChangeByCondition()
        {
            _expression = new Value(0);
            _leftExpression = new Value(0);
            _rightExpression = new Value(0);
        }
        public ChangeByCondition(Operation operation) {
            var regex = new Regex(@"(?<![<>=≠])(<|>|<=|>=|=|≠)(?![<>=≠])");
            var definition = operation.Parameters.FirstOrDefault(n => n.Name == "ChangeVariableExpression" && n.Type == (long)ParameterType.OUTPUT_DEFINITION);
            if (definition == null) throw new Exception($"Параметры операции ChangeByCondition{operation.Index} не заданы");
            var definitionParts = definition.Value.Split(":=");
            if(definitionParts.Length != 2) throw new Exception($"Некорректный параметр операции ChangeByCondition{operation.Index}");
            
            var condition = operation.Parameters.FirstOrDefault(n => n.Name == "Condition" && n.Type == (long)ParameterType.CONDITION);
            if (condition == null) throw new Exception($"Параметры операции ResizeFrame{operation.Index} не заданы");
            var defParts = regex.Split(condition.Value);
            if(defParts.Length != 3) throw new Exception($"Некорректный параметр операции ChangeByCondition{operation.Index}");

            var mathParser = new MathParser();
            _leftExpression = mathParser.Parse(defParts[0]);
            _rightExpression = mathParser.Parse(defParts[2]);
            _operator = defParts[1];
            _variable = definitionParts[0];
            _expression = mathParser.Parse(definitionParts[1]);
        }
        public Frame? Apply(Frame frame)
        {
            foreach(var variable in frame.Variables)
            {
                _expression.SetVarriable(variable.Key, variable.Value);
                _leftExpression.SetVarriable(variable.Key, variable.Value);
                _rightExpression.SetVarriable(variable.Key, variable.Value);
            }
            if (IsConditionRight(_leftExpression.Calculate(), _rightExpression.Calculate(), _operator))
            {
                frame.Variables[_variable] = _expression.Calculate();
            }
            return frame;
        }
        private bool IsConditionRight(double left, double right, string operation)
        {
            switch (operation)
            {
                case "=": return left == right;
                case "<": return left < right;
                case ">": return left > right;
                case "<=": return left <= right;
                case ">=": return left >= right;
                case "≠": return left != right;
            }
            return false;
        }
        public Operation GetOperation()
        {
            return new Operation() { Name = Name,
                Parameters = new Parameter[] {
                    new Parameter() { Name = "Condition", Type = (long)ParameterType.CONDITION, Value = "0=0" },
                    new Parameter() { Name = "ChangeVariableExpression", Type = (long)ParameterType.OUTPUT_DEFINITION, Value = _variable+":="+_expression.ToString() },
                } };
        }
    }
}
