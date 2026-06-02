// PluginEntry.cs
using Autodesk.AutoCAD.Runtime;

namespace MyBlockManager
{
    public class PluginEntry : IExtensionApplication
    {
        private static BlockLibraryPalette _palette;

        [CommandMethod("MYBLOCK")]
        public void MyBlockCommand()
        {
            if (_palette == null)
            {
                _palette = new BlockLibraryPalette();
            }

            // 命令的唯一职责就是确保面板显示出来
            // 刷新操作会自动由 StateChanged 事件处理
            _palette.Show();
        }

        public void Initialize()
        {
            // 插件加载时执行的代码
        }

        public void Terminate()
        {
            // 插件卸载时执行的代码
        }
    }
}