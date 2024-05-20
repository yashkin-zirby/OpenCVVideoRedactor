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
    class ChangeLightness : IFrameOperation
    {
        public string Name { get { return nameof(ChangeLightness); } }
        private IMathExpression _step;
        public ChangeLightness()
        {
            _step = new Value(0);
        }
        public ChangeLightness(Operation operation)
        {
            var mathParser = new MathParser();
            _step = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Яркость" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
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
            var image = frame.Image.CvtColor(ColorConversionCodes.BGR2HLS_FULL);
            Mat[] hls = Cv2.Split(image);
            hls[1] += step;
            Cv2.Merge(hls, image);
            frame.Image = image.CvtColor(ColorConversionCodes.HLS2BGR_FULL);
            if (alpha != null)
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
                    new Parameter(){Name="Яркость",Type=(long)ParameterType.EXPRESSION,Value=$"{_step}"}
                }
            };
        }
    }
}
