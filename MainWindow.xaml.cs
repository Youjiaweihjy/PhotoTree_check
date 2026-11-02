using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using 图像索引.Helpers;
using Microsoft.Win32;

namespace 图像索引
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var settingsWin = new SettingsWindow { Owner = this };
                settingsWin.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"打开设置窗口失败: {ex.Message}");
            }
        }

        private void BtnBrowseSource_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "请选择要读取的源文件夹（进入文件夹后随便选个文件点打开即可）",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择此文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                string? folderPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    TxtSourcePath.Text = folderPath;
                    LoadDirectoryTree(folderPath);
                }
            }
        }

        private void BtnBrowseDest_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "请选择要导出的目标文件夹（进入文件夹后随便选个文件点打开即可）",
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
                FileName = "选择此文件夹"
            };

            if (dialog.ShowDialog() == true)
            {
                string? folderPath = Path.GetDirectoryName(dialog.FileName);
                if (!string.IsNullOrEmpty(folderPath))
                {
                    TxtDestPath.Text = folderPath;
                }
            }
        }

        private void LoadDirectoryTree(string path)
        {
            DirTree.Items.Clear();
            if (!Directory.Exists(path)) return;

            try
            {
                var rootNode = CreateDirectoryNode(new DirectoryInfo(path));
                DirTree.Items.Add(rootNode);

                // 延迟展开，防止在 TreeView 还没渲染完毕时强行展开引发状态冲突
                Dispatcher.BeginInvoke(new Action(() => { rootNode.IsExpanded = true; }), System.Windows.Threading.DispatcherPriority.Background);

                TxtStatus.Text = $"已加载目录: {path}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载目录失败: {ex.Message}", "错误");
            }
        }

        private TreeViewItem CreateDirectoryNode(DirectoryInfo directoryInfo)
        {
            var item = new TreeViewItem { Header = directoryInfo.Name, Tag = directoryInfo.FullName };
            item.Expanded += TreeNode_Expanded;

            if (HasSubItems(directoryInfo))
            {
                item.Items.Add(new TreeViewItem { Header = "正在加载..." });
            }

            return item;
        }

        private bool HasSubItems(DirectoryInfo dir)
        {
            try
            {
                if (dir.EnumerateDirectories().Any()) return true;
                return dir.EnumerateFiles().Any(f => AppSettings.AllowedExtensions.Contains(f.Extension.ToLower()));
            }
            catch
            {
                return false; // 遇到无权限文件夹直接返回 false
            }
        }

        private void TreeNode_Expanded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem item && item.Tag is string path)
            {
                if (item.Items.Count == 1 && item.Items[0] is TreeViewItem dummy && dummy.Header?.ToString() == "正在加载...")
                {
                    item.Items.Clear();
                    var dirInfo = new DirectoryInfo(path);

                    try
                    {
                        // 1. 加载子文件夹
                        foreach (var directory in dirInfo.EnumerateDirectories())
                        {
                            if ((directory.Attributes & FileAttributes.System) == 0 &&
                                (directory.Attributes & FileAttributes.Hidden) == 0)
                            {
                                item.Items.Add(CreateDirectoryNode(directory));
                            }
                        }

                        // 2. 加载图片文件
                        foreach (var file in dirInfo.EnumerateFiles().Where(f => AppSettings.AllowedExtensions.Contains(f.Extension.ToLower())))
                        {
                            var fileItem = new TreeViewItem { Header = file.Name, Tag = file.FullName };
                            item.Items.Add(fileItem);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        item.Items.Add(new TreeViewItem { Header = "[无权限访问]" });
                    }
                    catch (Exception ex)
                    {
                        item.Items.Add(new TreeViewItem { Header = $"[加载错误: {ex.Message}]" });
                    }
                }
            }
        }

        private void DirTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (DirTree.SelectedItem is TreeViewItem selectedNode && selectedNode.Tag is string filePath)
            {
                if (File.Exists(filePath) && AppSettings.AllowedExtensions.Contains(Path.GetExtension(filePath).ToLower()))
                {
                    try
                    {
                        BitmapImage bitmap = new BitmapImage();
                        bitmap.BeginInit();
                        // 使用 FileStream 防止文件锁死
                        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                        {
                            bitmap.StreamSource = stream;
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.EndInit();
                        }
                        ImgPreview.Source = bitmap;
                        TxtStatus.Text = $"正在预览: {filePath}";
                    }
                    catch (Exception ex)
                    {
                        TxtStatus.Text = $"图片加载失败: {ex.Message}";
                    }
                }
            }
        }

        private void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            string keyword = TxtKeyword.Text.Trim();
            string sourcePath = TxtSourcePath.Text.Trim();
            string destPath = TxtDestPath.Text.Trim();

            if (string.IsNullOrEmpty(sourcePath) || string.IsNullOrEmpty(destPath))
            {
                MessageBox.Show("请先配置好读取和导出的路径！", "提示"); return;
            }
            if (string.IsNullOrEmpty(keyword))
            {
                MessageBox.Show("请输入要匹配的文件关键字！", "提示"); return;
            }

            try
            {
                TxtStatus.Text = "正在处理...";
                int count = FileExportHelper.ExportAndOrganizeFiles(
                    sourcePath,
                    keyword,
                    destPath,
                    AppSettings.AllowedExtensions.ToArray()
                );

                TxtStatus.Text = $"导出成功！共归类了 {count} 个文件到 {destPath}";
                MessageBox.Show($"成功导出 {count} 个文件！", "操作完成");
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "导出失败";
                MessageBox.Show($"导出过程中出现错误：\n{ex.Message}", "错误");
            }
        }
    }
}