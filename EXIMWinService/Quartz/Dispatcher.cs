using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;
using Quartz.Impl;
using log4net;
using EXIMWinService.Services;
using EXIMWinService.WebApi;


namespace EXIMWinService.Quartz
{
    public interface ICronDispatcher : IDisposable
    {
        void Init();
    }

    public class Dispatcher : ICronDispatcher
    {
        private static readonly ILog _logger = LogManager.GetLogger(typeof(Dispatcher));

        private static IScheduler _scheduler;
        private static IEXIMJobFactory _jobFactory;

        public Dispatcher(IEXIMJobFactory jobFactory)
        {
            _scheduler = new StdSchedulerFactory().GetScheduler();
            _jobFactory = jobFactory;
        }

        public void Init()
        {
            try
            {
                _logger.Info("Starting Dispatcher");
                init();
            }
            catch (Exception e)
            {
                _logger.ErrorFormat("Exception while preparing jobs: {0}\nStackTrace: \n{1}\n", e.Message, e.StackTrace);
            }
        }

        private void init()
        {
            foreach (var EXIMJob in _jobFactory.GetEXIMJobs())
            {
                scheduleEXIMJob(EXIMJob);
            }
            _scheduler.Start();
        }

        private void scheduleEXIMJob(EXIMJob EXIMJob)
        {
            _logger.InfoFormat("Scheduling job {0}", EXIMJob.Name);
            _scheduler.ScheduleJob(EXIMJob.JobDetail, EXIMJob.Trigger);
        }

        public void Dispose()
        {
            _logger.Info("Disposing Dispatcher");
            if (_scheduler != null)
            {
                _scheduler.Shutdown(false);
                _scheduler = null;
            }
        }
    }
}
