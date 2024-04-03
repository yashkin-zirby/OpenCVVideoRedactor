using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Pipepline
{
    public class FrameProcessingPipeline
    {

        public FrameProcessingPipeline(Resource resource) {
            var types = GetOperationsTypes();
            var operations = resource.Operations.Select(n => (IFrameOperation?)types.FirstOrDefault(k=>k.Name ==n.Name)
                ?.GetConstructor(new Type[] { })?.Invoke(null));

        }
        private IEnumerable<Type> GetOperationsTypes()
        {
            var type = typeof(IFrameOperation);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract);
        }
        public Frame Apply(Frame frame)
        {
            return frame;
        }
    }
}
