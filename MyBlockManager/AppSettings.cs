// AppSettings.cs
using System.Collections.Generic;

namespace MyBlockManager
{
    /// <summary>
    /// 定义插件的设置项
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// 存储用户添加的图块库文件夹路径
        /// </summary>
        public List<string> LibraryPaths { get; set; }

        public AppSettings()
        {
            LibraryPaths = new List<string>();
        }
    }
}