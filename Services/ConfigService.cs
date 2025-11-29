using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using Newtonsoft.Json;
using NeroUnfreeze.Models;
using MessageBox = System.Windows.MessageBox;

namespace NeroUnfreeze.Services
{
    /// <summary>
    /// 配置服务 - 负责加载和保存用户配置，以及加载默认配置
    /// </summary>
    public class ConfigService
    {
        /// <summary>
        /// 获取exe文件所在目录
        /// </summary>
        private static string GetExeDirectory()
        {
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(exePath))
            {
                exePath = Environment.ProcessPath;
            }
            if (string.IsNullOrEmpty(exePath))
            {
                exePath = System.Windows.Forms.Application.ExecutablePath;
            }
            return Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// exe目录下的配置文件路径
        /// </summary>
        private static string GetLocalConfigPath()
        {
            return Path.Combine(GetExeDirectory(), "NeroUnfreezeConfig.json");
        }

        /// <summary>
        /// 从NeroUnfreezeConfig.json加载默认预设配置
        /// </summary>
        private static Preset? LoadDefaultPreset()
        {
            var configPath = GetLocalConfigPath();
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var defaultConfig = JsonConvert.DeserializeObject<DefaultConfigFile>(json);
                    if (defaultConfig?.DefaultPreset != null)
                    {
                        var presetData = defaultConfig.DefaultPreset;
                        // 解析日期字符串
                        if (!string.IsNullOrEmpty(presetData.TargetDateString) && 
                            DateTime.TryParse(presetData.TargetDateString, out var targetDate))
                        {
                            presetData.TargetDate = targetDate;
                        }
                        else
                        {
                        // 如果为空或解析失败，使用当前年份的12月25日
                        presetData.TargetDate = new DateTime(DateTime.Now.Year, 12, 25);
                        }
                        var preset = presetData.ToPreset();
                        // 解析路径：将相对路径转换为绝对路径
                        preset.CharacterImagePath = ResolvePath(preset.CharacterImagePath);
                        preset.IceImagePath = ResolvePath(preset.IceImagePath);
                        preset.AudioPath = ResolvePath(preset.AudioPath);
                        return preset;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载默认配置失败: {ex.Message}");
            }

            // 如果加载失败，返回硬编码的默认值（与default-config.json保持一致）
            return new Preset
            {
                Name = "默认组合",
                TargetDate = new DateTime(DateTime.Now.Year, 12, 25),
                CountdownDays = 7,
                CharacterImagePath = "",
                IceImagePath = "",
                AudioPath = "",
                CharacterOpacity = 1.0,
                IceOpacity = 1.0,
                CharacterImageScale = 1.0,
                IceImageScale = 1.0,
                CharacterOffsetX = 0.0,
                CharacterOffsetY = 0.0,
                IceOffsetX = 0.0,
                IceOffsetY = 0.0,
                MaxAudioBlur = 1.0,
                MinAudioVolume = 1.0
            };
        }

        /// <summary>
        /// 将相对路径转换为绝对路径（相对于exe目录）
        /// </summary>
        private static string ResolvePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            // 如果已经是绝对路径，直接返回
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            // 相对路径：相对于exe目录
            var exeDir = GetExeDirectory();
            return Path.GetFullPath(Path.Combine(exeDir, path));
        }

