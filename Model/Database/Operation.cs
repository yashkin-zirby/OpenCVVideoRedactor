using System;
using System.Collections.Generic;

namespace OpenCVVideoRedactor.Model.Database
{
    public partial class Operation
    {
        public Operation()
        {
            Parameters = new HashSet<Parameter>();
        }
        public long Id { get; set; }
        public long? Source { get; set; }
        public long Index { get; set; }
        public string Name { get; set; } = null!;

        public virtual Resource? SourceNavigation { get; set; }
        public virtual ICollection<Parameter> Parameters { get; set; }
    }
}
