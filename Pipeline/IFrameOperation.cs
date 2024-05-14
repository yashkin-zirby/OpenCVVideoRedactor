using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipeline
{
    public interface IFrameOperation
    {
        public string Name { get; }
        public Frame? Apply(Frame frame);
        public Operation GetOperation();
    }
}
