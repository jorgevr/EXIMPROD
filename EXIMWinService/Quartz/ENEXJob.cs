using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Quartz;

namespace EXIMWinService.Quartz
{
    public class EXIMJob
    {
        public string Name { get; set; }
        public ITrigger Trigger { get; set; }
        public IJobDetail JobDetail { get; set; }
    }
}
