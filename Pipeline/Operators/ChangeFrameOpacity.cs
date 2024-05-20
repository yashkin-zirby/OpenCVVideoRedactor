using OpenCVVideoRedactor.Parser;
using OpenCvSharp;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class ChangeFrameOpacity : IFrameOperation
    {
        public string Name { get { return nameof(ChangeFrameOpacity); } }
        private MathExpression _opacity;
        public ChangeFrameOpacity()
        {
            _opacity = new Value(1);
        }
        public ChangeFrameOpacity(Operation operation)
        {
            var mathParser = new MathParser();
            _opacity = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Прозрачность" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "1");
        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _opacity.SetVarriable(variable.Key, variable.Value);
            }
            var opacity = Math.Max(Math.Min(_opacity.Calculate(),1), 0);
            if (frame.Image.Channels() == 3) frame.Image = frame.Image.CvtColor(ColorConversionCodes.BGR2BGRA);
            Mat[] channels = frame.Image.Split();
            channels[3] = channels[3] * opacity;
            Cv2.Merge(channels, frame.Image);
            return frame;
        }

        public Operation GetOperation()
        {
            return new Operation()
            {
                Name = Name,
                Parameters = new Parameter[] {
                    new Parameter(){Name="Прозрачность",Type=(long)ParameterType.EXPRESSION,Value=$"{_opacity}"}
                }
            };
        }
    }
}
