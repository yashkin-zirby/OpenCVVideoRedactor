using System;
using System.Collections.Generic;

namespace OpenCVVideoRedactor.Model.Database
{
    public partial class Parameter
    {
        public string Name { get; set; } = null!;
        public long Operation { get; set; }
        public long Type { get; set; }
        public string Value { get; set; } = null!;

        public virtual Operation OperationNavigation { get; set; } = null!;
    }
}
