using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EXIMWinService.Configuration;
using EXIMWinService.Ninject;

namespace EXIMWinService.Quartz
{
    public interface IEXIMJobFactory
    {
        IList<EXIMJob> GetEXIMJobs();
    }

    class EXIMJobFactory : IEXIMJobFactory
    {
        private static IConfigurationProvider _configProvider;

        public EXIMJobFactory(IConfigurationProvider configProvider)
        {
            _configProvider = configProvider;
        }

        public IList<EXIMJob> GetEXIMJobs()
        {
            var result = new List<EXIMJob>();

            foreach (var jobHelper in getJobHelpers())
            {
                var job = jobHelper.GetJob();
                result.Add(job);
            }

            return result;
        }

        private IList<IEXIMJobHelper> getJobHelpers()
        {
            var result = new List<IEXIMJobHelper>();
            result.Add(EXIMNijectModule.GetDependency<MeasuresJobHelper>());
            return result;
        }
    }
}
