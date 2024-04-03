using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace OpenCVVideoRedactor.Model.Database
{
    public enum ResourceType { IMAGE, VIDEO, AUDIO }
    public partial class Resource
    {
        public Resource()
        {
            Operations = new HashSet<Operation>();
            Variables = new HashSet<Variable>();
        }

        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public long ProjectId { get; set; }
        public long PossitionX { get; set; }
        public long PossitionY { get; set; }
        public long? StartTime { get; set; }
        public long Duration { get; set; }
        public long Type { get; set; }
        public long Layer { get; set; }

        public virtual Project Project { get; set; } = null!;
        public virtual ICollection<Operation> Operations { get; set; }
        public virtual ICollection<Variable> Variables { get; set; }
        [NotMapped]
        public bool IsInUse { get { return StartTime != null; } }
        [NotMapped]
        public bool IsNotAudio { get { return Type != (int)ResourceType.AUDIO; } }
        private Resource(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException("info");

            SerializationInfoEnumerator enumerator = info.GetEnumerator();

            while (enumerator.MoveNext())
            {
                switch (enumerator.Name)
                {
                    case nameof(Id):
                        if (enumerator.Value != null)
                            Id = (long)enumerator.Value;
                        break;
                    case nameof(Name):
                        if (enumerator.Value != null)
                            Name = (string)enumerator.Value;
                        break;
                    case nameof(ProjectId):
                        if (enumerator.Value != null)
                            ProjectId = (long)enumerator.Value;
                        break;
                    case nameof(PossitionX):
                        if (enumerator.Value != null)
                            PossitionX = (long)enumerator.Value;
                        break;
                    case nameof(PossitionY):
                        if (enumerator.Value != null)
                            PossitionY = (long)enumerator.Value;
                        break;
                    case nameof(StartTime):
                        StartTime = (long?)enumerator.Value;
                        break;
                    case nameof(Duration):
                        if (enumerator.Value != null)
                            Duration = (long)enumerator.Value;
                        break;
                    case nameof(Layer):
                        if (enumerator.Value != null)
                            Layer = (int)enumerator.Value;
                        break;
                    case nameof(Type):
                        if (enumerator.Value != null)
                            Type = (int)enumerator.Value;
                        break;
                    default:
                        break;
                }
            }
            Operations = new HashSet<Operation>();
        }
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Id), Id);
            info.AddValue(nameof(Name), Name);
            info.AddValue(nameof(ProjectId), ProjectId);
            info.AddValue(nameof(PossitionX), PossitionX);
            info.AddValue(nameof(PossitionY), PossitionY);
            info.AddValue(nameof(StartTime), StartTime);
            info.AddValue(nameof(Duration), Duration);
            info.AddValue(nameof(Layer), Layer);
            info.AddValue(nameof(Type), Type);
        }
        public bool IsPropertiesChanged(Resource other)
        {
            return !(other.Type == Type && other.Layer == Layer &&
                other.StartTime == StartTime && other.Duration == Duration &&
                other.PossitionX == PossitionX && other.PossitionY == PossitionY);
        }
    }
}
