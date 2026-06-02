// BlockLibraryPalette.cs
using Autodesk.AutoCAD.Windows;
using System;
using System.Drawing;

namespace MyBlockManager
{
    public class BlockLibraryPalette
    {
        private static PaletteSet _paletteSet;
        private static BlockLibraryControl _control;

        public void Show()
        {
            if (_paletteSet == null)
            {
                // GUID 必须是唯一的，用于AutoCAD识别和保存面板状态
                _paletteSet = new PaletteSet("我的图块库", new Guid("2D3E775F-91F3-4328-86F9-9B3B8B45E867"));
                _control = new BlockLibraryControl();

                _paletteSet.Add("图块列表", _control);

                _paletteSet.MinimumSize = new Size(300, 400);
                _paletteSet.Dock = DockSides.Left;

                // 订阅状态改变事件
                _paletteSet.StateChanged += PaletteSet_StateChanged;
            }

            _paletteSet.Visible = true;
        }

        /// <summary>
        /// 当面板状态（如显示/隐藏）改变时触发此方法
        /// </summary>
        private void PaletteSet_StateChanged(object sender, PaletteSetStateEventArgs e)
        {
            // --- 最终修复：不再使用有问题的StateEvent枚举 ---
            // 直接检查面板当前的可见状态。如果面板变为可见，就刷新。
            if (_paletteSet != null && _paletteSet.Visible)
            {
                // 调用我们之前写好的公共刷新方法
                _control?.RefreshContent();
            }
        }
    }
}

