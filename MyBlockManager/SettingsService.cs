// SettingsService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
// 为了使用JavaScriptSerializer，需要添加对 System.Web.Extensions 的引用
// 在“引用”->“添加引用”->“程序集”中找到它
using System.Web.Script.Serialization;

namespace MyBlockManager
{
    /// <summary>
    /// 管理插件的配置文件读写
    /// </summary>
    public class SettingsService
    {
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            // 将配置文件放在用户的AppData/Roaming文件夹下，这是一个安全且标准的位置
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string pluginFolder = Path.Combine(appDataPath, "MyBlockManager");

            // 如果文件夹不存在，则创建
            if (!Directory.Exists(pluginFolder))
            {
                Directory.CreateDirectory(pluginFolder);
            }

            _settingsFilePath = Path.Combine(pluginFolder, "settings.json");
        }

        /// <summary>
        /// 从 a settings.json 文件加载设置
        /// </summary>
        public AppSettings Load()
        {
            if (!File.Exists(_settingsFilePath))
            {
                return new AppSettings(); // 如果文件不存在，返回一个默认设置
            }

            try
            {
                string json = File.ReadAllText(_settingsFilePath);
                var serializer = new JavaScriptSerializer();
                // 反序列化结果可能为 null（如文件内容为 "null"），或 LibraryPaths 字段为 null，
                // 这里统一兜底，避免调用方出现 NullReferenceException
                var settings = serializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                if (settings.LibraryPaths == null)
                {
                    settings.LibraryPaths = new List<string>();
                }
                return settings;
            }
            catch (Exception)
            {
                // 如果文件损坏或格式错误，返回默认设置
                return new AppSettings();
            }
        }

        /// <summary>
        /// 将设置保存到 settings.json 文件
        /// </summary>
        public void Save(AppSettings settings)
        {
            try
            {
                var serializer = new JavaScriptSerializer();
                string json = serializer.Serialize(settings);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("保存设置失败: " + ex.Message);
                // 不再静默失败：提示用户设置未能保存
                MessageBox.Show(
                    "Failed to save settings: " + ex.Message,
                    "MyBlockManager",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }
    }
}