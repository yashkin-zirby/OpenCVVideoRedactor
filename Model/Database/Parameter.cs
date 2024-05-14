using DevExpress.Mvvm;
using System;
using System.Collections.Generic;

namespace OpenCVVideoRedactor.Model.Database
{
    public enum ParameterType { INPUT=0,OUTPUT=1, EXPRESSION = 2, DEFINITION = 4, OUTPUT_DEFINITION = 5, CONDITION = 8, COLOR = 16, FLAG=32, REQUIRED = 64 };
    public partial class Parameter
    {
        public Parameter() {
            
        }
        public Parameter(Parameter parameter)
        {
            Name = parameter.Name;
            Operation = parameter.Operation;
            Type = parameter.Type;
            Value = parameter.Value;
        }
        public string Name { get; set; } = null!;
        public long Operation { get; set; }
        public long Type { get; set; }
        public string Value { get; set; } = null!;

        public virtual Operation OperationNavigation { get; set; } = null!;
    }
}
