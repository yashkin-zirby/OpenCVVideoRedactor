using DevExpress.Mvvm;
using FFMpegCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Xaml.Behaviors.Core;
using OpenCVVideoRedactor.Helpers;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.VisualStyles;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace OpenCVVideoRedactor.ViewModel
{
    public class SplitResourceViewModel : BindableBase
    {
        private CurrentProjectInfo _projectInfo;
        private DatabaseContext _dbContext;
        public List<TimeSpan> Markers { get; set; } = new List<TimeSpan>();
        public long CurrentPossition { get; set; }
        public TimeSpan CurrentTime { get { return TimeSpan.FromTicks(CurrentPossition); } set { CurrentPossition = value.Ticks; } }
        public TimeSpan? SelectedMarker { get; set; } = null;
        public long Duration { get; set; } = 0;
        public string ResourcePath { get; set; } = "";
        public bool IsPaused { get; set; }
        public bool IsLoading { get; set; }
        public long LoadingValue { get; set; }

        public SplitResourceViewModel(CurrentProjectInfo projectInfo, DatabaseContext dbContext) {
            _projectInfo = projectInfo;
            _dbContext = dbContext;
            IsPaused = true;
            IsLoading = false;
            if (_projectInfo.SelectedResourceInUse != null && _projectInfo.SelectedResource != null && _projectInfo.ProjectInfo != null)
            {
                Duration = _projectInfo.SelectedResourceInUse.ActualDuration != null && _projectInfo.SelectedResource.Type != (int)ResourceType.IMAGE ? 
                    _projectInfo.SelectedResourceInUse.ActualDuration.Value.Ticks : _projectInfo.SelectedResource.Duration;
                var resource = _projectInfo.SelectedResource;
                var dir = "";
                switch ((ResourceType)resource.Type)
                {
                    case ResourceType.IMAGE: dir = _projectInfo.ImagesDir; break;
                    case ResourceType.AUDIO: dir = _projectInfo.AudiosDir; break;
                    case ResourceType.VIDEO: dir = _projectInfo.VideosDir; break;
                }
                ResourcePath = Path.Combine(dir, resource.Name);
            }

        }
        public ICommand PlayCommand { get; private set; } = new DelegateCommand(() => { });
        public ICommand StopCommand { get; private set; } = new DelegateCommand(() => { });
        public ICommand ConfigureDuration
        {
            get
            {
                return new DelegateCommand<RoutedEventArgs>((args) => {
                    
                    var mediaElement = args.Source as MediaElement;
                    if(mediaElement != null)
                    {
                        mediaElement.Pause();
                        if (_projectInfo.SelectedResource.Type != (int)ResourceType.IMAGE)
                        {
                            PlayCommand = new DelegateCommand(() => { mediaElement.Play(); IsPaused = false; });
                            StopCommand = new DelegateCommand(() => { mediaElement.Pause(); IsPaused = true; });
                            RaisePropertiesChanged(nameof(PlayCommand), nameof(StopCommand));
                            DispatcherTimer timer = new DispatcherTimer();
                            timer.Interval = TimeSpan.FromSeconds(0.05);
                            timer.Tick += (sender, args) =>
                            {
                                CurrentPossition = mediaElement.Position.Ticks;
                            };
                            timer.Start();
                            PropertyChanged += (sender, args) =>
                            {
                                if (args.PropertyName == nameof(CurrentPossition))
                                {
                                    if (!IsPaused) mediaElement.Pause();
                                    mediaElement.Position = TimeSpan.FromTicks(CurrentPossition);
                                    if (!IsPaused) mediaElement.Play();
                                    RaisePropertiesChanged(nameof(CurrentTime));
                                }
                            };
                        }
                    }
                });
            }
        }
        public ICommand AddMarkerCommand
        {
            get
            {
                return new DelegateCommand(() => {
                    if (!Markers.Contains(TimeSpan.FromTicks(CurrentPossition)) && CurrentPossition>0 && CurrentPossition < Duration)
                    {
                        Markers.Add(TimeSpan.FromTicks(CurrentPossition));
                        Markers = Markers.OrderBy(n => n.Ticks).ToList();
                    }
                    else
                    {
                        MessageBox.Show("Выберите другую позицию метки");
                    }
                });
            }
        }
        public ICommand DeleteMarkerCommand
        {
            get
            {
                return new DelegateCommand<MouseEventArgs>((e) => {
                    var source = e.Source as ItemsControl;
                    var pos = e.GetPosition(source);
                    if (source != null && pos.X >= 130 && pos.Y < 152)
                    {
                        var marker = ResourceHelper.GetDataFromItemsControl(source, pos) as TimeSpan?;
                        if (marker != null )
                        {
                            Markers = Markers.Where(n => n != marker.Value).ToList();
                        }
                    }
                });
            }
        }
        public ActionCommand ApplySplitCommand
        {
            get
            {
                return new ActionCommand((param) => {
                    if (_projectInfo.SelectedResource != null && _projectInfo.SelectedResourceInUse != null)
                    {
                        if (Markers.Count == 0)
                        {
                            MessageBox.Show("Сначало поставте временные точки для разделения фрагментов");
                            return;
                        }
                        IsLoading = true;
                        Task.Factory.StartNew(() =>
                        {
                            if (_projectInfo.SelectedResource.Type == (int)ResourceType.IMAGE)
                            {
                                SplitResource((source, result, start, end) => {
                                    File.Copy(source, result);
                                });
                            }
                            else
                            {
                                SplitResource((source, result, start, end) => {
                                    FFMpegArguments.FromFileInput(source, verifyExists: true).OutputToFile(result, overwrite: true, delegate (FFMpegArgumentOptions options)
                                    {
                                        options.Seek(start).EndSeek(end);
                                    }).ProcessSynchronously();
                                });
                            }
                            IsLoading = false;
                            var window = param as Window;
                            if (window != null)
                            {
                                _projectInfo.SelectedResource = null;
                                window.Dispatcher.Invoke(() => { window.Close(); });
                            }
                        });
                    }
                });
            }
        }
        private void SplitResource(Action<string,string,TimeSpan,TimeSpan> subFileOperation)
        {
            var source = _projectInfo.SelectedResource;
            if (source == null) return;
            var dir = Path.GetDirectoryName(ResourcePath);
            var name = Path.GetFileNameWithoutExtension(ResourcePath);
            var extention = Path.GetExtension(ResourcePath);
            int markerNum = 2;
            LoadingValue = 0;
            List<Resource> newResources = new List<Resource>();
            var prevMarker = TimeSpan.Zero;
            Markers.Add(TimeSpan.FromTicks(Duration));
            foreach (var marker in Markers)
            {
                while (File.Exists(Path.Combine(dir, name + markerNum + extention))) markerNum++;
                Resource resource = new Resource();
                resource.Name = name + markerNum + extention;
                resource.StartTime = prevMarker.Ticks+source.StartTime;
                resource.Duration = (marker - prevMarker).Ticks;
                resource.PossitionX = source.PossitionX;
                resource.PossitionY = source.PossitionY;
                resource.Layer = source.Layer;
                resource.Type = source.Type;
                resource.ProjectId = source.ProjectId;
                newResources.Add(resource);
                subFileOperation(ResourcePath, Path.Combine(dir, resource.Name), prevMarker, marker);
                prevMarker = marker;
                markerNum++;
                LoadingValue = marker.Ticks;
            }
            Markers.Remove(TimeSpan.FromTicks(Duration));
            try
            {
                _dbContext.Resources.AddRange(newResources);
                _dbContext.SaveChanges();
                foreach(var resource in newResources) ResourceHelper.CloneResourcePipeline(_dbContext, source, resource);
                source.StartTime = null;
                _dbContext.SaveChanges();
                _projectInfo.NoticeResourceUpdated(_dbContext);
            }
            catch
            {
                MessageBox.Show("При разбивке произошла ошибка, выполняется откат операции");
                foreach(var resource in newResources)
                {
                    if(File.Exists(Path.Combine(dir, resource.Name))) File.Delete(Path.Combine(dir, resource.Name));
                }
            }
        }

    }
}
