using System.Windows;
using System.Windows.Threading;

namespace 图像索引
{
    public partial class App : Application
    {
        public App()
        {
            // 捕获 UI 线程的异常
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            // 显示具体错误，防止程序直接闪退
            MessageBox.Show($"程序发生错误:\n{e.Exception.Message}\n\n详细信息:\n{e.Exception.StackTrace}", "致命错误", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true; // 阻止程序崩溃退出
        }
    }
}