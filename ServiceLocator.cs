using DevExpress.Mvvm.POCO;
using Microsoft.Extensions.DependencyInjection;
using OpenCVVideoRedactor.Model;
using OpenCVVideoRedactor.Model.Database;
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
        private static void InitModels(ServiceCollection services)
        {
            services.AddSingleton<CurrentProjectInfo>();
            services.AddSingleton<CreateProjectModel>();
            services.AddSingleton<PageInfo>();
            services.AddSingleton<DatabaseContext>();
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
            InitModels(services); 

            _provider = services.BuildServiceProvider();

            var db = _provider.GetRequiredService<DatabaseContext>();
            db.Database.EnsureCreated();

           /* foreach (var item in services)
            {
                if(item.ServiceType != typeof(DatabaseContext))
                    _provider.GetRequiredService(item.ServiceType);
            }*/
        }
        public ProjectsListPageViewModel ProjectListsViewModel => _provider!.GetRequiredService<ProjectsListPageViewModel>();
        public CreateProjectViewModel CreateProjectViewModel => _provider!.GetRequiredService<CreateProjectViewModel>();
        public MainPageViewModel MainPageViewModel => _provider!.GetRequiredService<MainPageViewModel>();
        public TimelineControlViewModel TimelineControlViewModel => _provider!.GetRequiredService<TimelineControlViewModel>();
        public MainViewModel MainViewModel => _provider!.GetRequiredService<MainViewModel>();
        public PipelineViewModel PipelineViewModel => _provider!.GetRequiredService<PipelineViewModel>();
        public SplitResourceViewModel SplitResourceViewModel => _provider!.GetRequiredService<SplitResourceViewModel>();
    }
}
