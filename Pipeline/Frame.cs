using OpenCvSharp;
using System;
using System.Collections.Generic;

namespace OpenCVVideoRedactor.Pipeline
{
    public class Frame
    {
        public long Id { get; set; }
        public Mat Image { get; set; }
        public Dictionary<string, double> Variables { get; set; }
        public Frame(long id, Mat image, Dictionary<string, double> variables) {
            Id = id;
            Image = image;
            Variables = variables;
        }
    }
}
