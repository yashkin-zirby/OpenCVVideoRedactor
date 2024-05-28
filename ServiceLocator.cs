using DevExpress.Mvvm.POCO;
using Microsoft.Extensions.DependencyInjection;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
using OpenCVVideoRedactor.Pipeline;
using OpenCVVideoRedactor.ViewModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace OpenCVVideoRedactor
{
    public class ServiceLocator
    {
        private static ServiceProvider? _provider = null;
        public ServiceLocator()
        {
            if(_provider == null)
            {
                Init();
            }
        }
        public static void Init()
        {
            var services = new ServiceCollection();
            services.AddSingleton<MainViewModel>();
            services.AddTransient<MainPageViewModel>();
            services.AddTransient<ProjectsListPageViewModel>();
            services.AddTransient<CreateProjectViewModel>();
            services.AddTransient<TimelineControlViewModel>();
            services.AddTransient<PipelineViewModel>();
            services.AddTransient<SplitResourceViewModel>();
            services.AddTransient<ModifyOperationViewModel>();
            services.AddTransient<ClonePipelineViewModel>();
            services.AddTransient<PipelineController>();
            
            services.AddSingleton<CurrentProjectInfo>();
            services.AddSingleton<CreateProjectModel>();
            services.AddSingleton<VideoProcessingModel>();
            services.AddSingleton<PageInfo>();
            services.AddSingleton<DatabaseContext>();

            _provider = services.BuildServiceProvider();

            var db = _provider.GetRequiredService<DatabaseContext>();
            db.Database.EnsureCreated();
        }
        //public MainPageViewModel MainPageViewModel => _provider!.GetRequiredService<MainPageViewModel>();
        public static MainPageViewModel MainPageViewModel => _provider!.GetRequiredService<MainPageViewModel>();
        public ProjectsListPageViewModel ProjectListsViewModel => _provider!.GetRequiredService<ProjectsListPageViewModel>();
        public CreateProjectViewModel CreateProjectViewModel => _provider!.GetRequiredService<CreateProjectViewModel>();
        public TimelineControlViewModel TimelineControlViewModel => _provider!.GetRequiredService<TimelineControlViewModel>();
        public MainViewModel MainViewModel => _provider!.GetRequiredService<MainViewModel>();
        public PipelineViewModel PipelineViewModel => _provider!.GetRequiredService<PipelineViewModel>();
        public SplitResourceViewModel SplitResourceViewModel => _provider!.GetRequiredService<SplitResourceViewModel>();
        public ModifyOperationViewModel ModifyOperationViewModel => _provider!.GetRequiredService<ModifyOperationViewModel>();
        public ClonePipelineViewModel ClonePipelineViewModel => _provider!.GetRequiredService<ClonePipelineViewModel>();
    }
}
