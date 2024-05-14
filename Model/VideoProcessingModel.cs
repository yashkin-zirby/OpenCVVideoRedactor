using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Model
{
    public class VideoProcessingModel : BindableBase
    {
        private bool _isProcessing;
        private long _currentValue;
        private long _maxValue;
        public bool IsProcessing { get { return _isProcessing; } set { _isProcessing = value; RaisePropertyChanged(nameof(IsProcessing)); } }
        public long CurrentProcessingValue { get { return _currentValue; } set { _currentValue = value; RaisePropertyChanged(nameof(CurrentProcessingValue)); } }
        public long MaxProcessingValue { get { return _maxValue; } set { _maxValue = value; RaisePropertyChanged(nameof(MaxProcessingValue)); } }
    }
}
