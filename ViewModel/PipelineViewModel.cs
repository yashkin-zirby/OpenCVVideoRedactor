using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Pipeline;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace OpenCVVideoRedactor.ViewModel
{
    public class PipelineViewModel : BindableBase
    {
        private CurrentProjectInfo _currentProject;
        private PipelineController? _controller = null;
        private bool _frameProcessing = false;
        private bool _isPlaying = false;
        public bool IsPlaying { get { return _isPlaying; } }
        public bool IsNotPlaying { get { return !_isPlaying; } }
        public string? FilePath { get; set; }
        public ImageSource? CurrentFrame { get; set; }
        public Thickness? CurrentResourcePossition { get; set; } = null;
        public double SelectedResourceWidth { get { return _currentProject.SelectedResourceWidth; } }
        public double SelectedResourceHeight { get { return _currentProject.SelectedResourceHeight; } }
        public bool SelectedResourceVisible { get; set; }
        public long VideoWidth
        {
            get { return _currentProject.ProjectInfo!.VideoWidth; }
            set { if (_currentProject.ProjectInfo != null) _currentProject.ProjectInfo.VideoWidth = Math.Max(1, Math.Min(4096, value)); }
        }
        public long VideoHeight
        {
            get { return _currentProject.ProjectInfo!.VideoHeight; }
            set { if (_currentProject.ProjectInfo != null) _currentProject.ProjectInfo.VideoHeight = Math.Max(1, Math.Min(2160, value)); }
        }
        public SolidColorBrush BackgroundColor
        {
            get
            {
                var color = _currentProject.ProjectInfo!.Background;
                var r = (byte)((color >> 16) & 255);
                var g = (byte)((color >> 8) & 255);
                var b = (byte)(color & 255);
                return new SolidColorBrush(Color.FromRgb(r, g, b));
            }
            set
            {
                long color = value.Color.R * 256 * 256;
                color += value.Color.G * 256;
                color += value.Color.B;
                if (_currentProject.ProjectInfo != null) _currentProject.ProjectInfo.Background = color;
            }
        }
        public PipelineViewModel(CurrentProjectInfo projectInfo) {
            _currentProject = projectInfo;
            projectInfo.PropertyChanged += ProjectPropertyChanged;
            if (projectInfo.ProjectInfo != null)
            {
                _controller = new PipelineController(projectInfo);
                CurrentFrame = _controller.GetFrame(projectInfo.CurrentTime);
            }
        }

        private void ProjectPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertiesChanged(e.PropertyName);
            if(e.PropertyName == nameof(_currentProject.CurrentTime) && !_frameProcessing)
            {
                _frameProcessing = true;
                Task.Factory.StartNew(() => {
                    if (_controller != null)
                    {
                        var time = _currentProject.CurrentTime;
                        var img = _controller.GetFrame(_currentProject.CurrentTime);
                        img.Freeze();
                        CurrentFrame = img;
                        _frameProcessing = false;
                        if (_currentProject.CurrentTime != time)
                        {
                            img = _controller.GetFrame(_currentProject.CurrentTime);
                            img.Freeze();
                            CurrentFrame = img;
                        }
                    };
                    SelectedResourceVisible = _currentProject.SelectedResource != null && _currentProject.SelectedResource.StartTime != null ?
                        _currentProject.CurrentTime.Ticks >= _currentProject.SelectedResource.StartTime.Value &&
                        _currentProject.CurrentTime.Ticks <= _currentProject.SelectedResource.Duration + _currentProject.SelectedResource.StartTime.Value
                        : false;
                    _frameProcessing = false;
                });
                return;
            }
            if(e.PropertyName == nameof(_currentProject.SelectedResource))
            {
                SelectedResourceVisible = _currentProject.SelectedResource != null && _currentProject.SelectedResource.StartTime != null ?
                    _currentProject.CurrentTime.Ticks >= _currentProject.SelectedResource.StartTime.Value &&
                    _currentProject.CurrentTime.Ticks <= _currentProject.SelectedResource.Duration + _currentProject.SelectedResource.StartTime.Value
                    : false;
                CurrentResourcePossition = _currentProject.SelectedResource == null? null:
                    new Thickness(_currentProject.SelectedResource.PossitionX, _currentProject.SelectedResource.PossitionY, 0, 0);
            }
            if (e.PropertyName == nameof(_currentProject.ResourcesInUse) && !_frameProcessing)
            {
                _frameProcessing = true;
                Task.Factory.StartNew(() => {
                    if (_controller != null && _controller.UpdateResourceList(_currentProject.ResourcesInUse))
                    {
                        var time = _currentProject.CurrentTime;
                        var img = _controller.GetFrame(time);
                        img.Freeze();
                        CurrentFrame = img;
                    };
                    _frameProcessing = false;
                });
            }
        }

        ~PipelineViewModel()
        {
            _currentProject.PropertyChanged -= ProjectPropertyChanged;
        }
    }
}
