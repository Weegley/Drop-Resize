using System;
using System.Windows.Forms;
using System.Reflection;
using System.Threading.Tasks;

namespace DropResize
{
    internal static class Program
    {
        /// <summary>
        /// Главная точка входа для приложения.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            var application = Assembly.GetExecutingAssembly();
            Task.Run(() => UpdateWorkspace.CleanupAfterStartupAsync(application.GetName().Version, application.Location));
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
