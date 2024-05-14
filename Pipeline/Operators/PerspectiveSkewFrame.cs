using ComplexMath.Parser;
using OpenCvSharp;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class PerspectiveSkewFrame : IFrameOperation
    {
        public string Name { get { return nameof(PerspectiveSkewFrame); } }
        private MathExpression _pointOffset1X;
        private MathExpression _pointOffset1Y;
        private MathExpression _pointOffset2X;
        private MathExpression _pointOffset2Y;
        private MathExpression _pointOffset3X;
        private MathExpression _pointOffset3Y;
        private MathExpression _pointOffset4X;
        private MathExpression _pointOffset4Y;
        private string _outputWidthVar = "";
        private string _outputHeightVar = "";
        public PerspectiveSkewFrame()
        {
            _pointOffset1X = new Value(0);
            _pointOffset1Y = new Value(0);
            _pointOffset2X = new Value(0);
            _pointOffset2Y = new Value(0);
            _pointOffset3X = new Value(0);
            _pointOffset3Y = new Value(0);
            _pointOffset4X = new Value(0);
            _pointOffset4Y = new Value(0);
        }
        public PerspectiveSkewFrame(Operation operation)
        {
            var mathParser = new MathParser();
            _pointOffset1X = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Левый верх смещение X" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _pointOffset1Y = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Левый верх смещение Y" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _pointOffset2X = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Правый верх смещение X" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _pointOffset2Y = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Правый верх смещение Y" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _pointOffset3X = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Правый низ смещение X" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _pointOffset3Y = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Правый низ смещение Y" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _pointOffset4X = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Левый низ смещение X" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _pointOffset4Y = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Левый низ смещение Y" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
            _outputHeightVar = operation.Parameters.FirstOrDefault(n => n.Name == "Новая высота(height)" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "";
            _outputWidthVar = operation.Parameters.FirstOrDefault(n => n.Name == "Новая ширина(width)" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "";
        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _pointOffset1X.SetVarriable(variable.Key, variable.Value);
                _pointOffset1Y.SetVarriable(variable.Key, variable.Value);
                _pointOffset2X.SetVarriable(variable.Key, variable.Value);
                _pointOffset2Y.SetVarriable(variable.Key, variable.Value);
                _pointOffset3X.SetVarriable(variable.Key, variable.Value);
                _pointOffset3Y.SetVarriable(variable.Key, variable.Value);
                _pointOffset4X.SetVarriable(variable.Key, variable.Value);
                _pointOffset4Y.SetVarriable(variable.Key, variable.Value);
            }
            var Offset1x = (float)_pointOffset1X.Calculate().Re;
            var Offset1y = (float)_pointOffset1Y.Calculate().Re;
            var Offset2x = (float)_pointOffset2X.Calculate().Re;
            var Offset2y = (float)_pointOffset2Y.Calculate().Re;
            var Offset3x = (float)_pointOffset3X.Calculate().Re;
            var Offset3y = (float)_pointOffset3Y.Calculate().Re;
            var Offset4x = (float)_pointOffset4X.Calculate().Re;
            var Offset4y = (float)_pointOffset4Y.Calculate().Re;
            var width = frame.Image.Width-1;
            var height = frame.Image.Height-1;
            var transformPoints = new Point2f[] {
                new Point2f(Offset1x,Offset1y),
                new Point2f(width+Offset2x,Offset2y),
                new Point2f(width+Offset3x, height+Offset3y),
                new Point2f(Offset4x,height+Offset4y),
            };
            var rect = Cv2.BoundingRect(transformPoints);
            var transformMatrix = Cv2.GetPerspectiveTransform(new Point2f[] {
                new Point2f(0,0),
                new Point2f(width,0),
                new Point2f(width,height),
                new Point2f(0,height),
            }, transformPoints);
            Mat A = Mat.Eye(3, 3, MatType.CV_64F); A.At<double>(0, 2) = -rect.Left; A.At<double>(1, 2) = -rect.Top;
            transformMatrix = A * transformMatrix;
            Mat result = new Mat();
            Cv2.WarpPerspective(frame.Image, result, transformMatrix, new OpenCvSharp.Size(rect.Width, rect.Height));
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
                    new Parameter() { Name = "Левый верх смещение X", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset1X}" },
                    new Parameter() { Name = "Левый верх смещение Y", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset1Y}" },
                    new Parameter() { Name = "Правый верх смещение X", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset2X}" },
                    new Parameter() { Name = "Правый верх смещение Y", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset2Y}" },
                    new Parameter() { Name = "Правый низ смещение X", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset3X}" },
                    new Parameter() { Name = "Правый низ смещение Y", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset3Y}" },
                    new Parameter() { Name = "Левый низ смещение X", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset4X}" },
                    new Parameter() { Name = "Левый низ смещение Y", Type = (long)ParameterType.EXPRESSION, Value = $"{_pointOffset4Y}" },
                    new Parameter() { Name = "Новая высота(height)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputHeightVar}" },
                    new Parameter() { Name = "Новая ширина(width)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputWidthVar}" },
                }
            };
        }
    }
}
