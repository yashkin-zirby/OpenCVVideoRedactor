using OpenCVVideoRedactor.Parser;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class ResizeFrame : IFrameOperation
    {
        public string Name { get { return nameof(ResizeFrame); } }
        private string _outputWidthVar = "";
        private string _outputHeightVar = "";
        private IMathExpression _widthExpression;
        private IMathExpression _heightExpression;
        public ResizeFrame()
        {
            _widthExpression = new Value(0);
            _heightExpression = new Value(0);
        }
        public ResizeFrame(Operation operation)
        {
            var widthExpression = operation.Parameters.FirstOrDefault(n => n.Name == "Ширина" && n.Type == (long)ParameterType.EXPRESSION);
            if (widthExpression == null) throw new Exception($"Параметры операции ResizeFrame{operation.Index} не заданы");
            var heightExpression = operation.Parameters.FirstOrDefault(n => n.Name == "Высота" && n.Type == (long)ParameterType.EXPRESSION);
            if (heightExpression == null) throw new Exception($"Параметры операции ResizeFrame{operation.Index} не заданы");
            var outputWidth = operation.Parameters.FirstOrDefault(n => n.Name == "Новая Ширина(width)" && n.Type == (long)ParameterType.OUTPUT);
            var outputHeight = operation.Parameters.FirstOrDefault(n => n.Name == "Новая Высота(height)" && n.Type == (long)ParameterType.OUTPUT);
            var mathParser = new MathParser();
            _widthExpression = mathParser.Parse(widthExpression.Value);
            _heightExpression = mathParser.Parse(heightExpression.Value);
            if (outputHeight != null) _outputHeightVar = outputHeight.Value;
            if (outputWidth != null) _outputWidthVar = outputWidth.Value;

        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _widthExpression.SetVarriable(variable.Key, variable.Value);
                _heightExpression.SetVarriable(variable.Key, variable.Value);
            }
            var width = _widthExpression.Calculate();
            var height = _heightExpression.Calculate();
            if (width < 1 && height < 1)
            {
                return frame;
            }
            if(width < 1)
            {
                width = frame.Image.Width * height / frame.Image.Height;
            }
            if (height < 1)
            {
                height = frame.Image.Height * width / frame.Image.Width;
            }
            frame.Image = frame.Image.Resize(new OpenCvSharp.Size(width,height));
            frame.Variables["width"] = frame.Image.Width;
            frame.Variables["height"] = frame.Image.Height;
            if (frame.Variables.ContainsKey(_outputHeightVar)) frame.Variables[_outputHeightVar] = frame.Image.Height;
            if (frame.Variables.ContainsKey(_outputWidthVar)) frame.Variables[_outputWidthVar] = frame.Image.Width;
            return frame;
        }

        public Operation GetOperation()
        {
            return new Operation()
            {
                Name = Name,
                Parameters = new Parameter[] {
                    new Parameter() { Name = "Ширина", Type = (long)ParameterType.EXPRESSION, Value = $"{_widthExpression}" },
                    new Parameter() { Name = "Высота", Type = (long)ParameterType.EXPRESSION, Value = $"{_heightExpression}" },
                    new Parameter() { Name = "Новая Ширина(width)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputHeightVar}" },
                    new Parameter() { Name = "Новая Высота(height)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputWidthVar}" },
                }
            };
        }
    }
}
