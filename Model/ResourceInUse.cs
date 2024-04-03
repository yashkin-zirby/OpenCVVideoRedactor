using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Windows;

namespace OpenCVVideoRedactor.Model
{
    public class ResourceInUse
    {
        public ResourceInUse(Resource resource, long maxDuration) { 
            Resource = resource; 
            MaxDuration = maxDuration;
            if (resource.StartTime == null) throw new System.Exception("Невозможно инициализовать класс ResourceInUse неиспользуемым ресурсом");
        }
        public Resource Resource { get; init; }
        public long MaxDuration { get; init; }
        public TimeSpan? ActualDuration { get; set; } = null;
        public double? ActualWidth { get; set; } = null;
        public double? ActualHeight { get; set; } = null;
        public double DurationWidth { get { return (double)Resource.Duration/MaxDuration; } }
        public Thickness StartTimeMargin { get { return new Thickness((double)Resource.StartTime!.Value / MaxDuration,0,0,0); } }

    }
}
