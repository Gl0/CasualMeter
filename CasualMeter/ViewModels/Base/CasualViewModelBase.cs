using System.Reflection;
using log4net;
using Lunyx.Common.UI.Wpf;

namespace CasualMeter.ViewModels.Base
{
    public class CasualViewModelBase : ViewModelBase
    {
        protected static readonly ILog Logger = LogManager.GetLogger
            (MethodBase.GetCurrentMethod().DeclaringType);
    }
}
