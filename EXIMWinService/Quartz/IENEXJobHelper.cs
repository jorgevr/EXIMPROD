using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EXIMWinService.Quartz
{
    public interface IEXIMJobHelper
    {
        EXIMJob GetJob();
    }
}
