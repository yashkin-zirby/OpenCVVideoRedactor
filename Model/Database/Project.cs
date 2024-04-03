using System;
using System.Collections.Generic;

namespace OpenCVVideoRedactor.Model.Database
{
    public partial class Project
    {
        public Project()
        {
            Resources = new HashSet<Resource>();
        }

        public long Id { get; set; }
        public string Title { get; set; } = null!;
        public string DataFolder { get; set; } = null!;
        public long VideoFps { get; set; }
        public long VideoWidth { get; set; }
        public long VideoHeight { get; set; }
        public long Background { get; set; }

        public virtual ICollection<Resource> Resources { get; set; }
    }
}
