using DevExpress.Mvvm;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.View;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
                    if(MessageBox.Show("Вы уверены, что хотите удалить проект?\nДанную операцию нельзя отменить.", 
                        $"Удалить проект", MessageBoxButtons.OKCancel) == DialogResult.OK)
                    {
                        var project = Projects[SelectedProjectIndex];
                        _dbContext.Remove(project);
                        _dbContext.SaveChanges();
                        Directory.Delete(project.DataFolder);
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
                    _currentProjectInfo.ProjectInfo = Projects[SelectedProjectIndex];
                    _pageInfo.CurrentPage = new MainPage();
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
                    _createProjectModel.VideoFps = 1;
                    _pageInfo.CurrentPage = new CreateProjectPage();
                });
            }
        }
    }
}
