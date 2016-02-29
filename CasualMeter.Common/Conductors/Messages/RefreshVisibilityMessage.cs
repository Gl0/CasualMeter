using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CasualMeter.Common.Conductors.Messages
{
    public class RefreshVisibilityMessage
    {
        public bool? IsVisible { get; set; }
        public bool Toggle { get; set; }
    }
}
