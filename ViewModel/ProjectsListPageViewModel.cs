using DevExpress.Mvvm;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.View;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenCVVideoRedactor.ViewModel
{
    public class ProjectsListPageViewModel : BindableBase
    {
        private DatabaseContext _dbContext;
        private PageInfo _pageInfo;
        private CreateProjectModel _createProjectModel;
        private CurrentProjectInfo _currentProjectInfo;
        public List<Project> Projects { get; set; }
        public int SelectedProjectIndex { get; set; }
        public bool ProjectIsSelected { get { return SelectedProjectIndex>=0 && SelectedProjectIndex < Projects.Count; } }
        public Visibility LoaderVisibility { get; set; } = Visibility.Collapsed;
        public ProjectsListPageViewModel(DatabaseContext dbContext, PageInfo pageInfo, CurrentProjectInfo currentProjectInfo, CreateProjectModel createProjectModel)
        {
            _dbContext = dbContext;
            _pageInfo = pageInfo;
            _createProjectModel = createProjectModel;
            SelectedProjectIndex = -1;
            _currentProjectInfo = currentProjectInfo;
            Projects = _dbContext.Projects.ToList();
        }
        public ICommand DeleteSelectedProject
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if(System.Windows.Forms.MessageBox.Show("Вы уверены, что хотите удалить проект?\nДанную операцию нельзя отменить.", 
                        $"Удалить проект", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        var project = Projects[SelectedProjectIndex];
                        _dbContext.Remove(project);
                        _dbContext.SaveChanges();
                        _currentProjectInfo.ProjectInfo = null;
                        DeleteDirectory(project.DataFolder);
                        Projects = _dbContext.Projects.ToList();
                    }
                });
            }
        }
        public ICommand OpenSelectedProject
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (SelectedProjectIndex >= 0 && SelectedProjectIndex < Projects.Count)
                    {
                        LoaderVisibility = Visibility.Visible;
                        Task.Factory.StartNew(() =>
                        {
                            _currentProjectInfo.ProjectInfo = Projects[SelectedProjectIndex];
                            var mainPageViewModel = ServiceLocator.MainPageViewModel;
                            App.Current.Dispatcher.Invoke(() => {
                                _pageInfo.CurrentPage = new MainPage();
                                _pageInfo.CurrentPage.DataContext = mainPageViewModel;
                            });
                        });
                    }
                });
            }
        }
        public ICommand NavigateToCreatePage
        {
            get
            {
                return new DelegateCommand(() => {
                    _createProjectModel.DataFolder = "";
                    _createProjectModel.Title = "";
                    _createProjectModel.BackgroundColor = Colors.Black;
                    _createProjectModel.VideoWidth = 640;
                    _createProjectModel.VideoHeight = 480;
                    _createProjectModel.VideoFps = 30;
                    _pageInfo.CurrentPage = new CreateProjectPage();
                });
            }
        }
        private static void DeleteDirectory(string directory)
        {
            while (Directory.Exists(directory))
            {
                try
                {
                    Directory.Delete(directory, true);
                }
                catch
                {
                    System.Threading.Thread.Sleep(100);
                }
            }
        }
    }
}
