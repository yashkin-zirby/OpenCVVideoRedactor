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
    class ChangeSaturation : IFrameOperation
    {
        public string Name { get { return nameof(ChangeSaturation); } }
        private MathExpression _step;
        public ChangeSaturation()
        {
            _step = new Value(0);
        }
        public ChangeSaturation(Operation operation)
        {
            var mathParser = new MathParser();
            _step = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Насыщенность" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _step.SetVarriable(variable.Key, variable.Value);
            }
            var step = (int)_step.Calculate();
            Mat? alpha = null;
            if (frame.Image.Channels() == 4) alpha = frame.Image.ExtractChannel(3);
            var image = frame.Image.CvtColor(ColorConversionCodes.BGR2HSV_FULL);
            Mat[] hsv = Cv2.Split(image);
            hsv[1] += step;
            Cv2.Merge(hsv, image);
            frame.Image = image.CvtColor(ColorConversionCodes.HSV2BGR_FULL);
            if(alpha != null)
            {
                frame.Image = frame.Image.CvtColor(ColorConversionCodes.BGR2BGRA);
                Mat[] channels = frame.Image.Split();
                channels[3] = alpha;
                Cv2.Merge(channels, frame.Image);
            }
            return frame;
        }

        public Operation GetOperation()
        {
            return new Operation()
            {
                Name = Name,
                Parameters = new Parameter[] {
                    new Parameter(){Name="Насыщенность",Type=(long)ParameterType.EXPRESSION,Value=$"{_step}"}
                }
            };
        }
    }
}
