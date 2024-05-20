using OpenCVVideoRedactor.Parser;
using OpenCvSharp;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class CropFrame : IFrameOperation
    {
        public string Name { get { return nameof(CropFrame); } }
        private string _outputWidthVar = "";
        private string _outputHeightVar = "";
        private MathExpression _leftExpression;
        private MathExpression _rightExpression;
        private MathExpression _topExpression;
        private MathExpression _bottomExpression;
        public CropFrame()
        {
            _leftExpression = new Value(0);
            _rightExpression = new Value(0);
            _topExpression = new Value(0);
            _bottomExpression = new Value(0);
        }
        public CropFrame(Operation operation)
        {
            var left = operation.Parameters.FirstOrDefault(n => n.Name == "Слева" && n.Type == (long)ParameterType.EXPRESSION);
            if (left == null) throw new Exception($"Параметры операции CropFrame{operation.Index} не заданы");
            var right = operation.Parameters.FirstOrDefault(n => n.Name == "Справа" && n.Type == (long)ParameterType.EXPRESSION);
            if (right == null) throw new Exception($"Параметры операции CropFrame{operation.Index} не заданы");
            var top = operation.Parameters.FirstOrDefault(n => n.Name == "Сверху" && n.Type == (long)ParameterType.EXPRESSION);
            if (top == null) throw new Exception($"Параметры операции CropFrame{operation.Index} не заданы");
            var bottom = operation.Parameters.FirstOrDefault(n => n.Name == "Снизу" && n.Type == (long)ParameterType.EXPRESSION);
            if (bottom == null) throw new Exception($"Параметры операции CropFrame{operation.Index} не заданы");
            var outputWidth = operation.Parameters.FirstOrDefault(n => n.Name == "Новая высота(height)" && n.Type == (long)ParameterType.OUTPUT);
            var outputHeight = operation.Parameters.FirstOrDefault(n => n.Name == "Новая ширина(width)" && n.Type == (long)ParameterType.OUTPUT);
            var mathParser = new MathParser();
            _leftExpression = mathParser.Parse(left.Value);
            _rightExpression = mathParser.Parse(right.Value);
            _topExpression = mathParser.Parse(top.Value);
            _bottomExpression = mathParser.Parse(bottom.Value);
            if (outputHeight != null) _outputHeightVar = outputHeight.Value;
            if (outputWidth != null) _outputWidthVar = outputWidth.Value;

        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _leftExpression.SetVarriable(variable.Key, variable.Value);
                _rightExpression.SetVarriable(variable.Key, variable.Value);
                _topExpression.SetVarriable(variable.Key, variable.Value);
                _bottomExpression.SetVarriable(variable.Key, variable.Value);
            }
            var left = _leftExpression.Calculate();
            var right = _rightExpression.Calculate();
            var top = _topExpression.Calculate();
            var bottom = _bottomExpression.Calculate();
            var width = frame.Image.Width - right - left;
            var height = frame.Image.Height - bottom - top;
            var imgWidth = (int)(frame.Image.Width - Math.Max(0,right) - Math.Max(0, left));
            var imgHeight = (int)(frame.Image.Height - Math.Max(0, top) - Math.Max(0, bottom));
            if (imgWidth <= 0 || imgHeight <= 0)
            {
                return null;
            }
            Mat result;
            if(frame.Image.Channels() == 4)
                result = new Mat(new Size(width,height),MatType.CV_8UC4,new Scalar(0,0,0,0));
            else
                result = new Mat(new Size(width, height), MatType.CV_8UC3, new Scalar(0, 0, 0));
            frame.Image[new Rect(Math.Max((int)left, 0), Math.Max((int)top, 0), imgWidth, imgHeight)].CopyTo(
                    result[new Rect(Math.Max(-(int)left, 0), Math.Max(-(int)top, 0), imgWidth, imgHeight)]);
            frame.Image = result;
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
                    new Parameter() { Name = "Слева", Type = (long)ParameterType.EXPRESSION, Value = $"{_leftExpression}" },
                    new Parameter() { Name = "Справа", Type = (long)ParameterType.EXPRESSION, Value = $"{_rightExpression}" },
                    new Parameter() { Name = "Сверху", Type = (long)ParameterType.EXPRESSION, Value = $"{_topExpression}" },
                    new Parameter() { Name = "Снизу", Type = (long)ParameterType.EXPRESSION, Value = $"{_bottomExpression}" },
                    new Parameter() { Name = "Новая высота(height)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputHeightVar}" },
                    new Parameter() { Name = "Новая ширина(width)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputWidthVar}" },
                }
            };
        }
    }
}
