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
using System.Text.Json; // 添加此行
using Windows.Storage; // 添加此行
using System.Collections.ObjectModel; // 添加此行
using System.Threading.Tasks; // 添加此行

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
        private const string SubZonesDataFileName = "subzones.json"; // 定义保存数据的文件名

        public MainWindow()
        {
            this.InitializeComponent();
            _subZoneManager = new SubZoneManager();

            // 监听SubZones集合的变化，当有新的子区模型添加时，创建对应的SubZoneWindow
            _subZoneManager.SubZones.CollectionChanged += SubZones_CollectionChanged;

            // 监听主窗口关闭事件，以便在应用程序退出时恢复隐藏的快捷方式和保存数据
            this.Closed += MainWindow_Closed;

            // 在窗口初始化时加载之前保存的子区数据
            _ = LoadSubZonesAsync();
        }

        private async void MainWindow_Closed(object sender, WindowEventArgs args)
        {
            App.RestoreHiddenShortcuts(); // 恢复所有隐藏的快捷方式
            await SaveSubZonesAsync(); // 保存子区数据
        }

        private void SubZones_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (Models.SubZoneModel newSubZoneModel in e.NewItems)
                {
                    var subZoneWindow = new SubZoneWindow(newSubZoneModel);
                    subZoneWindow.RequestClose += SubZoneWindow_RequestClose; // 订阅 RequestClose 事件
                    App.Windows.Add(subZoneWindow); // 将子区窗口添加到全局列表中
                    subZoneWindow.Activate();
                }
            }
        }

        private void SubZoneWindow_RequestClose(object? sender, Guid subZoneId)
        {
            // 从 App.Windows 列表中移除并关闭对应的 SubZoneWindow
            var windowToRemove = App.Windows.FirstOrDefault(w => (w as SubZoneWindow)?.SubZoneModel.Id == subZoneId);
            if (windowToRemove != null)
            {
                App.Windows.Remove(windowToRemove);
                windowToRemove.Close();
            }
            // 同时从 SubZoneManager 中移除对应的 SubZoneModel
            var modelToRemove = _subZoneManager.SubZones.FirstOrDefault(m => m.Id == subZoneId);
            if (modelToRemove != null)
            {
                _subZoneManager.SubZones.Remove(modelToRemove);
            }
        }

        private void AddSubZoneButton_Click(object sender, RoutedEventArgs e)
        {
            // 示例：创建一个默认大小和位置的子区
            // 这里可以添加逻辑让用户选择子区位置和大小，目前先使用固定值
            _subZoneManager.AddSubZone(new Point(100, 100), new Size(200, 150));
        }

        /// <summary>
        /// 将当前的子区数据保存到文件。
        /// </summary>
        private async Task SaveSubZonesAsync()
        {
            try
            {
                var serializableSubZones = new List<SerializableSubZoneModel>();
                foreach (var subZone in _subZoneManager.SubZones)
                {
                    var serializableSubZone = new SerializableSubZoneModel
                    {
                        Id = subZone.Id,
                        Position = subZone.Position,
                        Size = subZone.Size,
                        OriginalSize = subZone.OriginalSize,
                        BackgroundColor = subZone.BackgroundColor,
                        Opacity = subZone.Opacity,
                        IsLocked = subZone.IsLocked,
                        IsContentVisible = subZone.IsContentVisible,
                        Shortcuts = subZone.Shortcuts.Select(s => new SerializableShortcutModel
                        {
                            Path = s.Path,
                            Name = s.Name,
                            OriginalPath = s.OriginalPath
                        }).ToList()
                    };
                    serializableSubZones.Add(serializableSubZone);
                }

                var json = JsonSerializer.Serialize(serializableSubZones);
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.CreateFileAsync(SubZonesDataFileName, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, json);
                System.Diagnostics.Debug.WriteLine("SubZone data saved successfully.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving SubZone data: {ex.Message}");
            }
        }

        /// <summary>
        /// 从文件加载子区数据并重建。
        /// </summary>
        private async Task LoadSubZonesAsync()
        {
            try
            {
                StorageFolder localFolder = ApplicationData.Current.LocalFolder;
                StorageFile file = await localFolder.GetFileAsync(SubZonesDataFileName);
                string json = await FileIO.ReadTextAsync(file);
                var serializableSubZones = JsonSerializer.Deserialize<List<SerializableSubZoneModel>>(json);

                if (serializableSubZones != null)
                {
                    foreach (var serializableSubZone in serializableSubZones)
                    {
                        var newSubZone = new SubZoneModel
                        {
                            Id = serializableSubZone.Id,
                            Position = serializableSubZone.Position,
                            Size = serializableSubZone.Size,
                            OriginalSize = serializableSubZone.OriginalSize,
                            BackgroundColor = serializableSubZone.BackgroundColor,
                            Opacity = serializableSubZone.Opacity,
                            IsLocked = serializableSubZone.IsLocked,
                            IsContentVisible = serializableSubZone.IsContentVisible
                        };

                        // 重新加载快捷方式图标
                        foreach (var serializableShortcut in serializableSubZone.Shortcuts)
                        {
                            var shortcut = new ShortcutModel
                            {
                                Path = serializableShortcut.Path,
                                Name = serializableShortcut.Name,
                                OriginalPath = (string?)serializableShortcut.OriginalPath,
                                Icon = await SubZoneView.GetAppIcon(serializableShortcut.Path) // 重新获取图标
                            };
                            newSubZone.Shortcuts.Add(shortcut);
                            // 如果原始快捷方式被隐藏，则将其路径添加到全局隐藏列表中
                            if (!string.IsNullOrEmpty(shortcut.OriginalPath))
                            {
                                App.HiddenShortcutOriginalPaths.Add(shortcut.OriginalPath);
                            }
                        }
                        _subZoneManager.SubZones.Add(newSubZone);
                    }
                    System.Diagnostics.Debug.WriteLine("SubZone data loaded successfully.");
                }
            }
            catch (FileNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("SubZones data file not found. Starting with empty data.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading SubZone data: {ex.Message}");
            }
        }
    }
}
