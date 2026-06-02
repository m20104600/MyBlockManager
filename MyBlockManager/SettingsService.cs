// SettingsService.cs
using System;
using System.IO;
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
                return serializer.Deserialize<AppSettings>(json);
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
                // 可以添加错误日志记录
                System.Diagnostics.Debug.WriteLine("保存设置失败: " + ex.Message);
            }
        }
    }
}