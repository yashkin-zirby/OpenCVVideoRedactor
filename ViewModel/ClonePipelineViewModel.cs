using DevExpress.Mvvm;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Microsoft.EntityFrameworkCore;
using OpenCVVideoRedactor.Helpers;

namespace OpenCVVideoRedactor.ViewModel
{
    public class ClonePipelineViewModel : BindableBase
    {
        private DatabaseContext _dbContext;
        private CurrentProjectInfo _projectInfo;
        private Action? _close;
        private long id = -1;
        public IEnumerable<Resource> PipelineSources { 
            get { 
                return _dbContext.Resources.Where(n=>n.Operations.Count > 0 && n.Id != id)
                    .Include(n=>n.Operations); 
            } 
        }
        public Resource? SelectedResource { get; set; } = null;
        public Resource? Resource { get { return _projectInfo.SelectedResource; } }
        public ClonePipelineViewModel(DatabaseContext dbContext, CurrentProjectInfo projectInfo)
        {
            _dbContext = dbContext;
            _projectInfo = projectInfo;
            if(_projectInfo.SelectedResource != null)id = _projectInfo.SelectedResource.Id;
        }
        public ICommand AttachCloseMethod
        {
            get
            {
                return new DelegateCommand<RoutedEventArgs>(e => {
                    var window = e.Source as Window;
                    if (window != null)
                    {
                        _close = () => window.Close();
                        if (_projectInfo.SelectedResource == null) _close();
                    }
                });
            }
        }
        public ICommand ClonePipeline
        {
            get
            {
                return new DelegateCommand(() => {
                    if(SelectedResource == null)
                    {
                        MessageBox.Show("Выбирите конвеер для копирования");
                        return;
                    }
                    if (Resource != null) { 
                        if (Resource.Operations.Count > 0){
                            var choose = MessageBox.Show("Вы уверены что хотите копировать выбранный конвеер?\nКонвеер текущего ресурса будет утрачен", "Ресурс уже имеет конвеер", MessageBoxButton.OKCancel);
                            if (choose != MessageBoxResult.OK) return;
                        }
                        ResourceHelper.CloneResourcePipeline(_dbContext, SelectedResource, Resource);
                        _projectInfo.NoticeResourceUpdated(_dbContext);
                    }
                    if (_close != null) _close();
                });
            }
        }
    }
}
