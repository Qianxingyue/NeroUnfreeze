using System;
using System.Windows;
using System.Windows.Forms;
using System.Drawing;
using System.IO;
using NeroUnfreeze.Models;
using NeroUnfreeze.Services;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace NeroUnfreeze.Windows
{
    /// <summary>
    /// 主窗口 - 程序入口，负责系统托盘、配置管理和效果触发
    /// </summary>
    public partial class MainWindow : Window
    {
        private NotifyIcon? notifyIcon;
        private ImageDisplayWindow? imageWindow;
        private Config config = null!;
        private bool hasPlayedAudio = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadConfig();
            InitializeTrayIcon();
            CheckAndUpdate();
            SetAutoStart();
        }

        /// <summary>
        /// 加载用户配置
        /// </summary>
        private void LoadConfig()
        {
            config = ConfigService.LoadConfig();
        }

        /// <summary>
        /// 初始化系统托盘图标
        /// </summary>
        private void InitializeTrayIcon()
        {
            Icon? trayIcon = null;
            
            try
            {
                // 方法1: 从exe文件提取图标（因为ApplicationIcon已设置，图标已嵌入到exe中）
                var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Environment.ProcessPath;
                }
                if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
                {
                    try
                    {
                        trayIcon = System.Drawing.Icon.ExtractAssociatedIcon(exePath);
                    }
                    catch
                    {
                        // 如果提取失败，继续尝试其他方法
                    }
                }

                // 方法2: 从运行目录加载icon.ico（作为备用方案）
                if (trayIcon == null)
                {
                    var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "icon.ico");
                    if (File.Exists(iconPath))
                    {
                        trayIcon = new Icon(iconPath);
                    }
                }
            }
            catch
            {
                // 如果加载失败，使用默认图标
            }

            notifyIcon = new NotifyIcon
            {
                Icon = trayIcon ?? SystemIcons.Application,
                Text = "NeroUnfreeze",
                Visible = true
            };

            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("设置", null, (s, e) => OpenSettings());
            contextMenu.Items.Add("关于", null, (s, e) => ShowAbout());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => ExitApplication());

            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.DoubleClick += (s, e) => OpenSettings();
        }

        /// <summary>
        /// 根据配置设置开机自启动
        /// </summary>
        private void SetAutoStart()
        {
            if (config.AutoStart)
            {
                AutoStartService.SetAutoStart(true);
            }
        }

        /// <summary>
        /// 检查日期并更新显示效果和播放音频
        /// </summary>
        private void CheckAndUpdate()
        {
            if (config.Presets.Count == 0 || config.SelectedPresetIndex >= config.Presets.Count)
            {
                return;
            }

            var preset = config.Presets[config.SelectedPresetIndex];
            var today = DateTime.Now.Date;
            var targetDate = preset.TargetDate.Date;
            var daysRemaining = (targetDate - today).Days;

            if (daysRemaining < 0)
            {
                // 节日已过，隐藏窗口
                if (imageWindow != null)
                {
                    imageWindow.Hide();
                }
                return;
            }

            if (daysRemaining <= preset.CountdownDays)
            {
                // 计算透明度进度 (0 = 目标日期, 1 = 倒计时开始)
                var progress = daysRemaining / (double)preset.CountdownDays;
                progress = Math.Max(0, Math.Min(1, progress));

                double characterOpacity;
                double iceOpacity;

                if (progress > 0.5)
                {
                    // 前半部分：progress 从 1.0 到 0.5
                    // 角色透明度保持为0（不可见），但在最后一天（progress = 0.5）时变为1.0
                    characterOpacity = 0.0;
                    // 冰块透明度从0.2逐渐均匀上升到1.0
                    // progress = 1.0 时，iceOpacity = 0.2
                    // progress = 0.5 时，iceOpacity = 1.0
                    var firstHalfProgress = (1.0 - progress) / 0.5; // 从0到1
                    iceOpacity = 0.2 + (1.0 - 0.2) * firstHalfProgress;
                }
                else
                {
                    // 后半部分：progress 从 0.5 到 0.0（包括0.5）
                    // 角色透明度保持1.0不变（包括前半部分的最后一天）
                    characterOpacity = 1.0;
                    // 冰块透明度从1.0逐渐降低到0
                    // progress = 0.5 时，iceOpacity = 1.0
                    // progress = 0.0 时，iceOpacity = 0.0
                    var secondHalfProgress = progress / 0.5; // 从1到0
                    iceOpacity = 1.0 * secondHalfProgress;
                }

                // 显示图片窗口
                if (imageWindow == null)
                {
                    imageWindow = new ImageDisplayWindow();
                }

                if (!string.IsNullOrEmpty(preset.CharacterImagePath) && File.Exists(preset.CharacterImagePath) &&
                    !string.IsNullOrEmpty(preset.IceImagePath) && File.Exists(preset.IceImagePath))
                {
                    imageWindow.UpdateImages(
                        preset.CharacterImagePath, preset.IceImagePath, 
                        characterOpacity, iceOpacity, 
                        preset.CharacterImageScale, preset.IceImageScale,
                        preset.CharacterOffsetX, preset.CharacterOffsetY,
                        preset.IceOffsetX, preset.IceOffsetY);
                    imageWindow.SetPreventMinimize(config.PreventMinimizeOnWinD);
                    imageWindow.Show();
                }

                // 播放音频（只播放一次）
                if (!hasPlayedAudio && !string.IsNullOrEmpty(preset.AudioPath) && File.Exists(preset.AudioPath))
                {
                    // 模糊值逻辑：第一天0.99，节日当天0，中间均匀下降
                    // progress = 1.0 时（第一天），audioBlur = 0.99
                    // progress = 0.0 时（节日当天），audioBlur = 0.0
                    var audioBlur = 0.99 * progress;
                    
                    // 音量逻辑：分为两部分
                    double audioVolume;
                    if (progress > 0.5)
                    {
                        // 前半部分：progress 从 1.0 到 0.5
                        if (progress >= 1.0)
                        {
                            // 第一天（progress = 1.0）：音量 = 1.0（因为模糊度为0.99，需要大音量才能听到）
                            audioVolume = 1.0;
                        }
                        else
                        {
                            // 之后每天：音量从0.2均匀提升到0.4
                            // progress = 1.0 时，audioVolume = 0.2（但第一天特殊处理为1.0）
                            // progress = 0.5 时，audioVolume = 0.4
                            var firstHalfProgress = (1.0 - progress) / 0.5; // 从0到1（不包括progress=1.0的情况）
                            audioVolume = 0.2 + (0.4 - 0.2) * firstHalfProgress;
                        }
                    }
                    else
                    {
                        // 后半部分：progress 从 0.5 到 0.0
                        // 音量从0.5提升到1.0
                        // progress = 0.5 时，audioVolume = 0.5
                        // progress = 0.0 时，audioVolume = 1.0
                        var secondHalfProgress = (0.5 - progress) / 0.5; // 从0到1
                        audioVolume = 0.5 + (1.0 - 0.5) * secondHalfProgress;
                    }
                    
                    AudioService.PlayAudioWithBlur(preset.AudioPath, audioBlur, audioVolume);
                    hasPlayedAudio = true;
                }
            }
            else
            {
                // 隐藏窗口
                if (imageWindow != null)
                {
                    imageWindow.Hide();
                }
            }
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsWindow(config);
            settingsWindow.WindowState = WindowState.Normal;
            settingsWindow.ShowActivated = true;
            if (settingsWindow.ShowDialog() == true)
            {
                config = settingsWindow.Config;
                ConfigService.SaveConfig(config);
                hasPlayedAudio = false; // 重置音频播放标志
                CheckAndUpdate();
            }
        }

        private void ShowAbout()
        {
            MessageBox.Show(
                "NeroUnfreeze\n\n" +
                "圣诞节快乐！PADORU~PADORU~\n\n" +
                "版本: 1.0",
                "关于",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void ExitApplication()
        {
            notifyIcon?.Dispose();
            imageWindow?.Close();
            Application.Current.Shutdown();
        }

        protected override void OnClosed(EventArgs e)
        {
            notifyIcon?.Dispose();
            imageWindow?.Close();
            base.OnClosed(e);
        }
    }
}

