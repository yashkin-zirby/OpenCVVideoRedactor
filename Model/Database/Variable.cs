using System;
using System.Collections.Generic;

namespace OpenCVVideoRedactor.Model.Database
{
    public partial class Variable
    {
        public string Name { get; set; } = null!;
        public long Resource { get; set; }
        public double Value { get; set; }

        public virtual Resource ResourceNavigation { get; set; } = null!;
    }
}
