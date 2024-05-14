using ComplexMath.Parser;
using OpenCvSharp;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class RotateFrame : IFrameOperation
    {
        public string Name { get { return nameof(RotateFrame); } }
        private MathExpression _AngleExpression;
        private string _outputWidthVar = "";
        private string _outputHeightVar = "";
        public RotateFrame()
        {
            _AngleExpression = new Value(0);
        }
        public RotateFrame(Operation operation)
        {
            var angleExpression = operation.Parameters.FirstOrDefault(n => n.Name == "Угол поворота" && n.Type == (long)ParameterType.EXPRESSION);
            if (angleExpression == null) throw new Exception($"Параметры операции RotateFrame{operation.Index} не заданы");
            _outputHeightVar = operation.Parameters.FirstOrDefault(n => n.Name == "Новая высота(height)" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "";
            _outputWidthVar = operation.Parameters.FirstOrDefault(n => n.Name == "Новая ширина(width)" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "";
            var mathParser = new MathParser();
            _AngleExpression = mathParser.Parse(angleExpression.Value);
        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _AngleExpression.SetVarriable(variable.Key, variable.Value);
            }
            var angle = _AngleExpression.Calculate().Re;
            Mat result = new Mat();
            var width = frame.Image.Width;
            var height = frame.Image.Height;
            var rotationMatrix = Cv2.GetRotationMatrix2D(new Point(width/2, height/2), angle, 1);
            var abs_cos = Math.Abs(rotationMatrix.At<double>(0, 0));
            var abs_sin = Math.Abs(rotationMatrix.At<double>(0, 1));

            var bound_w = (int)(height * abs_sin + width * abs_cos);
            var bound_h = (int)(height * abs_cos + width * abs_sin);
            rotationMatrix.At<double>(0, 2) += bound_w / 2 - width/2;
            rotationMatrix.At<double>(1, 2) += bound_h / 2 - height/2;

            Cv2.WarpAffine(frame.Image, result, rotationMatrix, new Size(bound_w, bound_h));
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
                    new Parameter() { Name = "Угол поворота", Type = (long)ParameterType.EXPRESSION, Value = $"{_AngleExpression}" },
                    new Parameter() { Name = "Новая высота(height)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputHeightVar}" },
                    new Parameter() { Name = "Новая ширина(width)", Type = (long)ParameterType.OUTPUT, Value = $"{_outputWidthVar}" },
                }
            };
        }
    }
}
