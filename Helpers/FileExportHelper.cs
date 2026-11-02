using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace 图像索引.Helpers
{
    public class FileExportHelper
    {
        // 注意：这里的 string[] extensions 改成了 string[]? extensions，? 代表允许为 null
        public static int ExportAndOrganizeFiles(string sourceDirectory, string keyword, string destDirectory, string[]? extensions = null)
        {
            if (!Directory.Exists(sourceDirectory))
                throw new DirectoryNotFoundException("源文件夹不存在！");

            if (string.IsNullOrWhiteSpace(destDirectory))
                throw new ArgumentException("目标文件夹路径不能为空！");

            if (!Directory.Exists(destDirectory))
            {
                Directory.CreateDirectory(destDirectory);
            }

            int copyCount = 0;
            var searchOption = SearchOption.AllDirectories;

            var files = Directory.GetFiles(sourceDirectory, "*.*", searchOption)
                                 .Where(f => Path.GetFileName(f).Contains(keyword));

            if (extensions != null && extensions.Length > 0)
            {
                files = files.Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));
            }

            foreach (var file in files)
            {
                try
                {
                    string fileName = Path.GetFileName(file);
                    string destPath = Path.Combine(destDirectory, fileName);

                    if (File.Exists(destPath))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                        string ext = Path.GetExtension(fileName);
                        destPath = Path.Combine(destDirectory, $"{nameWithoutExt}_copy_{Guid.NewGuid().ToString("N").Substring(0, 4)}{ext}");
                    }

                    File.Copy(file, destPath);
                    copyCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"复制文件 {file} 失败: {ex.Message}");
                }
            }
            return copyCount;
        }
    }
}