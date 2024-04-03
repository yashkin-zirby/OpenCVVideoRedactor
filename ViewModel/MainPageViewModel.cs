using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using OpenCVVideoRedactor.Helpers;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.PopUpWindows;
using OpenCVVideoRedactor.View;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenCVVideoRedactor.ViewModel
{
    public class MainPageViewModel : BindableBase, IDropHandler
    {
        private CurrentProjectInfo _currentProject;
        private DatabaseContext _dbContext;
        private string[] _types = new string[] { "images", "videos", "audios" };
        public double ResourcesWidth { get; set; } = 200;
        public double PropertiesWidth { get; set; } = 200;
        public double TimelineHeight { get; set; } = 200;
        public bool IsResourcesWidthChanging { get; set; } = false;
        public bool IsTimelineHeightChanging { get; set; } = false;
        public bool IsPropertiesWidthChanging { get; set; } = false;
        public IEnumerable<Resource> Images { 
            get {
                return _currentProject.Images;
            } 
        }
        public IEnumerable<Resource> Videos
        {
            get
            {
                return _currentProject.Videos;
            }
        }
        public IEnumerable<Resource> Audios
        {
            get
            {
                return _currentProject.Audios;
            }
        }
        public bool ItemIsSelected { get { return _currentProject.SelectedResource != null; } }
        public Resource? SelectedResource { 
            get { return _currentProject.SelectedResource; } 
            set {
                _currentProject.SelectedResource = value;
            } 
        }
        public string SelectedResourceName
        {
            get
            {
                if (SelectedResource == null) return "";
                return Path.GetFileNameWithoutExtension(SelectedResource.Name);
            }
            set
            {
                if (SelectedResource == null) return;
                string newName = value + Path.GetExtension(SelectedResource.Name);
                string? dir = GetDirByType(SelectedResource.Type);
                if (dir == null) return;
                string filePath = Path.Combine(dir, newName);
                if(value.Length > 0)
                {
                    if (File.Exists(filePath))
                    {
                        MessageBox.Show("Ресурс с таким именем уже существует");
                        return;
                    }
                    else
                    {
                        File.Move(Path.Combine(dir, SelectedResource.Name),filePath);
                        SelectedResource.Name = newName;
                        _dbContext.SaveChanges();
                        _currentProject.NoticeResourceUpdated(null);
                    }
                }
            }
        }
        private string? GetDirByType(long type)
        {
            switch((ResourceType)type)
            {
                case ResourceType.IMAGE: return _currentProject.ImagesDir;
                case ResourceType.VIDEO: return _currentProject.VideosDir;
                case ResourceType.AUDIO: return _currentProject.AudiosDir;
            }
            return null;
        }
        public TimeSpan? SelectedResourceDuration
        { 
            get { return SelectedResource != null? new TimeSpan(SelectedResource.Duration): null; }
            set
            {
                if (value.HasValue && SelectedResource != null)
                {
                    SelectedResource.Duration = value.Value.Ticks;
                    _dbContext.SaveChanges();
                    _currentProject.NoticeResourceUpdated(null);
                }
            }
        }
        public TimeSpan? SelectedResourceStartTime
        {
            get { return SelectedResource != null && SelectedResource.StartTime != null ? new TimeSpan(SelectedResource.StartTime.Value) : null; }
            set
            {
                if (SelectedResource != null)
                {
                    SelectedResource.StartTime = value.HasValue ? value.Value.Ticks : null;
                    _dbContext.SaveChanges();
                    _currentProject.NoticeResourceUpdated(null);
                }
            }
        }
        public Visibility LoaderVisibility { get; set; } = Visibility.Collapsed;
        public static MainPageViewModel? Instance { get; private set; } = null;

        public MainPageViewModel(DatabaseContext dbContext, PageInfo pageInfo, CurrentProjectInfo currentProject)
        {
            _dbContext = dbContext;
            _currentProject = currentProject;
            if(_currentProject.ProjectInfo == null)
            {
                pageInfo.CurrentPage = new ProjectsListPage();
                return;
            }
            ResourceHelper.DropNotExistingResources(_currentProject.ProjectInfo.DataFolder,dbContext);
            _currentProject.PropertyChanged += ProjectPropertiesChanged;
            _currentProject.Resources = _dbContext.Resources.Where(n => n.ProjectId == _currentProject.ProjectInfo.Id).ToList();
            Instance = this;
        }
        ~MainPageViewModel()
        {
            Instance = null;
            _currentProject.PropertyChanged -= ProjectPropertiesChanged;
        }
        private void ProjectPropertiesChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            RaisePropertyChanged(eventArgs.PropertyName);
            if(eventArgs.PropertyName == nameof(SelectedResource))
            {
                RaisePropertiesChanged(nameof(SelectedResourceStartTime), nameof(SelectedResourceDuration), nameof(ItemIsSelected), nameof(SelectedResourceName));
            }
        }
        public static ICommand AddResourceCommand
        { 
            get {
                return Instance == null? new DelegateCommand(() => { }): Instance.AddResource;
            } 
        }
        public ICommand AddResource
        {
            get
            {
                return new DelegateCommand(() => {
                    if(_currentProject.ProjectInfo != null) {
                        VistaOpenFileDialog dlg = new VistaOpenFileDialog();
                        dlg.Multiselect = true;
                        dlg.CheckPathExists = true;
                        dlg.CheckFileExists = true;
                        dlg.Filter = "all files|*.*|image files|*.png;*.jpg;*.bmp;*.jpeg|video files|*.mp4;*.avi|audio files|*.aac;*.wav;*.mp3";
                        var value = dlg.ShowDialog();
                        if (value.HasValue && value.Value)
                        {
                            LoaderVisibility = Visibility.Visible;
                            Task.Factory.StartNew(() => {
                                foreach (var file in dlg.FileNames)
                                {
                                    AddFile(file);
                                }
                                LoaderVisibility = Visibility.Collapsed;
                                _currentProject.Resources = _dbContext.Resources.Where(n => n.ProjectId == _currentProject.ProjectInfo.Id).ToList();
                            });
                        }
                    }
                });
            }
        }
        public ICommand RemoveResource
        {
            get
            {
                return new DelegateCommand(() => {
                    if (SelectedResource != null)
                    {
                        if(MessageBox.Show($"Удалить '{SelectedResource.Name}'?\nЭто действие нельзя отменить",
                            "Удаление ресурса",MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            _dbContext.Resources.Remove(SelectedResource);
                            if (_dbContext.SaveChanges() > 0)
                            {
                                var typeResources = _types[SelectedResource.Type];
                                var dir = Path.Combine(_currentProject.ProjectInfo!.DataFolder, typeResources);
                                File.Delete(Path.Combine(dir, SelectedResource.Name));
                                _currentProject.Resources = _dbContext.Resources.Where(n => n.ProjectId == _currentProject.ProjectInfo.Id).ToList();
                            }
                        }
                    }
                });
            }
        }
        public ICommand SaveProperties
        {
            get { return new DelegateCommand(() => { if(_dbContext.SaveChanges()>0)_currentProject.NoticeResourceUpdated(null); }); }
        }
        public ICommand StartResizeResourceWindow
        {
            get { return new DelegateCommand(() => { IsResourcesWidthChanging = true; }); }
        }
        public ICommand EndResizeWindow
        {
            get { return new DelegateCommand(() => { IsResourcesWidthChanging = false; IsPropertiesWidthChanging = false; IsTimelineHeightChanging = false; }); }
        }
        public ICommand StartResizePropertiesWindow
        {
            get { return new DelegateCommand(() => { IsPropertiesWidthChanging = true; }); }
        }
        public ICommand StartResizeTimelineWindow
        {
            get { return new DelegateCommand(() => { IsTimelineHeightChanging = true; }); }
        }
        public ICommand DoDragCommand
        {
            get {
                return new DelegateCommand<MouseButtonEventArgs>((e) => {
                    var parrent = e.Source as ItemsControl;
                    if (parrent != null && _currentProject.ProjectInfo != null)
                    {
                        var resource = ResourceHelper.GetDataFromItemsControl(parrent, e.GetPosition(parrent)) as Resource;
                        SelectedResource = resource;
                        if (e.ClickCount > 1)
                        {
                            var color = _currentProject.ProjectInfo!.Background;
                            var r = (byte)((color >> 16) & 255);
                            var g = (byte)((color >> 8) & 255);
                            var b = (byte)(color & 255);
                            MediaViewer.ShowDialog(
                                Path.Combine(_currentProject.ProjectInfo.DataFolder, _types[SelectedResource.Type], SelectedResource.Name),
                                SelectedResource.Type, new SolidColorBrush(Color.FromRgb(r, g, b)));
                        }
                        else if (SelectedResource != null)
                        {
                            DragDrop.DoDragDrop(parrent, SelectedResource, DragDropEffects.Move);
                        }
                    }
                }); 
            }
        }
        public ICommand ResizeWindow
        {
            get
            {
                return new DelegateCommand<MouseEventArgs>((e) => {
                    double Width = App.Current.MainWindow.ActualWidth;
                    double Height = App.Current.MainWindow.ActualHeight;
                    if (IsResourcesWidthChanging)
                    {
                        var pos = e.GetPosition(App.Current.MainWindow);
                        ResourcesWidth = Math.Min(pos.X + 2.5, Width-PropertiesWidth-200);
                    }
                    if (IsPropertiesWidthChanging)
                    {
                        var pos = e.GetPosition(App.Current.MainWindow);
                        PropertiesWidth = Width - Math.Max(pos.X + 2.5,ResourcesWidth+200);
                    }
                    if (IsTimelineHeightChanging)
                    {
                        var pos = e.GetPosition(App.Current.MainWindow);
                        TimelineHeight = Height - pos.Y - 5;
                    }
                });
            }
        }
        public void OnDataDropped(IDataObject data)
        {
            if (data.GetDataPresent(DataFormats.FileDrop))
            {
                var files = data.GetData(DataFormats.FileDrop) as string[];
                if(files != null && _currentProject.ProjectInfo != null)
                {
                    LoaderVisibility = Visibility.Visible;
                    Task.Factory.StartNew(() => {
                        foreach (var file in files)
                        {
                            AddFile(file);
                        }
                        LoaderVisibility = Visibility.Collapsed;
                        _currentProject.Resources = _dbContext.Resources.Where(n => n.ProjectId == _currentProject.ProjectInfo.Id).ToList();
                    });
                }
            }
        }
        private void AddFile(string file)
        {
            var extension = Path.GetExtension(file);
            if (extension.ToLower() == ".mp4" || extension.ToLower() == ".avi")
            {
                Resource? ref_audio = null;
                Resource video;
                try
                {
                    video = ResourceHelper.CreateResourceFromVideo(file, _currentProject.ProjectInfo, out ref_audio);
                }
                catch (FileAlreadyExists ex)
                {
                    var newName = "";
                    do
                    {
                        newName = InputBox.ShowDialog("Изменить имя файла?", ex.Message, Path.GetFileNameWithoutExtension(file), (s) => s.Length > 0);
                        if (newName == "") return;
                        newName = newName + extension;
                    } while (File.Exists(Path.Combine(_currentProject.VideosDir,newName)));
                    video = ResourceHelper.CreateResourceFromVideo(file, _currentProject.ProjectInfo, out ref_audio, newName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                if (ref_audio != null)
                {
                    _dbContext.Resources.Add(ref_audio);
                }
                _dbContext.Resources.Add(video);
                _dbContext.SaveChanges();
            }
            else if (extension.ToLower() == ".aac" || extension.ToLower() == ".wav")
            {
                LoaderVisibility = Visibility.Visible;
                var resource = new Resource();
                try
                {
                    resource = ResourceHelper.CreateResourceFromAudio(file, _currentProject.ProjectInfo);
                }
                catch (FileAlreadyExists ex)
                {
                    var newName = "";
                    do
                    {
                        newName = InputBox.ShowDialog("Изменить имя файла?", ex.Message, Path.GetFileNameWithoutExtension(file), (s) => s.Length > 0);
                        if (newName == "") return;
                        newName = newName + extension;
                    } while (File.Exists(Path.Combine(_currentProject.AudiosDir, newName)));
                    resource = ResourceHelper.CreateResourceFromAudio(file, _currentProject.ProjectInfo, newName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                _dbContext.Resources.Add(resource);
                _dbContext.SaveChanges();
            }
            else if(extension.ToLower() == ".png" || extension.ToLower() == ".jpg" ||
                extension.ToLower() == ".bmp" || extension.ToLower() == ".jpeg")
            {
                LoaderVisibility = Visibility.Visible;
                var resource = new Resource();
                try
                {
                    resource = ResourceHelper.CreateResourceFromImage(file, _currentProject.ProjectInfo);
                }
                catch (FileAlreadyExists ex)
                {
                    var newName = "";
                    do
                    {
                        newName = InputBox.ShowDialog("Изменить имя файла?", ex.Message, Path.GetFileNameWithoutExtension(file), (s) => s.Length > 0);
                        if (newName == "") return;
                        newName = newName + extension;
                    } while (File.Exists(Path.Combine(_currentProject.ImagesDir, newName)));
                    resource = ResourceHelper.CreateResourceFromImage(file, _currentProject.ProjectInfo, newName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
                _dbContext.Resources.Add(resource);
                _dbContext.SaveChanges();
            }
            else
            {
                MessageBox.Show($"Формат {extension} не поддерживается.");
            }
        }
    }
}
