using EXIMWinService.FileExtracter;
using EXIMWinService.Ftp;
using EXIMWinService.Quartz;
using EXIMWinService.WebApi;
using Ninject.Modules;
using Ninject;
using EXIMWinService.Processers;
using EXIMWinService.Configuration;
using EXIMWinService.Services;
using Renci.SshNet;

namespace EXIMWinService.Ninject
{
    class EXIMNijectModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ICronDispatcher>().To<Dispatcher>();
            Bind<IEXIMJobFactory>().To<EXIMJobFactory>();
            Bind<IConfigurationProvider>().To<ConfigurationProvider>();
            Bind<IMeasureService>().To<MeasureService>();
            Bind<IPlantService>().To<PlantService>();
            Bind<IMeasureProcesser>().To<MeasureProcesser>();
            Bind<IMeasureFileExtracter>().To<MeasureFileExtracter>();
            Bind<IPlantPowerFileExtracter>().To<PlantPowerFileExtracter>();
            Bind<IPlantPowerService>().To<PlantPowerService>();
            Bind<IFtpInfo>().To<FtpInfo>();
            Bind<IFtpClient>().To<Sftp>();
            Bind<IWebApiInfo>().To<WebApiInfo>();
            Bind<IWebApiClient>().To<WebApiClient>();
        }

        public static T GetDependency<T>()
        {
            return new StandardKernel(new EXIMNijectModule()).Get<T>();
        }
    }
}
