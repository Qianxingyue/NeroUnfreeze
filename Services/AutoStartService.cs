using Microsoft.Win32;
using System;

namespace NeroUnfreeze.Services
{
    /// <summary>
    /// 开机自启动服务 - 通过注册表管理程序的开机自启动
    /// </summary>
    public class AutoStartService
    {
        private const string RegistryKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "NeroUnfreeze";

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey, false))
                {
                    return key?.GetValue(AppName) != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetAutoStart(bool enabled)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
                {
                    if (key == null)
                    {
                        Registry.CurrentUser.CreateSubKey(RegistryKey);
                    }
                }

                using (var key = Registry.CurrentUser.OpenSubKey(RegistryKey, true))
                {
                    if (enabled)
                    {
                        // 使用AppContext.BaseDirectory以支持单文件发布
                        var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                        if (string.IsNullOrEmpty(exePath))
                        {
                            // 单文件发布时，使用Environment.ProcessPath
                            exePath = Environment.ProcessPath ?? System.Windows.Forms.Application.ExecutablePath;
                        }
                        if (!string.IsNullOrEmpty(exePath))
                        {
                            key?.SetValue(AppName, exePath);
                        }
                    }
                    else
                    {
                        key?.DeleteValue(AppName, false);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"设置开机自启动失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }
}

