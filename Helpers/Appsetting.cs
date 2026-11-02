using System.Collections.Generic;

namespace 图像索引.Helpers
{
    public static class AppSettings
    {
        // 默认支持的图片扩展名
        public static List<string> AllowedExtensions = new List<string>
        {
            ".jpg", ".jpeg", ".png", ".gif", ".bmp"
        };
    }
}