using DevExpress.Mvvm;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenCVVideoRedactor.Model
{
    public class VideoEventArgs : EventArgs { public string VideoEventType { get; private set; } public VideoEventArgs(string type) { VideoEventType = type; } }
    public delegate void VideoEventHandler(object sender, VideoEventArgs e);
    public class VideoProcessingModel : BindableBase
    {
        private bool _isProcessing;
        private long _currentValue;
        private long _maxValue;
        private bool _isPlaing = false;
        public bool IsProcessing { get { return _isProcessing; } set { _isProcessing = value; RaisePropertyChanged(nameof(IsProcessing)); } }
        public long CurrentProcessingValue { get { return _currentValue; } set { _currentValue = value; RaisePropertyChanged(nameof(CurrentProcessingValue)); } }
        public long MaxProcessingValue { get { return _maxValue; } set { _maxValue = value; RaisePropertyChanged(nameof(MaxProcessingValue)); } }
        public event VideoEventHandler? VideoEvent;
        public bool IsPlaying { get { return _isPlaing; } set { _isPlaing = value; RaisePropertiesChanged(nameof(IsPlaying)); } }
        public void CompileVideo()
        {
            if (VideoEvent != null) VideoEvent.Invoke(null, new VideoEventArgs("compile"));
        }
        public void PlayVideo()
        {
            if (VideoEvent != null) VideoEvent.Invoke(null, new VideoEventArgs("play"));
        }
        public void StopVideo()
        {
            if (VideoEvent != null) VideoEvent.Invoke(null, new VideoEventArgs("stop"));
        }
    }
}
