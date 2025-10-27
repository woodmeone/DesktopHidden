using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage; // 添加这个命名空间
using static Microsoft.UI.Colors; // 修改为 using static，因为 Colors 是一个静态类

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DesktopHidden
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;

        public static new List<Window> Windows { get; } = new List<Window>();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
            // 注册全局异常处理程序，用于捕获所有未处理的异常
            // 例如：当应用程序中发生未捕获的异常时，此事件将被触发，允许您记录错误信息。
            this.UnhandledException += App_UnhandledException;
        }

        /// <summary>
        /// 处理应用程序中未捕获的异常。
        /// </summary>
        /// <param name="sender">事件源。</param>
        /// <param name="e">提供事件数据的对象。</param>
        private async void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            e.Handled = true; // 标记异常已处理，防止应用程序崩溃
            string exceptionMessage = $"Unhandled Exception: {e.Message}\nSource: {e.Exception.Source}\nStack Trace: {e.Exception.StackTrace}";
            // 记录异常信息到文件
            // 例如：在应用数据文件夹中创建一个日志文件，将异常信息写入其中。
            var localFolder = global::Windows.Storage.ApplicationData.Current.LocalFolder;
            // 使用 OpenIfExists 和 FileAccessMode.ReadWrite 模拟 OpenOrCreate
            var logFile = await localFolder.CreateFileAsync("CrashLog.txt", global::Windows.Storage.CreationCollisionOption.OpenIfExists);
            await global::Windows.Storage.FileIO.AppendTextAsync(logFile, $"{DateTime.Now}: {exceptionMessage}\n");

            // 可以在此处显示一个友好的错误消息给用户
            // 例如：您可以弹出一个消息框，通知用户应用程序遇到了一个错误，并建议他们重新启动。
            // MessageBox.Show("应用程序发生了一个未知错误，即将退出。"); // 注意：WinUI 3 中没有MessageBox.Show，需要使用ContentDialog或其他方式

            // 如果需要，可以在此处终止应用程序
            // Application.Current.Exit();
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            _window = new MainWindow();
            Windows.Add(_window); // 将主窗口添加到全局列表中
            _window.Activate();
        }

        public Window? GetWindowForElement(UIElement element)
        {
            if (element == null || element.XamlRoot == null) return null;

            foreach (Window window in App.Windows) // 修改此处，遍历App.Windows
            {
                if (window.Content.XamlRoot == element.XamlRoot)
                {
                    return window;
                }
            }
            return null;
        }
    }
}
