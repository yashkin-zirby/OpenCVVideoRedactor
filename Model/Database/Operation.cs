using System;
using System.Collections.Generic;

namespace OpenCVVideoRedactor.Model.Database
{
    public partial class Operation
    {
        public Operation()
        {
            InverseNextNavigation = new HashSet<Operation>();
            Parameters = new HashSet<Parameter>();
        }

        public long Id { get; set; }
        public long Source { get; set; }
        public long? Next { get; set; }
        public string Name { get; set; } = null!;

        public virtual Operation? NextNavigation { get; set; }
        public virtual Resource SourceNavigation { get; set; } = null!;
        public virtual ICollection<Operation> InverseNextNavigation { get; set; }
        public virtual ICollection<Parameter> Parameters { get; set; }
    }
}
