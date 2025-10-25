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
using Windows.Foundation;
using Windows.Foundation.Collections;
using DesktopHidden.Managers;
using DesktopHidden.Models;
using DesktopHidden.Views;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DesktopHidden
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private readonly SubZoneManager _subZoneManager;

        public MainWindow()
        {
            this.InitializeComponent();
            _subZoneManager = new SubZoneManager();

            // 监听SubZones集合的变化，当有新的子区模型添加时，创建对应的SubZoneWindow
            _subZoneManager.SubZones.CollectionChanged += SubZones_CollectionChanged;
        }

        private void SubZones_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Models.SubZoneModel newSubZoneModel in e.NewItems)
                {
                    var subZoneWindow = new SubZoneWindow(newSubZoneModel);
                    App.Windows.Add(subZoneWindow); // 将子区窗口添加到全局列表中
                    subZoneWindow.Activate();
                }
            }
        }

        private void AddSubZoneButton_Click(object sender, RoutedEventArgs e)
        {
            // 示例：创建一个默认大小和位置的子区
            // 这里可以添加逻辑让用户选择子区位置和大小，目前先使用固定值
            _subZoneManager.AddSubZone(new Point(100, 100), new Size(200, 150));
        }
    }
}