        /// <summary>
        /// 将绝对路径转换为相对路径（相对于exe目录），如果不在exe目录下则返回绝对路径
        /// </summary>
        private static string MakeRelativePath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return path;
            }

            var exeDir = GetExeDirectory();
            var fullPath = Path.GetFullPath(path);
            var exeDirFull = Path.GetFullPath(exeDir);

            // 如果路径在exe目录下，转换为相对路径
            if (fullPath.StartsWith(exeDirFull, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var relativePath = Path.GetRelativePath(exeDirFull, fullPath);
                    // 使用正斜杠，更通用
                    return relativePath.Replace('\\', '/');
                }
                catch
                {
                    return path;
                }
            }

            // 不在exe目录下，返回绝对路径
            return path;
        }

        /// <summary>
        /// 处理配置中的路径：将相对路径转换为绝对路径
        /// </summary>
        private static void ResolveConfigPaths(Config config)
        {
            foreach (var preset in config.Presets)
            {
                preset.CharacterImagePath = ResolvePath(preset.CharacterImagePath);
                preset.IceImagePath = ResolvePath(preset.IceImagePath);
                preset.AudioPath = ResolvePath(preset.AudioPath);
            }
        }

        /// <summary>
        /// 处理配置中的路径：将exe目录下的绝对路径转换为相对路径
        /// </summary>
        private static void MakeConfigPathsRelative(Config config)
        {
            foreach (var preset in config.Presets)
            {
                preset.CharacterImagePath = MakeRelativePath(preset.CharacterImagePath);
                preset.IceImagePath = MakeRelativePath(preset.IceImagePath);
                preset.AudioPath = MakeRelativePath(preset.AudioPath);
            }
        }

        /// <summary>
        /// 加载用户配置，如果不存在则使用默认配置
        /// 从exe目录下的NeroUnfreezeConfig.json加载
        /// </summary>
        public static Config LoadConfig()
        {
            var configPath = GetLocalConfigPath();
            
            // 从exe目录加载配置
            if (File.Exists(configPath))
            {
                try
                {
                    var json = File.ReadAllText(configPath);
                    var config = JsonConvert.DeserializeObject<Config>(json);
                    if (config != null && config.Presets.Count > 0)
                    {
                        // 处理路径：将相对路径转换为绝对路径
                        ResolveConfigPaths(config);
                        return config;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"加载配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            // 如果配置文件不存在或加载失败，从NeroUnfreezeConfig.json加载默认配置
            var defaultPreset = LoadDefaultPreset() ?? new Preset
            {
                Name = "默认组合",
                TargetDate = new DateTime(DateTime.Now.Year, 12, 25),
                CountdownDays = 7
            };
            var defaultConfig = new Config
            {
                Presets = new List<Preset> { defaultPreset },
                AutoStart = true,
                PreventMinimizeOnWinD = false
            };

            // 尝试从NeroUnfreezeConfig.json加载全局设置
            try
            {
                if (File.Exists(configPath))
                {
                    var json = File.ReadAllText(configPath);
                    var defaultConfigData = JsonConvert.DeserializeObject<DefaultConfigFile>(json);
                    if (defaultConfigData?.DefaultConfig != null)
                    {
                        defaultConfig.AutoStart = defaultConfigData.DefaultConfig.AutoStart;
                        defaultConfig.PreventMinimizeOnWinD = defaultConfigData.DefaultConfig.PreventMinimizeOnWinD;
                    }
                }
            }
            catch
            {
                // 忽略错误，使用硬编码默认值
            }

            return defaultConfig;
        }

        /// <summary>
        /// 获取默认预设配置（供创建新预设时使用）
        /// </summary>
        public static Preset GetDefaultPreset()
        {
            return LoadDefaultPreset() ?? new Preset();
        }

        /// <summary>
        /// 保存用户配置到文件
        /// 保存到exe目录下的NeroUnfreezeConfig.json
        /// </summary>
        public static void SaveConfig(Config config)
        {
            // 创建配置副本，避免修改原始配置
            var configToSave = JsonConvert.DeserializeObject<Config>(JsonConvert.SerializeObject(config));
            if (configToSave == null)
            {
                return;
            }

            // 处理路径：将exe目录下的绝对路径转换为相对路径
            MakeConfigPathsRelative(configToSave);

            // 保存到exe目录
            var localConfigPath = GetLocalConfigPath();
            try
            {
                var json = JsonConvert.SerializeObject(configToSave, Formatting.Indented);
                File.WriteAllText(localConfigPath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存配置失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    /// <summary>
    /// 默认配置文件数据模型
    /// </summary>
    public class DefaultConfigFile
    {
        [JsonProperty("DefaultPreset")]
        public DefaultPresetData? DefaultPreset { get; set; }

        [JsonProperty("DefaultConfig")]
        public DefaultConfigData? DefaultConfig { get; set; }
    }

    public class DefaultPresetData
    {
        [JsonProperty("Name")]
        public string Name { get; set; } = "默认组合";

        [JsonProperty("TargetDate")]
        public string TargetDateString { get; set; } = "";

        [JsonIgnore]
        public DateTime TargetDate { get; set; }

        [JsonProperty("CountdownDays")]
        public int CountdownDays { get; set; } = 7;

        [JsonProperty("CharacterImagePath")]
        public string CharacterImagePath { get; set; } = "";

        [JsonProperty("IceImagePath")]
        public string IceImagePath { get; set; } = "";

        [JsonProperty("AudioPath")]
        public string AudioPath { get; set; } = "";

        [JsonProperty("CharacterOpacity")]
        public double CharacterOpacity { get; set; } = 1.0;

        [JsonProperty("IceOpacity")]
        public double IceOpacity { get; set; } = 1.0;

        [JsonProperty("CharacterImageScale")]
        public double CharacterImageScale { get; set; } = 1.0;

        [JsonProperty("IceImageScale")]
        public double IceImageScale { get; set; } = 1.0;

        [JsonProperty("CharacterOffsetX")]
        public double CharacterOffsetX { get; set; } = 0.0;

        [JsonProperty("CharacterOffsetY")]
        public double CharacterOffsetY { get; set; } = 0.0;

        [JsonProperty("IceOffsetX")]
        public double IceOffsetX { get; set; } = 0.0;

        [JsonProperty("IceOffsetY")]
        public double IceOffsetY { get; set; } = 0.0;

        [JsonProperty("MaxAudioBlur")]
        public double MaxAudioBlur { get; set; } = 0.8;

        [JsonProperty("MinAudioVolume")]
        public double MinAudioVolume { get; set; } = 0.2;

        public Preset ToPreset()
        {
            return new Preset
            {
                Name = Name,
                TargetDate = TargetDate,
                CountdownDays = CountdownDays,
                CharacterImagePath = CharacterImagePath,
                IceImagePath = IceImagePath,
                AudioPath = AudioPath,
                CharacterOpacity = CharacterOpacity,
                IceOpacity = IceOpacity,
                CharacterImageScale = CharacterImageScale,
                IceImageScale = IceImageScale,
                CharacterOffsetX = CharacterOffsetX,
                CharacterOffsetY = CharacterOffsetY,
                IceOffsetX = IceOffsetX,
                IceOffsetY = IceOffsetY,
                MaxAudioBlur = MaxAudioBlur,
                MinAudioVolume = MinAudioVolume
            };
        }
    }

    public class DefaultConfigData
    {
        [JsonProperty("AutoStart")]
        public bool AutoStart { get; set; } = true;

        [JsonProperty("PreventMinimizeOnWinD")]
        public bool PreventMinimizeOnWinD { get; set; } = false;
    }
}

