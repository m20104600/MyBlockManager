# MyBlockManager

一个 AutoCAD 2023 图块库管理插件：在停靠面板中浏览、搜索、预览并一键插入图块库中的图块（`.dwg`）。

## ✨ 功能特性

- 📁 **图块库管理**：指定一个图块库文件夹，自动**递归扫描**其中的 `.dwg` 文件
- 🔍 **实时搜索**：按文件名即时过滤（不区分大小写）
- 🖼️ **缩略图预览**：选中条目即**异步加载**该 `.dwg` 的缩略图，不卡界面
- 🖱️ **一键插入**：双击列表项，在图纸中拖动定位并插入图块
- 🌓 **主题自适应**：自动跟随 AutoCAD 明 / 暗主题（系统变量 `COLORTHEME`）

## 🧩 环境要求

- AutoCAD 2023
- .NET Framework 4.8.1
- 目标平台：x64

## 🚀 安装与使用

1. 在 AutoCAD 命令行输入 `NETLOAD`，选择编译好的 `MyBlockManager.dll`
2. 输入命令 `MYBLOCK`，打开「我的图块库」面板
3. 点击 **添加库** 选择图块文件夹，或在路径框中粘贴路径后按回车
4. 在列表中搜索 / 选择图块，**双击**即可插入到当前图纸

| 命令 | 说明 |
|------|------|
| `MYBLOCK` | 打开 / 显示图块库面板 |

## 🛠️ 从源码编译

```powershell
msbuild MyBlockManager\MyBlockManager.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x64
```

- 默认引用 `C:\Program Files\Autodesk\AutoCAD 2023\` 下的 AutoCAD 托管程序集（`accoremgd` / `acdbmgd` / `acmgd`）。
- 若 AutoCAD 安装在其他位置或为其他版本，**无需修改项目文件**，在编译时覆盖 `AutoCADPath` 即可：

  ```powershell
  msbuild MyBlockManager\MyBlockManager.csproj /t:Rebuild /p:Configuration=Release /p:Platform=x64 /p:AutoCADPath="D:\Autodesk\AutoCAD 2024\"
  ```

- 输出位置：`MyBlockManager\bin\Release\MyBlockManager.dll`

## 📂 项目结构

```
MyBlockManager/
├── MyBlockManager.sln
└── MyBlockManager/
    ├── PluginEntry.cs                 # 插件入口，注册 MYBLOCK 命令
    ├── BlockLibraryPalette.cs         # AutoCAD 停靠面板（PaletteSet）
    ├── BlockLibraryControl.cs         # 核心 UI 逻辑：扫描 / 搜索 / 预览 / 插入 / 主题
    ├── BlockLibraryControl.Designer.cs
    ├── SettingsService.cs             # 设置读写（JSON）
    ├── AppSettings.cs                 # 设置数据模型
    └── Properties/AssemblyInfo.cs
```

## ⚙️ 配置文件

用户设置（图块库路径）保存在：

```
%AppData%\MyBlockManager\settings.json
```

删除该文件即可重置为默认设置。
