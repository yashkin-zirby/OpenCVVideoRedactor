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
    class ApplyGrayFilter : IFrameOperation
    {
        public string Name { get { return nameof(ApplyGrayFilter); } }
        public ApplyGrayFilter()
        {
        }
        public ApplyGrayFilter(Operation operation)
        {
        }
        public Frame? Apply(Frame frame)
        {
            if (frame.Image.Channels() == 4)
            {
                var alpha = frame.Image.ExtractChannel(3);
                var img = new Mat();
                Cv2.BitwiseAnd(frame.Image, frame.Image, img, alpha);
                img = img.CvtColor(ColorConversionCodes.RGBA2GRAY);
                frame.Image = img.CvtColor(ColorConversionCodes.GRAY2BGRA);
                Cv2.InsertChannel(alpha, frame.Image, 3);
                return frame;
            }
            frame.Image = frame.Image.CvtColor(ColorConversionCodes.BGR2GRAY);
            frame.Image = frame.Image.CvtColor(ColorConversionCodes.GRAY2BGR);
            return frame;
        }

        public Operation GetOperation()
        {
            return new Operation()
            {
                Name = Name,
                Parameters = new Parameter[] {
                }
            };
        }
    }
}
