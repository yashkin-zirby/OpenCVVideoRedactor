using OpenCVVideoRedactor.Parser;
using OpenCvSharp;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class RemoveBackgroundColor : IFrameOperation
    {
        public string Name { get { return nameof(RemoveBackgroundColor); } }
        private IMathExpression _hueDifference;
        private IMathExpression _saturationDifference;
        private IMathExpression _lightnessDifference;
        private IMathExpression _maskBlur;
        private Scalar _color;
        public RemoveBackgroundColor()
        {
            _hueDifference = new Value(20);
            _saturationDifference = new Value(100);
            _lightnessDifference = new Value(80);
            _maskBlur = new Value(5);
            _color = new Scalar(0, 128, 0);
        }
        public RemoveBackgroundColor(Operation operation)
        {
            var mathParser = new MathParser();
            var color = operation.Parameters
                .FirstOrDefault(n => n.Name == "Цвет" && n.Type == (long)ParameterType.COLOR)?.Value;
            if (color == null)
            {
                _color = new Scalar(0, 128, 0);
            }
            else
            {
                var components = color.Split(",").Select(n => int.Parse(n)).ToArray();
                _color = new Scalar(components[2], components[1], components[1]);
            }
            _hueDifference = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Разница оттенка" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "20");
            _saturationDifference = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Разница насыщености" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "100");
            _lightnessDifference = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Разница яркости" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "80");
            _maskBlur = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Сила размытия маски" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "5");

        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _hueDifference.SetVarriable(variable.Key, variable.Value);
                _saturationDifference.SetVarriable(variable.Key, variable.Value);
                _lightnessDifference.SetVarriable(variable.Key, variable.Value);
                _maskBlur.SetVarriable(variable.Key, variable.Value);
            }
            var hDif = _hueDifference.Calculate();
            var sDif = _saturationDifference.Calculate();
            var lDif = _lightnessDifference.Calculate();
            var maskBlur = _maskBlur.Calculate();
            Mat? alphaMask = null;
            if (frame.Image.Channels() == 4)
            {
                alphaMask = frame.Image.ExtractChannel(3);
                frame.Image = frame.Image.CvtColor(ColorConversionCodes.BGRA2BGR);
            }
            Mat hls = frame.Image.CvtColor(ColorConversionCodes.BGR2HLS);
            var color = new Mat(1, 1, MatType.CV_8UC3, _color);
            var c = color.CvtColor(ColorConversionCodes.BGR2HLS).At<Vec3b>(0, 0);
            Mat mask = new Mat();
            Cv2.InRange(hls, new Scalar(Math.Max(c.Item0 - hDif, 0), Math.Max(c.Item1 - lDif, 0), Math.Max(c.Item2 - sDif, 0)),
                new Scalar(Math.Min(c.Item0 + hDif, 255), Math.Min(c.Item1 + lDif, 255), Math.Min(c.Item2 + sDif, 255)), mask);
            mask = 255 - mask;
            if (maskBlur > 0) mask = mask.GaussianBlur(new Size(0, 0), sigmaX: maskBlur, sigmaY: maskBlur, borderType: BorderTypes.Default);
            if (alphaMask != null) mask = mask.BitwiseAnd(alphaMask);
            frame.Image = frame.Image.CvtColor(ColorConversionCodes.BGR2BGRA);
            mask.InsertChannel(frame.Image, 3);
            return frame;
        }
        public Operation GetOperation()
        {
            return new Operation()
            {
                Name = Name,
                Parameters = new Parameter[] {
                    new Parameter(){Name="Цвет",Type=(long)ParameterType.COLOR,Value=$"{_color.Val2},{_color.Val1},{_color.Val0}"},
                    new Parameter(){Name="Сила размытия маски",Type=(long)ParameterType.EXPRESSION,Value=$"{_maskBlur}"},
                    new Parameter(){Name="Разница оттенка",Type=(long)ParameterType.EXPRESSION,Value=$"{_hueDifference}"},
                    new Parameter(){Name="Разница насыщености",Type=(long)ParameterType.EXPRESSION,Value=$"{_saturationDifference}"},
                    new Parameter(){Name="Разница яркости",Type=(long)ParameterType.EXPRESSION,Value=$"{_lightnessDifference}"},
                }
            };
        }
    }
}
