using OpenCVVideoRedactor.Helpers;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.Pipeline;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using OpenCVVideoRedactor.Pipeline.Operations;
using System.Windows;

namespace OpenCVVideoRedactor.Pipepline
{
    public class FrameProcessingPipeline
    {
        private static string[] _premiumOperations = new string[] { nameof(DetectFaceOnFrame), nameof(RemoveBackgroundColor) };
        private IEnumerable<IFrameOperation?> _operations;
        public FrameProcessingPipeline(Resource resource) {
            var types = GetOperationsTypes();
            _operations = resource.Operations.OrderBy(n => n.Index).Select(n => {
                IFrameOperation? operation = null;
                try
                {
                    operation = (IFrameOperation?)types.FirstOrDefault(k => k.Name == n.Name)
                        ?.GetConstructor(new Type[] { typeof(Operation) })?.Invoke(new object[] { n });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
                return operation;
            });

        }
        public static IEnumerable<IFrameOperation?> GetOperations(){
            return GetOperationsTypes().Select(n => (IFrameOperation?)n?.GetConstructor(new Type[] { })?.Invoke(new object[] { }));
        }
        public static IEnumerable<Type> GetOperationsTypes()
        {
            var type = typeof(IFrameOperation);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && p.IsClass && !p.IsAbstract &&
                        (KeyActivationHelper.IsActivated || !_premiumOperations.Contains(p.Name)));
        }
        public Frame? Apply(Frame frame)
        {
            foreach(var operation in _operations)
            {
                if (operation != null)
                {
                    var result = operation.Apply(frame);
                    if (result == null) return null;
                    frame = result;
                }
            }
            return frame;
        }
    }
}
