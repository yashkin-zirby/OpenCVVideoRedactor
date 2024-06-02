using DevExpress.Mvvm;
using OpenCVVideoRedactor.Helpers;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OpenCVVideoRedactor.ViewModel
{
    public class TimelineControlViewModel : BindableBase, IDropHandler
    {
        private CurrentProjectInfo _currentProjectInfo;
        private DatabaseContext _dbContext;
        private VideoProcessingModel _videoProcessingModel;
        private double _scale = 0;
        private double _fps = 30;
        public double Scale { 
            get {
                return _scale;
            } set {
                _scale = value;
                RaisePropertiesChanged(nameof(Scale), nameof(ActualScale), nameof(ScaleString));
            }
        }
        public double ActualScale { get { return Math.Pow(10, _scale); } }
        public string VideoState { get; set; } = "▶️";
        public string ScaleString { get { return $"Масштаб: {Math.Round(ActualScale*100)}%"; } }
        public TimeSpan CurrentTime { 
            get { return _currentProjectInfo.CurrentTime; }
            set
            {
                _currentProjectInfo.CurrentTime = value;
            }
        }
        public int CurrentFrame
        {
            get { return (int)(_currentProjectInfo.CurrentTime.Ticks * _fps / 10000000); }
            set
            {
                _currentProjectInfo.CurrentTime = TimeSpan.FromTicks((long)Math.Round(value * (10000000.0 / _fps)));
            }
        }
        public long CurrentTimeTicks
        {
            get { return _currentProjectInfo.CurrentTime.Ticks; }
            set
            {
                _currentProjectInfo.CurrentTime = new TimeSpan(value);
                RaisePropertyChanged(nameof(CurrentFrame));
            }
        }
        public long MaxDuration
        {
            get { return _currentProjectInfo.MaxDuration; }
        }
        public Visibility StartTextVisibility { get { return _currentProjectInfo.ResourcesInUse.Count() > 0 ? Visibility.Collapsed : Visibility.Visible; } }
        public IEnumerable<ResourceInUse> ResourcesInUse { get { return _currentProjectInfo.ResourcesInUse; } }
        public bool IsResourceSelected { get { return _currentProjectInfo.SelectedResource != null && _currentProjectInfo.SelectedResource.IsInUse; } }
        public ResourceInUse? SelectedResource
        {
            get { return _currentProjectInfo.SelectedResource != null?ResourcesInUse.FirstOrDefault(n => n.Resource.Id == _currentProjectInfo.SelectedResource.Id):null; }
            set
            {
                if(value != null)_currentProjectInfo.SelectedResource = value.Resource;
            }
        }
        public TimelineControlViewModel(DatabaseContext dbContext, CurrentProjectInfo currentProjectInfo, VideoProcessingModel videoProcessingModel) {
            _videoProcessingModel = videoProcessingModel;
            _currentProjectInfo = currentProjectInfo;
            _dbContext = dbContext;
            _currentProjectInfo.PropertyChanged += ProjectPropertiesChanged;
            _videoProcessingModel.PropertyChanged += (sender, args) => {
                if (args.PropertyName == nameof(_videoProcessingModel.IsPlaying))
                    VideoState = _videoProcessingModel.IsPlaying ? "◼" : "▶️";
            };
        }
        ~TimelineControlViewModel()
        {
            _currentProjectInfo.PropertyChanged -= ProjectPropertiesChanged;
        }
        private void ProjectPropertiesChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            RaisePropertyChanged(eventArgs.PropertyName);
            if(eventArgs.PropertyName == nameof(_currentProjectInfo.CurrentTime))
            {
                RaisePropertyChanged(nameof(CurrentFrame));
                RaisePropertiesChanged(nameof(CurrentTimeTicks));
                return;
            }
            if(eventArgs.PropertyName == nameof(_currentProjectInfo.SelectedResource)) {
                RaisePropertyChanged(nameof(IsResourceSelected)); return;
            }
            if(eventArgs.PropertyName == nameof(_currentProjectInfo.ResourcesInUse))
            {
                RaisePropertyChanged(nameof(StartTextVisibility)); return;
            }
        }
        public ICommand SplitResourceCommand
        {
            get
            {
                return new DelegateCommand(() => {
                    if (_currentProjectInfo.SelectedResource != null && _currentProjectInfo.SelectedResource.StartTime != null)
                    {
                        new SplitResourceWindow().ShowDialog();
                    }
                });
            }
        }
        public ICommand ClonePipelineCommand
        {
            get
            {
                return new DelegateCommand(() => {
                    if (_currentProjectInfo.SelectedResource != null && _currentProjectInfo.SelectedResource.StartTime != null)
                    {
                        new ClonePipelineWindow().ShowDialog();
                    }
                });
            }
        }
        public ICommand CompileVideoCommand
        {
            get
            {
                return new DelegateCommand(() => {
                    _videoProcessingModel.CompileVideo();
                });
            }
        }
        public ICommand PlayVideoCommand
        {
            get
            {
                return new DelegateCommand(() => {
                    if(VideoState == "▶️")
                    {
                        _videoProcessingModel.PlayVideo();
                    }
                    else
                    {
                        _videoProcessingModel.StopVideo();
                    }
                });
            }
        }
        public ICommand RemoveItemFromInUse
        {
            get
            {
                return new DelegateCommand(() => { 
                    if(_currentProjectInfo.SelectedResource != null && _currentProjectInfo.SelectedResource.StartTime != null)
                    {
                        _currentProjectInfo.SelectedResource.StartTime = null;
                        _dbContext.SaveChanges();
                        _currentProjectInfo.NoticeResourceUpdated(null);
                    }
                });
            }
        }
        public void OnDataDropped(IDataObject data, object? dropSourceObject = null)
        {
            var resource = data.GetData(typeof(Resource)) as Resource;
            if (resource != null)
            {
                if (resource != null && !resource.IsInUse)
                {
                    resource.Layer = ResourcesInUse.Count() >0? ResourcesInUse.Max(n=>n.Resource.Layer)+1:0;
                    resource.StartTime = 0;
                    _dbContext.SaveChanges();
                    _currentProjectInfo.Resources = _currentProjectInfo.Resources.ToList();
                }
                else
                {
                    MessageBox.Show("Данный медиа-файл уже используется");
                }
            }
        }
    }
}
