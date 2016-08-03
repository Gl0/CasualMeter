using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CasualMeter.Common.Conductors.Messages
{
    [Flags]
    public enum ExportType
    {
        None = 0,
        Upload = 1,
        Excel = 2,
        ExcelTemp = 4
    }

    public class ExportStatsMessage
    {
        public ExportType ExportType { get; set; }
    }
}
