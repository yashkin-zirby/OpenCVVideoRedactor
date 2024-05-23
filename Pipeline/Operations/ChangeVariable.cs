using OpenCVVideoRedactor.Parser;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipeline.Operations
{
    class ChangeVariable : IFrameOperation
    {
        public string Name { get { return nameof(ChangeVariable); } }
        private string _variable = "";
        private IMathExpression _expression;
        public ChangeVariable()
        {
            _expression = new Value(0);
        }
        public ChangeVariable(Operation operation) {
            var definition = operation.Parameters.FirstOrDefault(n => n.Name == "ChangeVariableExpression" && n.Type == (long)ParameterType.DEFINITION);
            if (definition == null) throw new Exception($"Параметры операции ChangeVariable{operation.Index} не заданы");
            var definitionParts = definition.Value.Split(":=");
            if(definitionParts.Length != 2) throw new Exception($"Некорректный параметр операции ChangeVariable{operation.Index}");
            var mathParser = new MathParser();
            _variable = definitionParts[0];
            _expression = mathParser.Parse(definitionParts[1]);
        }
        public Frame? Apply(Frame frame)
        {
            foreach(var variable in frame.Variables)
            {
                _expression.SetVarriable(variable.Key, variable.Value);
            }
            frame.Variables[_variable] = _expression.Calculate();
            return frame;
        }

        public Operation GetOperation()
        {
            return new Operation() { Name = Name,
                Parameters = new Parameter[] { 
                    new Parameter() { Name = "ChangeVariableExpression", Type = (long)ParameterType.DEFINITION, Value = _variable+":="+_expression.ToString() },
                } };
        }
    }
}
