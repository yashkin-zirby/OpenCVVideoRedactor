using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore;
using Ookii.Dialogs.Wpf;
using OpenCVVideoRedactor.Helpers;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.Pipeline;
using OpenCVVideoRedactor.Pipepline;
using OpenCVVideoRedactor.PopUpWindows;
using OpenCVVideoRedactor.View;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Printing;
using System.Text;
using System.Text.RegularExpressions;
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
        public bool IsPropertiesColumnVisible { get { return _currentProject.IsPropertiesColumnVisible; } }
        public bool IsResourcesColumnVisible { get { return _currentProject.IsResourcesColumnVisible; } }
        public bool IsResourcesWidthChanging { get; set; } = false;
        public bool IsTimelineHeightChanging { get; set; } = false;
        public bool IsPropertiesWidthChanging { get; set; } = false;
        public string NewVariableName { get; set; } = "";
        public double NewVariableValue { get; set; } = 0;
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
        public bool HasVariable { get { return SelectedVariable != null; } }
        public IEnumerable<IFrameOperation?> AvailableOperations { get { return FrameProcessingPipeline.GetOperations(); } }
        public IFrameOperation? Operation { get; set; } = null;
        public Variable? SelectedVariable { get; set; } = null;
        public List<Operation> ResourceOperations { get; set; } = new List<Operation>();
        public List<Variable> ResourceVariables { 
            get {
                var variables = SelectedResource != null ? 
                    _dbContext.Variables.Where(n => n.Resource == SelectedResource.Id).ToList()
                    : new List<Variable>();
                if(variables.Count>0) SelectedVariable = variables[0];
                return variables;
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
        public bool IsProcessing { get { return _processingModel.IsProcessing; } }
        public long CurrentProcessingValue { get { return _processingModel.CurrentProcessingValue; } }
        public long MaxProcessingValue { get { return _processingModel.MaxProcessingValue; } }
        public Visibility LoaderVisibility { get; set; } = Visibility.Collapsed;
        public static MainPageViewModel? Instance { get; private set; } = null;
        private VideoProcessingModel _processingModel;
        public MainPageViewModel(DatabaseContext dbContext, PageInfo pageInfo, CurrentProjectInfo currentProject, VideoProcessingModel processingModel)
        {
            _dbContext = dbContext;
            _currentProject = currentProject;
            _processingModel = processingModel;
            if (_currentProject.ProjectInfo == null)
            {
                pageInfo.CurrentPage = new ProjectsListPage();
                return;
            }
            ResourceHelper.DropNotExistingResources(_currentProject.ProjectInfo,dbContext);
            _currentProject.PropertyChanged += ProjectPropertiesChanged;
            _processingModel.PropertyChanged += ProjectPropertiesChanged;
            UpdateResourceList();
            Instance = this;
        }
        private void UpdateResourceList()
        {
            _currentProject.Resources = _dbContext.Resources.Where(n => n.ProjectId == _currentProject.ProjectInfo.Id)
                .Include(n=>n.Variables).Include(n => n.Operations).ThenInclude(n=>n.Parameters).ToList();
        }
        ~MainPageViewModel()
        {
            Instance = null;
            _currentProject.PropertyChanged -= ProjectPropertiesChanged;
            _processingModel.PropertyChanged -= ProjectPropertiesChanged;
        }
        private void ProjectPropertiesChanged(object? sender, PropertyChangedEventArgs eventArgs)
        {
            RaisePropertyChanged(eventArgs.PropertyName);
            if(eventArgs.PropertyName == nameof(SelectedResource))
            {
                RaisePropertiesChanged(nameof(SelectedResourceStartTime), nameof(SelectedResourceDuration), nameof(ItemIsSelected), nameof(SelectedResourceName));
                RaisePropertyChanged(nameof(ResourceVariables));
                RaisePropertyChanged(nameof(HasVariable));
                ResourceOperations = SelectedResource != null ?
                    _dbContext.Operations.Where(n => n.Source == SelectedResource.Id).OrderBy(n=>n.Index).ToList()
                    : new List<Operation>();
            }
        }
        public static ICommand AddResourceCommand
        { 
            get {
                return new DelegateCommand(() => { if(Instance != null)Instance.AddResource.Execute(null); });
            } 
        }
        public ICommand CreateNewVariable
        {
            get
            {
                Regex validateVariableName = new Regex("^[A-Za-z]+[_0-9]*$",RegexOptions.Singleline);
                return new DelegateCommand(() => {
                if (SelectedResource != null)
                {
                    if (NewVariableName.Length == 0)
                    {
                        return;
                    }
                    if (!validateVariableName.IsMatch(NewVariableName))
                    {
                        MessageBox.Show($"Название переменной должно начинаться с буквы английского алфавита" +
                            $" и не должно сожержать никаких других символов кроме цифр и символа '_'");
                        return;
                    }
                    if (NewVariableName == "x" || NewVariableName == "y" || NewVariableName == "time" || NewVariableName == "duration" ||
                           NewVariableName == "frame" || NewVariableName == "width" || NewVariableName == "height")
                        {
                            MessageBox.Show($"Переменная '{NewVariableName}' зарезервирована");
                            return;
                        }
                        if(!SelectedResource.Variables.Any(n=>n.Name == NewVariableName))
                        {
                            var variable = new Variable();
                            variable.Name = NewVariableName;
                            variable.Value = NewVariableValue;
                            variable.Resource = SelectedResource.Id;
                            _dbContext.Variables.Add(variable);
                            _dbContext.SaveChanges();
                            NewVariableName = "";
                            NewVariableValue = 0;
                            var resource = SelectedResource;
                            SelectedResource = null;
                            SelectedResource = resource;
                        }
                        else
                        {
                            MessageBox.Show($"Переменная '{NewVariableName}' уже существует");
                        }
                    }
                });
            }
        }
        public ICommand OperationSelected
        {
            get
            {
                return new DelegateCommand<SelectionChangedEventArgs>((e) => {
                    var listBox = e.Source as ListBox;
                    if (listBox != null)
                    {
                        _currentProject.SelectedOperation = listBox.SelectedValue as Operation;
                        listBox.UnselectAll();
                    }
                });
            }
        }
        public ICommand AddOperation
        {
            get
            {
                return new DelegateCommand(() => {
                    if (Operation != null && SelectedResource != null)
                    {
                        if(Operation != null)
                        {
                            var operation = Operation.GetOperation();
                            _currentProject.SelectedOperation = operation;
                            _currentProject.NoticeResourceUpdated(null);
                            return;
                        }
                        MessageBox.Show("Выбранная операция не поддерживатеся");
                    }
                });
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
                                UpdateResourceList();
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
                        var name = SelectedResource.Name;
                        if (MessageBox.Show($"Удалить '{name}'?",
                            "Удаление ресурса",MessageBoxButton.OKCancel) == MessageBoxResult.OK)
                        {
                            _dbContext.Resources.Remove(SelectedResource);
                            if (_dbContext.SaveChanges() > 0)
                            {
                                var typeResources = _types[SelectedResource.Type];
                                var dir = Path.Combine(_currentProject.ProjectInfo!.DataFolder, typeResources);
                                UpdateResourceList();
                                DeleteFile(Path.Combine(dir, name));
                            }
                        }
                    }
                });
            }
        }
        public static void DeleteFile(String fileToDelete)
        {
            while(File.Exists(fileToDelete))
            {
                try
                {
                    File.Delete(fileToDelete);
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                }
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
        public ICommand DoDragOperationCommand
        {
            get
            {
                return new DelegateCommand<MouseButtonEventArgs>((e) => {
                    var parrent = e.Source as ItemsControl;
                    if (parrent != null && _currentProject.ProjectInfo != null)
                    {
                        var point = e.GetPosition(parrent);
                        var operation = ResourceHelper.GetDataFromItemsControl(parrent, e.GetPosition(parrent)) as Operation;
                        if (operation != null)
                        {
                            if (parrent.ActualWidth - point.X >= 7 && parrent.ActualWidth - point.X <= 27)
                            {
                                var op = _dbContext.Operations.FirstOrDefault(n => n.Name == operation.Name && n.Source == operation.Source);
                                if(op != null)
                                {
                                    _dbContext.Operations.Remove(op);
                                    _dbContext.SaveChanges();
                                    var list = _dbContext.Operations.Where(n => n.Source == op.Source && n.Index > op.Index).ToList();
                                    list.ForEach(n => { n.Index = n.Index - 1; });
                                    _dbContext.Operations.UpdateRange(list);
                                    _dbContext.SaveChanges();
                                    _currentProject.NoticeResourceUpdated(_dbContext);
                                    return;
                                }
                            }
                            DragDrop.DoDragDrop(parrent, operation, DragDropEffects.Move);
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
        public void OnDataDropped(IDataObject data, object? dropSourceObject = null)
        {
            var operation = data.GetData(typeof(Operation)) as Operation;
            var source = dropSourceObject as Operation;
            if (operation != null && source != null)
            {
                var first = _dbContext.Operations.FirstOrDefault(n => n.Id == operation.Id);
                var second = _dbContext.Operations.FirstOrDefault(n => n.Id == source.Id);
                if(first != null && second != null)
                {
                    var index = first.Index;
                    first.Index = second.Index;
                    second.Index = index;
                    _dbContext.SaveChanges();
                    _currentProject.NoticeResourceUpdated(null);
                }
                return;
            }
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
                        UpdateResourceList();
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
