using DevExpress.Mvvm;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.View;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenCVVideoRedactor.ViewModel
{
    public class MainViewModel : BindableBase
    {
        public Page CurrentPage { get; set; }
        private PageInfo _pageInfo;
        private CreateProjectModel _createProjectModel;
        private CurrentProjectInfo _projectInfo;
        public Visibility IsVisibleSaveButton { get { return _projectInfo.ProjectInfo != null ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsVisibleImportButton { get { return _pageInfo.CurrentPage is MainPage ? Visibility.Visible : Visibility.Collapsed; } }
        public Visibility IsVisibleExportButton { get { return _pageInfo.CurrentPage is MainPage ? Visibility.Visible : Visibility.Collapsed; } }
        public bool IsResourcesColumnVisible { get { return _projectInfo.IsResourcesColumnVisible; } set { _projectInfo.IsResourcesColumnVisible = value; } }
        public bool IsPropertiesColumnVisible { get { return _projectInfo.IsPropertiesColumnVisible; } set { _projectInfo.IsPropertiesColumnVisible = value; } }
        public MainViewModel(PageInfo pageInfo, CreateProjectModel createProjectModel, CurrentProjectInfo projectInfo) {
            CurrentPage = new ProjectsListPage();
            _pageInfo = pageInfo;
            _pageInfo.CurrentPage = CurrentPage;
            _createProjectModel = createProjectModel;
            _projectInfo = projectInfo;
            _projectInfo.PropertyChanged += CurrentProjectChanged;
            _pageInfo.PropertyChanged += PageInfoChanged;
        }
        ~MainViewModel()
        {
            _projectInfo.PropertyChanged -= CurrentProjectChanged;
            _pageInfo.PropertyChanged -= PageInfoChanged;
        }
        private void CurrentProjectChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(_projectInfo.ProjectInfo))RaisePropertyChanged(nameof(IsVisibleSaveButton));
        }
        private void PageInfoChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            CurrentPage = _pageInfo.CurrentPage;
            RaisePropertiesChanged(nameof(IsVisibleExportButton), nameof(IsVisibleImportButton));
        }
        public ICommand ExportVideo
        {
            get { return new DelegateCommand(() => { _projectInfo.CompileVideo(); }); }
        }
        public ICommand ImportResource
        {
            get { return MainPageViewModel.AddResourceCommand; }
        }
        public ICommand NavigateToProjectsListCommand
        {
            get { return new DelegateCommand(() => { _pageInfo.CurrentPage = new ProjectsListPage(); }); }
        }
        public ICommand AboutWindowShow
        {
            get { return new DelegateCommand(() => { _pageInfo.CurrentPage = new AboutPage(); }); }
        }
        public ICommand NavigateToCreatePage
        {
            get { return new DelegateCommand(() => {
                _createProjectModel.DataFolder = "";
                _createProjectModel.Title = "";
                _createProjectModel.BackgroundColor = Colors.Black;
                _createProjectModel.VideoWidth = 640;
                _createProjectModel.VideoHeight = 480;
                _createProjectModel.VideoFps = 1;
                _pageInfo.CurrentPage = new CreateProjectPage(); 
            }); }
        }
    }
}
