using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.Pipeline;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace OpenCVVideoRedactor.ViewModel
{
    public class PipelineViewModel : BindableBase
    {
        private CurrentProjectInfo _currentProject;
        private PipelineController? _controller = null;
        private bool _frameProcessing = false;
        private long _selectedRectId = 0;
        private bool _isPlaying = false;
        private bool _isPaused = false;
        private Action? _play = null;
        private Action? _stop = null;
        public TimeSpan VideoCurrentTime { get; set; }
        public bool IsPlaying { get { return _isPlaying; } set{ _isPlaying = value; if (!value) FilePath = null; RaisePropertiesChanged(nameof(IsPlaying),nameof(IsNotPlaying)); } }
        public bool IsNotPlaying { get { return !_isPlaying; } }
        public string? FilePath { get; set; }
        public ImageSource? CurrentFrame { get; set; }
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
        private VideoProcessingModel _processingModel;
        public PipelineViewModel(CurrentProjectInfo projectInfo, VideoProcessingModel processingModel, PipelineController controller) {
            _currentProject = projectInfo;
            _processingModel = processingModel;
            projectInfo.PropertyChanged += ProjectPropertyChanged;
            if (projectInfo.ProjectInfo != null)
            {
                _controller = controller;
                CurrentFrame = _controller.GetFrame(projectInfo.CurrentTime, _selectedRectId);
            }
            _processingModel.VideoEvent += VideoEventHandler;
        }

        private void VideoEventHandler(object? sender, VideoEventArgs e)
        {
            if(_controller != null && !_processingModel.IsProcessing)
            {
                if (e.VideoEventType == "compile")
                {
                    Task.Factory.StartNew(async () =>
                    {
                        _processingModel.IsProcessing = true;
                        await _controller.GenerateVideo((time, duration) => { 
                            _processingModel.CurrentProcessingValue = time; 
                            _processingModel.MaxProcessingValue = duration; 
                            if(time == duration) _processingModel.IsProcessing = false;
                            IsPlaying = _controller.VideoIsReady;
                        });
                    });
                    return;
                }
                if (!_controller.VideoIsReady)
                {
                    Task.Factory.StartNew(async () =>
                    {
                        _processingModel.IsProcessing = true;
                        await _controller.GenerateVideo((time, duration) => {
                            _processingModel.CurrentProcessingValue = time;
                            _processingModel.MaxProcessingValue = duration;
                            if (time == duration) _processingModel.IsProcessing = false;
                            IsPlaying = _controller.VideoIsReady;
                        }, "H264 (рекомендуемый)", false);
                        if (_currentProject.ProjectInfo != null) FilePath = System.IO.Path.Combine(_currentProject.ProjectInfo.DataFolder, "output.mp4");
                        if (_play != null) _play();
                    });
                    return;
                }
                if (e.VideoEventType == "play")
                {
                    if(_currentProject.ProjectInfo != null) FilePath = System.IO.Path.Combine(_currentProject.ProjectInfo.DataFolder, "output.mp4");
                    if (_play != null) _play();
                }
                else
                {
                    if (_stop != null) _stop();
                }
            }
        }
        public ICommand ConfigureVideoView
        {
            get
            {
                return new DelegateCommand<RoutedEventArgs>((args) => {

                    var mediaElement = args.Source as MediaElement;
                    if (mediaElement != null)
                    {
                        mediaElement.Pause();
                        DispatcherTimer timer = new DispatcherTimer();
                        timer.Interval = TimeSpan.FromSeconds(0.05);
                        timer.Tick += (sender, args) =>
                        {
                            if (_controller != null && _controller.VideoIsReady)
                                _currentProject.CurrentTime = mediaElement.Position;
                            if (mediaElement.Position.Ticks >= _currentProject.MaxDuration && _stop != null) _stop();
                        };
                        _play = () => { mediaElement.Play(); _isPaused = false; timer.Start(); _processingModel.IsPlaying = true; };
                        _stop = () => { mediaElement.Pause(); _isPaused = true; timer.Stop(); _processingModel.IsPlaying = false; };
                        mediaElement.Position = _currentProject.CurrentTime;
                        if(IsPlaying)_play();
                        PropertyChanged += (sender, args) =>
                        {
                            if (args.PropertyName == nameof(VideoCurrentTime))
                            {
                                if (!_isPaused) mediaElement.Pause();
                                mediaElement.Position = VideoCurrentTime;
                                if (!_isPaused) mediaElement.Play();
                            }
                        };
                    }
                });
            }
        }
        private readonly object _currentTimeChanged = new object();
        private void ProjectPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            RaisePropertiesChanged(e.PropertyName);
            if(e.PropertyName == nameof(_currentProject.CurrentTime))
            {
                if (_controller != null && _controller.VideoIsReady)
                {
                    VideoCurrentTime = _currentProject.CurrentTime;
                }
                lock (_currentTimeChanged)
                    Monitor.PulseAll(_currentTimeChanged);
                Task.Run(() =>
                {
                    lock (_currentTimeChanged)
                        if (!Monitor.Wait(_currentTimeChanged, 1000))
                        {
                            while (_frameProcessing) Thread.Sleep(10);
                            _frameProcessing = true;
                            if (_controller != null)
                            {
                                var img = _controller.GetFrame(_currentProject.CurrentTime, _selectedRectId);
                                img.Freeze();
                                CurrentFrame = img;
                            };
                            _frameProcessing = false;
                        }
                });
                if (_frameProcessing) return;
                Task.Factory.StartNew(() => {
                    _frameProcessing = true;
                    if (_controller != null)
                    {
                        var img = _controller.GetFrame(_currentProject.CurrentTime, _selectedRectId);
                        img.Freeze();
                        CurrentFrame = img;
                    };
                    _frameProcessing = false;
                });
                return;
            }
            if (e.PropertyName == nameof(_currentProject.SelectedResource) && _currentProject.SelectedResourceInUse != null)
            {
                while (_frameProcessing) Thread.Sleep(10);
                var resource = _currentProject.SelectedResourceInUse.Resource;
                if (_controller != null)
                {
                    _selectedRectId = resource.Id;
                    _controller.UpdatePipelineFor(resource);
                    IsPlaying = _controller.VideoIsReady;
                    var img = _controller.GetFrame(_currentProject.CurrentTime, _selectedRectId);
                    img.Freeze();
                    CurrentFrame = img;
                };
            }
            if (e.PropertyName == nameof(_currentProject.ResourcesInUse))
            {
                Task.Factory.StartNew(() => {
                    while (_frameProcessing) Thread.Sleep(10);
                    _frameProcessing = true;
                    if (_controller != null && _controller.UpdateResourceList(_currentProject.ResourcesInUse))
                    {
                        IsPlaying = _controller.VideoIsReady;
                        var img = _controller.GetFrame(_currentProject.CurrentTime, _selectedRectId);
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
            _processingModel.VideoEvent -= VideoEventHandler;
        }
    }
}
