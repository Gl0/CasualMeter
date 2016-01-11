using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using CasualMeter.Common.Helpers;
using log4net;
using Lunyx.Common.UI.Wpf;

namespace CasualMeter
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : ISingleInstanceApp
    {
        private const string Unique = "283055BC-D5AC-46CC-B1A9-51C053BB9028";

        private static readonly ILog Logger = LogManager.GetLogger
            (MethodBase.GetCurrentMethod().DeclaringType);

        [STAThread]
        public static void Main()
        {
            if (SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                Logger.Info("Starting up.");
                // Initialize process helper
                ProcessHelper.Instance.Initialize();

                var application = new App();
                application.InitializeComponent();
                application.ShutdownMode = ShutdownMode.OnMainWindowClose;

                // register unhandled exceptions
                AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
                Current.DispatcherUnhandledException += CurrentOnDispatcherUnhandledException;
                TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

                application.Run();

                Logger.Info("Closing.");
                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();
                Environment.Exit(0);//application doesn't fully exit without this for some reason
            }
        }

 
        private static void HandleException(Exception e)
        {
            if (e == null) return;
            if (e.InnerException != null)
            {
                HandleException(e.InnerException);
            }
            else
            {
                Logger.Error(e);
            }
        }

        private static void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            e.SetObserved();
            HandleException(e.Exception);
        }

        private static void CurrentOnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            HandleException(e.Exception);
        }

        private static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            HandleException(e.ExceptionObject as Exception);
            if (e.IsTerminating)
                MessageBox.Show("There was an unexpected error. Please check the log for more details.", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            // handle command line arguments of second instance
            // ...
            return true;
        }

        private static string CleanReleaseNotes(string value)
        {
            var r = new Regex(@".*<p>(?<content>.*)</p>.*", RegexOptions.Singleline);
            var match = r.Match(value);
            return match.Success ? ScrubHtml(match.Groups["content"].Value) : value;
        }

        private static string ScrubHtml(string value)
        {
            var step1 = Regex.Replace(value, @"<[^>]+>|&nbsp;", "").Trim();
            var step2 = Regex.Replace(step1, @"\s{2,}", " ");
            return step2;
        }
    }
}
