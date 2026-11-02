using System;
using System.Linq;
using System.Windows;
using 图像索引.Helpers;

namespace 图像索引
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            // 加载当前的扩展名配置
            TxtExtensions.Text = string.Join(", ", AppSettings.AllowedExtensions);
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            // 解析用户输入的逗号分隔字符串
            var input = TxtExtensions.Text;
            var exts = input.Split(new[] { ',', '，' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToLower())
                            .Where(s => s.StartsWith("."))
                            .Distinct()
                            .ToList();

            if (exts.Count == 0)
            {
                MessageBox.Show("请至少输入一个有效的扩展名（需以 . 开头）！", "提示");
                return;
            }

            AppSettings.AllowedExtensions = exts;
            MessageBox.Show("设置已保存！重新浏览目录生效。", "成功");
            this.DialogResult = true;
            this.Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}