using ComplexMath.Parser;
using OpenCvSharp;
using OpenCVVideoRedactor.Model.Database;
using System;
using OpenCvSharp.Dnn;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;
using DevExpress.Mvvm.Native;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class DetectFaceOnFrame : IFrameOperation
    {
        private static string faceModel = @"MLModels\face.caffemodel";
        private static string configFile = @"MLModels\deploy.prototxt";
        private static Net? faceNet = CvDnn.ReadNetFromCaffe(configFile, faceModel);
        public string Name { get { return nameof(DetectFaceOnFrame); } }
        private MathExpression _scaleFactor;
        private Scalar _meanColor;
        private bool _findBiggest = false;
        private string _resultRectX = "";
        private string _resultRectY = "";
        private string _resultRectWidth = "";
        private string _resultRectHeight = "";
        public DetectFaceOnFrame()
        {
            _scaleFactor = new Value(1);
            _meanColor = new Scalar(104, 117, 123);
        }
        public DetectFaceOnFrame(Operation operation)
        {
            var mathParser = new MathParser();
            _scaleFactor = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Масштабирующий фактор" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "1");
            var color = operation.Parameters
                .FirstOrDefault(n => n.Name == "Смещение цвета" && n.Type == (long)ParameterType.COLOR)?.Value;
            if (color == null)
            {
                _meanColor = new Scalar(104, 117, 123);
            }
            else
            {
                var components = color.Split(",").Select(n=>int.Parse(n)).ToArray();
                _meanColor = new Scalar(components[0], components[1], components[2]);
            }
            _findBiggest = operation.Parameters.FirstOrDefault(n => n.Name == "Выбирать наибольшее лицо" && n.Type == (long)ParameterType.FLAG)?.Value == "True";

            _resultRectX = operation.Parameters.FirstOrDefault(n => n.Name == "X" && n.Type == (long)ParameterType.OUTPUT)?.Value ?? "";
            _resultRectY = operation.Parameters.FirstOrDefault(n => n.Name == "Y" && n.Type == (long)ParameterType.OUTPUT)?.Value ?? "";
            _resultRectWidth = operation.Parameters.FirstOrDefault(n => n.Name == "Width" && n.Type == (long)ParameterType.OUTPUT)?.Value ?? "";
            _resultRectHeight = operation.Parameters.FirstOrDefault(n => n.Name == "Height" && n.Type == (long)ParameterType.OUTPUT)?.Value ?? "";
        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _scaleFactor.SetVarriable(variable.Key, variable.Value);
            }
            var image = frame.Image.Channels() == 4 ? frame.Image.CvtColor(ColorConversionCodes.BGRA2BGR) : frame.Image;
            var scaleFactor = _scaleFactor.Calculate().Re;
            int frameHeight = image.Rows;
            int frameWidth = image.Cols;
            if (frameWidth / (double)frameHeight > 1.25)
            {
                Rect? resultRect = null;
                for(int i = 0; i < frameWidth-frameHeight; i+= frameHeight * 2 / 3)
                {
                    var detectedRect = DetectFrame(frame, image[new Rect(i, 0, frameHeight, frameHeight)], scaleFactor);
                    if (detectedRect == null) continue;
                    var rectValue = detectedRect.Value;
                    rectValue.X += i;
                    if (resultRect == null) resultRect = rectValue;
                    else if (_findBiggest ? rectValue.Width * rectValue.Height > resultRect.Value.Width * resultRect.Value.Height :
                        rectValue.Width * rectValue.Height < resultRect.Value.Width * resultRect.Value.Height)resultRect = rectValue;
                }
                var lastRect = DetectFrame(frame, image[new Rect(frameWidth - frameHeight, 0, frameHeight, frameHeight)], scaleFactor);
                if(lastRect != null)
                {
                    var rectValue = lastRect.Value;
                    rectValue.X += frameWidth-frameHeight;
                    if (resultRect == null)resultRect = rectValue;
                    else if (_findBiggest ? rectValue.Width * rectValue.Height > resultRect.Value.Width * resultRect.Value.Height :
                        rectValue.Width * rectValue.Height < resultRect.Value.Width * resultRect.Value.Height) resultRect = rectValue;
                }
                if (frame.Variables.ContainsKey(_resultRectX)) frame.Variables[_resultRectX] = resultRect.HasValue?resultRect.Value.X:0;
                if (frame.Variables.ContainsKey(_resultRectY)) frame.Variables[_resultRectY] = resultRect.HasValue ? resultRect.Value.Y : 0;
                if (frame.Variables.ContainsKey(_resultRectWidth)) frame.Variables[_resultRectWidth] = resultRect.HasValue ? resultRect.Value.Width : frameWidth;
                if (frame.Variables.ContainsKey(_resultRectHeight)) frame.Variables[_resultRectHeight] = resultRect.HasValue ? resultRect.Value.Height : frameHeight;
                return frame;
            }
            if(frameHeight / (double)frameWidth > 1.25)
            {
                Rect? resultRect = null;
                for (int i = 0; i < frameHeight - frameWidth; i += frameWidth * 2 / 3)
                {
                    var detectedRect = DetectFrame(frame, image[new Rect(0, i, frameWidth, frameWidth)], scaleFactor);
                    if (detectedRect == null) continue;
                    var rectValue = detectedRect.Value;
                    rectValue.Y += i;
                    if (resultRect == null) resultRect = rectValue;
                    else if (_findBiggest ? rectValue.Width * rectValue.Height > resultRect.Value.Width * resultRect.Value.Height :
                        rectValue.Width * rectValue.Height < resultRect.Value.Width * resultRect.Value.Height) resultRect = rectValue;
                }
                var lastRect = DetectFrame(frame, image[new Rect(0, frameHeight - frameWidth, frameWidth, frameWidth)], scaleFactor);
                if (lastRect != null)
                {
                    var rectValue = lastRect.Value;
                    rectValue.Y += frameHeight - frameWidth;
                    if (resultRect == null) resultRect = rectValue;
                    else if (_findBiggest ? rectValue.Width * rectValue.Height > resultRect.Value.Width * resultRect.Value.Height :
                        rectValue.Width * rectValue.Height < resultRect.Value.Width * resultRect.Value.Height) resultRect = rectValue;
                }
                if (frame.Variables.ContainsKey(_resultRectX)) frame.Variables[_resultRectX] = resultRect.HasValue ? resultRect.Value.X : 0;
                if (frame.Variables.ContainsKey(_resultRectY)) frame.Variables[_resultRectY] = resultRect.HasValue ? resultRect.Value.Y : 0;
                if (frame.Variables.ContainsKey(_resultRectWidth)) frame.Variables[_resultRectWidth] = resultRect.HasValue ? resultRect.Value.Width : frameWidth;
                if (frame.Variables.ContainsKey(_resultRectHeight)) frame.Variables[_resultRectHeight] = resultRect.HasValue ? resultRect.Value.Height : frameHeight;
                return frame;
            }
            var rect = DetectFrame(frame,image,scaleFactor);
            if (rect != null)
            {
                if (frame.Variables.ContainsKey(_resultRectX)) frame.Variables[_resultRectX] = rect.Value.X;
                if (frame.Variables.ContainsKey(_resultRectY)) frame.Variables[_resultRectY] = rect.Value.Y;
                if (frame.Variables.ContainsKey(_resultRectWidth)) frame.Variables[_resultRectWidth] = rect.Value.Width;
                if (frame.Variables.ContainsKey(_resultRectHeight)) frame.Variables[_resultRectHeight] = rect.Value.Height;
            }
            else
            {
                if (frame.Variables.ContainsKey(_resultRectX)) frame.Variables[_resultRectX] = 0;
                if (frame.Variables.ContainsKey(_resultRectY)) frame.Variables[_resultRectY] = 0;
                if (frame.Variables.ContainsKey(_resultRectWidth)) frame.Variables[_resultRectWidth] = frameWidth;
                if (frame.Variables.ContainsKey(_resultRectHeight)) frame.Variables[_resultRectHeight] = frameHeight;
            }
            return frame;
        }
        private Rect? DetectFrame(Frame frame, Mat image, double scaleFactor)
        {
            int frameHeight = image.Rows;
            int frameWidth = image.Cols;
            if (faceNet != null)
            {
                using var blob = CvDnn.BlobFromImage(image, scaleFactor, new Size(300, 300),
                _meanColor, false, false);
                faceNet.SetInput(blob, "data");

                using var detection = faceNet.Forward("detection_out");
                using var detectionMat = new Mat(detection.Size(2), detection.Size(3), MatType.CV_32F,
                    detection.Ptr(0));
                for (int i = 0; i < detectionMat.Rows; i++)
                {
                    float confidence = detectionMat.At<float>(i, 2);

                    if (confidence > 0.7)
                    {
                        int x1 = (int)(detectionMat.At<float>(i, 3) * frameWidth);
                        int y1 = (int)(detectionMat.At<float>(i, 4) * frameHeight);
                        int x2 = (int)(detectionMat.At<float>(i, 5) * frameWidth);
                        int y2 = (int)(detectionMat.At<float>(i, 6) * frameHeight);
                        return new Rect(x1, y1, x2-x1, y2-y1);
                    }
                }
            }
            return null;
        }
        public Operation GetOperation()
        {
            return new Operation()
            {
                Name = Name,
                Parameters = new Parameter[] {
                    new Parameter(){Name="Масштабирующий фактор",Type=(long)ParameterType.EXPRESSION,Value=$"{_scaleFactor}"},
                    new Parameter(){Name="Выбирать наибольшее лицо",Type=(long)ParameterType.FLAG,Value=$"{_findBiggest}"},
                    new Parameter(){Name="Смещение цвета",Type=(long)ParameterType.COLOR,Value=$"{_meanColor.Val0},{_meanColor.Val1},{_meanColor.Val2}"},
                    new Parameter(){Name="X",Type=(long)ParameterType.OUTPUT,Value=$"{_resultRectX}"},
                    new Parameter(){Name="Y",Type=(long)ParameterType.OUTPUT,Value=$"{_resultRectY}"},
                    new Parameter(){Name="Width",Type=(long)ParameterType.OUTPUT,Value=$"{_resultRectWidth}"},
                    new Parameter(){Name="Height",Type=(long)ParameterType.OUTPUT,Value=$"{_resultRectHeight}"},
                }
            };
        }
    }
}
