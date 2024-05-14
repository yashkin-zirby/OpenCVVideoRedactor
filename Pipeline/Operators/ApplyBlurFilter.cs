using ComplexMath.Parser;
using OpenCvSharp;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipeline.Operators
{
    class ApplyBlurFilter: IFrameOperation
    {
        public string Name { get { return nameof(ApplyBlurFilter); } }
        private MathExpression _blur;
        public ApplyBlurFilter()
        {
            _blur = new Value(0);
        }
        public ApplyBlurFilter(Operation operation)
        {
            var mathParser = new MathParser();
            _blur = mathParser.Parse(operation.Parameters
                .FirstOrDefault(n => n.Name == "Мощность размытия" && n.Type == (long)ParameterType.EXPRESSION)?.Value ?? "0");
        }
        public Frame? Apply(Frame frame)
        {
            foreach (var variable in frame.Variables)
            {
                _blur.SetVarriable(variable.Key, variable.Value);
            }
            var blur = 2*(int)Math.Max(Math.Floor(_blur.Calculate().Re),0)+1;
            frame.Image = frame.Image.Blur(new Size(blur,blur));
            return frame;
        }

        public Operation GetOperation()
        {
            return new Operation()
            {
                Name = Name,
                Parameters = new Parameter[] {
                    new Parameter(){Name="Мощность размытия",Type=(long)ParameterType.EXPRESSION,Value=$"{_blur}"}
                }
            };
        }
    }
}
