using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Helpers
{
    interface IDropHandler
    {
        void OnDataDropped(IDataObject data);
    }
}
