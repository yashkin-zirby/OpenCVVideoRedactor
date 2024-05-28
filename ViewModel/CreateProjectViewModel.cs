using DevExpress.Mvvm;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.View;
using System;
using System.IO;
using System.Windows.Input;
using System.Windows.Media;
using Ookii.Dialogs.Wpf;
using System.Windows;

namespace OpenCVVideoRedactor.ViewModel
{
    public class CreateProjectViewModel : BindableBase
    {
        private DatabaseContext _dbContext;
        private PageInfo _pageInfo;
        private CreateProjectModel _model;
        private string _selectedFolder = "";
        private string _color = "";
        public string ErrorMessage { get;set; }
        public string Title { 
            get { return _model.Title; } 
            set { 
                _model.Title = value;
                ErrorMessage = "";
                if (_selectedFolder.Length > 0) _model.DataFolder = Path.Combine(_selectedFolder, value);
            } 
        }
        public string DataFolder { get { return _model.DataFolder; } set { _model.DataFolder = value; } }
        public long VideoFps { get { return _model.VideoFps; } set { _model.VideoFps = Math.Max(1, Math.Min(60, value)); } }
        public long VideoWidth { get { return _model.VideoWidth; } set { _model.VideoWidth = Math.Max(1,Math.Min(4096, value)); } }
        public long VideoHeight { get { return _model.VideoHeight; } set { _model.VideoHeight = Math.Max(1, Math.Min(2160, value)); } }
        public bool NextButtonEnabled { get { return Title.Length > 0 && DataFolder.Length > 0; } }
        public string Color { 
            get { return _color; } 
            set { 
                _color = value;
                var color = System.Windows.Media.ColorConverter.ConvertFromString(_color);
                RaisePropertyChanged(nameof(Color));
                if (color != null)
                {
                    _model.BackgroundColor = (System.Windows.Media.Color)color;
                }
            } 
        }
        public SolidColorBrush BackgroundColor { get { return new SolidColorBrush(_model.BackgroundColor); } }
        public CreateProjectViewModel(DatabaseContext dbContext, PageInfo pageInfo, CreateProjectModel model)
        {
            _color = model.BackgroundColor.ToString();
            ErrorMessage = "";
            _dbContext = dbContext;
            _pageInfo = pageInfo;
            _model = model;
            _model.PropertyChanged += ModelChanged;
        }
        ~CreateProjectViewModel() {
            _model.PropertyChanged -= ModelChanged;
        }

        private void ModelChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            this.RaisePropertyChanged(e.PropertyName);
        }
        public ICommand SelectFolder
        {
            get
            {
                return new DelegateCommand(() => {
                    VistaFolderBrowserDialog dlg = new VistaFolderBrowserDialog();
                    dlg.Multiselect = false;
                    dlg.UseDescriptionForTitle = true;
                    dlg.Description = "Выбирите имя файла";
                    var value = dlg.ShowDialog();
                    if (value.HasValue && value.Value)
                    {
                        _selectedFolder = dlg.SelectedPath;
                        DataFolder = Path.Combine(dlg.SelectedPath, Title);
                        ErrorMessage = "";
                    }
                });
            }
        }
        public ICommand GoToNextCreatePage
        {
            get
            {
                return new DelegateCommand(() => {
                    if (Directory.Exists(DataFolder))
                    {
                        MessageBox.Show($"Директория {DataFolder} занята. Выберете другое местоположение или имя проекта");
                        return;
                    }
                    if(Title.Length > 0 && DataFolder.Length > 0)
                        _pageInfo.CurrentPage = new FinishProjectCreationPage();
                });
            }
        }
        public ICommand GoToPrevPage
        {
            get
            {
                return new DelegateCommand(() => {
                        _pageInfo.CurrentPage = new CreateProjectPage();
                });
            }
        }
        public ICommand CreateProject
        {
            get
            {
                return new DelegateCommand(() => {
                    Project project = new Project();
                    project.VideoWidth = VideoWidth;
                    project.VideoHeight = VideoHeight;
                    project.VideoFps = VideoFps;
                    project.Title = Title;
                    project.DataFolder = DataFolder;
                    project.Background = BackgroundColor.Color.R * 256 * 256 + BackgroundColor.Color.G * 256 + BackgroundColor.Color.B;
                    _model.DataFolder = "";
                    _model.Title = "";
                    _model.BackgroundColor = Colors.Black;
                    _model.VideoWidth = 640;
                    _model.VideoHeight = 480;
                    _model.VideoFps = 1; 
                    _dbContext.Add(project);
                    if (_dbContext.SaveChanges()>0)
                    {
                        Directory.CreateDirectory(project.DataFolder);
                        _pageInfo.CurrentPage = new ProjectsListPage();
                    }
                    else
                    {
                        MessageBox.Show("Не удалось создать проект");
                    }
                });
            }
        }
        private void SelectColor(System.Windows.Media.Color color)
        {
            _model.BackgroundColor = color;
            _color = _model.BackgroundColor.ToString();
            RaisePropertyChanged(nameof(Color));
        }
        public ICommand SelectColorBlack { get{ return new DelegateCommand(() =>SelectColor(Colors.Black));}}
        public ICommand SelectColorWhite { get { return new DelegateCommand(() => SelectColor(Colors.White)); } }
        public ICommand SelectColorGreen { get { return new DelegateCommand(() => SelectColor(Colors.Green)); } }
        public ICommand SelectColorBlue { get { return new DelegateCommand(() => SelectColor(Colors.Blue)); } }
        public ICommand SelectColorGray { get { return new DelegateCommand(() => SelectColor(Colors.Gray)); } }
        public ICommand SelectColorRed { get { return new DelegateCommand(() => SelectColor(Colors.Red)); } }
        public ICommand SelectColorYellow { get { return new DelegateCommand(() => SelectColor(Colors.Yellow)); } }
        public ICommand SelectColorGreenYellow { get { return new DelegateCommand(() => SelectColor(Colors.GreenYellow)); } }
        public ICommand SelectColorPurple { get { return new DelegateCommand(() => SelectColor(Colors.Purple)); } }
        public ICommand SelectColorBrown { get { return new DelegateCommand(() => SelectColor(Colors.Brown)); } }
        public ICommand SelectColorSkyBlue { get { return new DelegateCommand(() => SelectColor(Colors.SkyBlue)); } }
        public ICommand SelectColorChocolate { get { return new DelegateCommand(() => SelectColor(Colors.Chocolate)); } }
    }
}
