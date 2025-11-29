using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using Microsoft.Win32;
using NeroUnfreeze.Models;
using NeroUnfreeze.Services;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace NeroUnfreeze.Windows
{
    /// <summary>
    /// 设置窗口 - 提供可视化界面用于配置所有参数
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public Config Config { get; private set; }
        private Preset? currentPreset;

        public SettingsWindow(Config config)
        {
            InitializeComponent();
            Config = new Config
            {
                Presets = config.Presets.Select(p => new Preset
                {
                    Name = p.Name,
                    TargetDate = p.TargetDate,
                    CountdownDays = p.CountdownDays,
                    CharacterImagePath = p.CharacterImagePath,
                    IceImagePath = p.IceImagePath,
                    AudioPath = p.AudioPath,
                    CharacterOpacity = p.CharacterOpacity,
                    IceOpacity = p.IceOpacity,
                    CharacterImageScale = p.CharacterImageScale,
                    IceImageScale = p.IceImageScale,
                    CharacterOffsetX = p.CharacterOffsetX,
                    CharacterOffsetY = p.CharacterOffsetY,
                    IceOffsetX = p.IceOffsetX,
                    IceOffsetY = p.IceOffsetY,
                    MaxAudioBlur = p.MaxAudioBlur,
                    MinAudioVolume = p.MinAudioVolume
                }).ToList(),
                SelectedPresetIndex = config.SelectedPresetIndex,
                AutoStart = config.AutoStart,
                PreventMinimizeOnWinD = config.PreventMinimizeOnWinD
            };

            AutoStartCheckBox.IsChecked = Config.AutoStart;
            PreventMinimizeOnWinDCheckBox.IsChecked = Config.PreventMinimizeOnWinD;
            LoadPresets();
            LoadCurrentPreset();
        }

        private void LoadPresets()
        {
            PresetComboBox.Items.Clear();
            foreach (var preset in Config.Presets)
            {
                PresetComboBox.Items.Add(preset.Name);
            }
            if (Config.Presets.Count > 0 && Config.SelectedPresetIndex < Config.Presets.Count)
            {
                PresetComboBox.SelectedIndex = Config.SelectedPresetIndex;
            }
        }

        private void LoadCurrentPreset()
        {
            if (PresetComboBox.SelectedIndex < 0 || PresetComboBox.SelectedIndex >= Config.Presets.Count)
            {
                return;
            }

            currentPreset = Config.Presets[PresetComboBox.SelectedIndex];
            Config.SelectedPresetIndex = PresetComboBox.SelectedIndex;

            NameTextBox.Text = currentPreset.Name;
            TargetDatePicker.SelectedDate = currentPreset.TargetDate;
            CountdownDaysSlider.Value = currentPreset.CountdownDays;
            CountdownDaysTextBox.Text = currentPreset.CountdownDays.ToString();
            CharacterImagePathTextBox.Text = currentPreset.CharacterImagePath;
            IceImagePathTextBox.Text = currentPreset.IceImagePath;
            AudioPathTextBox.Text = currentPreset.AudioPath;
            CharacterOpacitySlider.Value = currentPreset.CharacterOpacity;
            CharacterOpacityTextBox.Text = currentPreset.CharacterOpacity.ToString("F2");
            IceOpacitySlider.Value = currentPreset.IceOpacity;
            IceOpacityTextBox.Text = currentPreset.IceOpacity.ToString("F2");
            CharacterImageScaleSlider.Value = currentPreset.CharacterImageScale;
            CharacterImageScaleTextBox.Text = currentPreset.CharacterImageScale.ToString("F2");
            IceImageScaleSlider.Value = currentPreset.IceImageScale;
            IceImageScaleTextBox.Text = currentPreset.IceImageScale.ToString("F2");
            CharacterOffsetXSlider.Value = currentPreset.CharacterOffsetX;
            CharacterOffsetXTextBox.Text = currentPreset.CharacterOffsetX.ToString("F0");
            CharacterOffsetYSlider.Value = currentPreset.CharacterOffsetY;
            CharacterOffsetYTextBox.Text = currentPreset.CharacterOffsetY.ToString("F0");
            IceOffsetXSlider.Value = currentPreset.IceOffsetX;
            IceOffsetXTextBox.Text = currentPreset.IceOffsetX.ToString("F0");
            IceOffsetYSlider.Value = currentPreset.IceOffsetY;
            IceOffsetYTextBox.Text = currentPreset.IceOffsetY.ToString("F0");
            MaxAudioBlurSlider.Value = currentPreset.MaxAudioBlur;
            MaxAudioBlurTextBox.Text = currentPreset.MaxAudioBlur.ToString("F2");
            MinAudioVolumeSlider.Value = currentPreset.MinAudioVolume;
            MinAudioVolumeTextBox.Text = currentPreset.MinAudioVolume.ToString("F2");

            UpdatePreview();
        }

        /// <summary>
        /// 解析图片路径（相对路径转绝对路径）
        /// </summary>
        private string ResolveImagePath(string path)
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
            var exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrEmpty(exePath))
            {
                exePath = Environment.ProcessPath;
            }
            if (string.IsNullOrEmpty(exePath))
            {
                exePath = System.Windows.Forms.Application.ExecutablePath;
            }
            var exeDir = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            return Path.GetFullPath(Path.Combine(exeDir, path));
        }

        private void UpdatePreview()
        {
            if (currentPreset == null) return;

            try
            {
                var canvas = (System.Windows.Controls.Canvas)PreviewCharacterImage.Parent;
                var baseX = 50.0;
                var baseY = 50.0;

                // 更新角色图片预览
                if (!string.IsNullOrEmpty(currentPreset.CharacterImagePath))
                {
                    // 解析路径（相对路径转绝对路径）
                    var imagePath = ResolveImagePath(currentPreset.CharacterImagePath);
                    if (File.Exists(imagePath))
                    {
                        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                        PreviewCharacterImage.Source = bitmap;
                        PreviewCharacterImage.Opacity = currentPreset.CharacterOpacity;
                        PreviewCharacterImage.Width = bitmap.PixelWidth * currentPreset.CharacterImageScale;
                        PreviewCharacterImage.Height = bitmap.PixelHeight * currentPreset.CharacterImageScale;
                        System.Windows.Controls.Canvas.SetLeft(PreviewCharacterImage, baseX + currentPreset.CharacterOffsetX);
                        System.Windows.Controls.Canvas.SetTop(PreviewCharacterImage, baseY + currentPreset.CharacterOffsetY);
                    }
                    else
                    {
                        PreviewCharacterImage.Source = null;
                    }
                }
                else
                {
                    PreviewCharacterImage.Source = null;
                }

                // 更新冰块图片预览（根据模拟进度）
                if (!string.IsNullOrEmpty(currentPreset.IceImagePath))
                {
                    // 解析路径（相对路径转绝对路径）
                    var imagePath = ResolveImagePath(currentPreset.IceImagePath);
                    if (File.Exists(imagePath))
                    {
                        var bitmap = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                        PreviewIceImage.Source = bitmap;
                        // 模拟进度：假设当前是倒计时中间
                        var simulatedProgress = 0.5;
                        PreviewIceImage.Opacity = currentPreset.IceOpacity * simulatedProgress;
                        PreviewIceImage.Width = bitmap.PixelWidth * currentPreset.IceImageScale;
                        PreviewIceImage.Height = bitmap.PixelHeight * currentPreset.IceImageScale;
                        System.Windows.Controls.Canvas.SetLeft(PreviewIceImage, baseX + currentPreset.IceOffsetX);
                        System.Windows.Controls.Canvas.SetTop(PreviewIceImage, baseY + currentPreset.IceOffsetY);
                    }
                    else
                    {
                        PreviewIceImage.Source = null;
                    }
                }
                else
                {
                    PreviewIceImage.Source = null;
                }
            }
            catch
            {
                // 忽略预览错误
            }
        }

        private void PresetComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            LoadCurrentPreset();
        }

        private void AddPreset_Click(object sender, RoutedEventArgs e)
        {
            // 使用默认配置创建新预设
            var defaultPreset = ConfigService.GetDefaultPreset();
            var newPreset = new Preset
            {
                Name = $"组合 {Config.Presets.Count + 1}",
                // 节日日期设置为当前年份的12月25日
                TargetDate = new DateTime(DateTime.Now.Year, 12, 25),
                CountdownDays = defaultPreset.CountdownDays,
                CharacterImagePath = defaultPreset.CharacterImagePath,
                IceImagePath = defaultPreset.IceImagePath,
                AudioPath = defaultPreset.AudioPath,
                CharacterOpacity = defaultPreset.CharacterOpacity,
                IceOpacity = defaultPreset.IceOpacity,
                CharacterImageScale = defaultPreset.CharacterImageScale,
                IceImageScale = defaultPreset.IceImageScale,
                CharacterOffsetX = defaultPreset.CharacterOffsetX,
                CharacterOffsetY = defaultPreset.CharacterOffsetY,
                IceOffsetX = defaultPreset.IceOffsetX,
                IceOffsetY = defaultPreset.IceOffsetY,
                MaxAudioBlur = defaultPreset.MaxAudioBlur,
                MinAudioVolume = defaultPreset.MinAudioVolume
            };
            Config.Presets.Add(newPreset);
            LoadPresets();
            PresetComboBox.SelectedIndex = Config.Presets.Count - 1;
        }

        private void DeletePreset_Click(object sender, RoutedEventArgs e)
        {
            if (PresetComboBox.SelectedIndex < 0 || Config.Presets.Count <= 1)
            {
                MessageBox.Show("至少需要保留一个组合。", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (MessageBox.Show("确定要删除当前组合吗？", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Config.Presets.RemoveAt(PresetComboBox.SelectedIndex);
                if (Config.SelectedPresetIndex >= Config.Presets.Count)
                {
                    Config.SelectedPresetIndex = Config.Presets.Count - 1;
                }
                LoadPresets();
                if (Config.Presets.Count > 0)
                {
                    PresetComboBox.SelectedIndex = Config.SelectedPresetIndex;
                }
            }
        }

        private void NameTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null)
            {
                currentPreset.Name = NameTextBox.Text;
                var index = PresetComboBox.SelectedIndex;
                LoadPresets();
                PresetComboBox.SelectedIndex = index;
            }
        }

        private void TargetDatePicker_SelectedDateChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (currentPreset != null && TargetDatePicker.SelectedDate.HasValue)
            {
                currentPreset.TargetDate = TargetDatePicker.SelectedDate.Value;
            }
        }

        private void CountdownDaysSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.CountdownDays = (int)CountdownDaysSlider.Value;
                CountdownDaysTextBox.Text = currentPreset.CountdownDays.ToString();
            }
        }

        private void CountdownDaysTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && int.TryParse(CountdownDaysTextBox.Text, out int value) && value >= 1 && value <= 30)
            {
                currentPreset.CountdownDays = value;
                CountdownDaysSlider.Value = value;
            }
        }

        private void BrowseCharacterImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
            };
            if (dialog.ShowDialog() == true && currentPreset != null)
            {
                currentPreset.CharacterImagePath = dialog.FileName;
                CharacterImagePathTextBox.Text = dialog.FileName;
                UpdatePreview();
            }
        }

        private void BrowseIceImage_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif|所有文件|*.*"
            };
            if (dialog.ShowDialog() == true && currentPreset != null)
            {
                currentPreset.IceImagePath = dialog.FileName;
                IceImagePathTextBox.Text = dialog.FileName;
                UpdatePreview();
            }
        }

        private void BrowseAudio_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "音频文件|*.mp3;*.wav;*.m4a;*.aac|所有文件|*.*"
            };
            if (dialog.ShowDialog() == true && currentPreset != null)
            {
                currentPreset.AudioPath = dialog.FileName;
                AudioPathTextBox.Text = dialog.FileName;
            }
        }

        private void CharacterOpacitySlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.CharacterOpacity = CharacterOpacitySlider.Value;
                CharacterOpacityTextBox.Text = currentPreset.CharacterOpacity.ToString("F2");
                UpdatePreview();
            }
        }

        private void CharacterOpacityTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(CharacterOpacityTextBox.Text, out double value) && value >= 0 && value <= 1)
            {
                currentPreset.CharacterOpacity = value;
                CharacterOpacitySlider.Value = value;
                UpdatePreview();
            }
        }

        private void IceOpacitySlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.IceOpacity = IceOpacitySlider.Value;
                IceOpacityTextBox.Text = currentPreset.IceOpacity.ToString("F2");
                UpdatePreview();
            }
        }

        private void IceOpacityTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(IceOpacityTextBox.Text, out double value) && value >= 0 && value <= 1)
            {
                currentPreset.IceOpacity = value;
                IceOpacitySlider.Value = value;
                UpdatePreview();
            }
        }

        private void MaxAudioBlurSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.MaxAudioBlur = MaxAudioBlurSlider.Value;
                MaxAudioBlurTextBox.Text = currentPreset.MaxAudioBlur.ToString("F2");
            }
        }

        private void CharacterImageScaleSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.CharacterImageScale = CharacterImageScaleSlider.Value;
                CharacterImageScaleTextBox.Text = currentPreset.CharacterImageScale.ToString("F2");
                UpdatePreview();
            }
        }

        private void CharacterImageScaleTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(CharacterImageScaleTextBox.Text, out double value) && value >= 0.1 && value <= 3.0)
            {
                currentPreset.CharacterImageScale = value;
                CharacterImageScaleSlider.Value = value;
                UpdatePreview();
            }
        }

        private void IceImageScaleSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.IceImageScale = IceImageScaleSlider.Value;
                IceImageScaleTextBox.Text = currentPreset.IceImageScale.ToString("F2");
                UpdatePreview();
            }
        }

        private void IceImageScaleTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(IceImageScaleTextBox.Text, out double value) && value >= 0.1 && value <= 3.0)
            {
                currentPreset.IceImageScale = value;
                IceImageScaleSlider.Value = value;
                UpdatePreview();
            }
        }

        private void CharacterOffsetXSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.CharacterOffsetX = CharacterOffsetXSlider.Value;
                CharacterOffsetXTextBox.Text = currentPreset.CharacterOffsetX.ToString("F0");
                UpdatePreview();
            }
        }

        private void CharacterOffsetXTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(CharacterOffsetXTextBox.Text, out double value) && value >= -200 && value <= 200)
            {
                currentPreset.CharacterOffsetX = value;
                CharacterOffsetXSlider.Value = value;
                UpdatePreview();
            }
        }

        private void CharacterOffsetYSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.CharacterOffsetY = CharacterOffsetYSlider.Value;
                CharacterOffsetYTextBox.Text = currentPreset.CharacterOffsetY.ToString("F0");
                UpdatePreview();
            }
        }

        private void CharacterOffsetYTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(CharacterOffsetYTextBox.Text, out double value) && value >= -200 && value <= 200)
            {
                currentPreset.CharacterOffsetY = value;
                CharacterOffsetYSlider.Value = value;
                UpdatePreview();
            }
        }

        private void IceOffsetXSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.IceOffsetX = IceOffsetXSlider.Value;
                IceOffsetXTextBox.Text = currentPreset.IceOffsetX.ToString("F0");
                UpdatePreview();
            }
        }

        private void IceOffsetXTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(IceOffsetXTextBox.Text, out double value) && value >= -200 && value <= 200)
            {
                currentPreset.IceOffsetX = value;
                IceOffsetXSlider.Value = value;
                UpdatePreview();
            }
        }

        private void IceOffsetYSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.IceOffsetY = IceOffsetYSlider.Value;
                IceOffsetYTextBox.Text = currentPreset.IceOffsetY.ToString("F0");
                UpdatePreview();
            }
        }

        private void IceOffsetYTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(IceOffsetYTextBox.Text, out double value) && value >= -200 && value <= 200)
            {
                currentPreset.IceOffsetY = value;
                IceOffsetYSlider.Value = value;
                UpdatePreview();
            }
        }

        private void MaxAudioBlurTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(MaxAudioBlurTextBox.Text, out double value) && value >= 0 && value <= 1)
            {
                currentPreset.MaxAudioBlur = value;
                MaxAudioBlurSlider.Value = value;
            }
        }

        private void PreviewAudio_Click(object sender, RoutedEventArgs e)
        {
            if (currentPreset != null && !string.IsNullOrEmpty(currentPreset.AudioPath) && 
                System.IO.File.Exists(currentPreset.AudioPath))
            {
                try
                {
                    // 使用当前设置的最大模糊值预览音频，音量使用最大值（1.0）以便清晰听到模糊效果
                    NeroUnfreeze.Services.AudioService.PlayAudioWithBlur(
                        currentPreset.AudioPath, 
                        currentPreset.MaxAudioBlur,  // 使用最大模糊值
                        1.0, // 使用最大音量以便清晰听到模糊效果
                        (errorMsg) => // 错误回调
                        {
                            // 在UI线程中显示错误消息
                            Dispatcher.Invoke(() =>
                            {
                                System.Windows.MessageBox.Show(errorMsg, "错误", 
                                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            });
                        });
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"预览音频时发生错误: {ex.Message}", "错误", 
                        System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                }
            }
            else
            {
                System.Windows.MessageBox.Show("请先选择音频文件。", "提示", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }

        private void MinAudioVolumeSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
        {
            if (currentPreset != null)
            {
                currentPreset.MinAudioVolume = MinAudioVolumeSlider.Value;
                MinAudioVolumeTextBox.Text = currentPreset.MinAudioVolume.ToString("F2");
            }
        }

        private void MinAudioVolumeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (currentPreset != null && double.TryParse(MinAudioVolumeTextBox.Text, out double value) && value >= 0 && value <= 1)
            {
                currentPreset.MinAudioVolume = value;
                MinAudioVolumeSlider.Value = value;
            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            // 保存当前预设的所有设置
            if (currentPreset != null)
            {
                currentPreset.Name = NameTextBox.Text;
                if (TargetDatePicker.SelectedDate.HasValue)
                {
                    currentPreset.TargetDate = TargetDatePicker.SelectedDate.Value;
                }
                currentPreset.CountdownDays = (int)CountdownDaysSlider.Value;
                currentPreset.CharacterImagePath = CharacterImagePathTextBox.Text;
                currentPreset.IceImagePath = IceImagePathTextBox.Text;
                currentPreset.AudioPath = AudioPathTextBox.Text;
                currentPreset.CharacterOpacity = CharacterOpacitySlider.Value;
                currentPreset.IceOpacity = IceOpacitySlider.Value;
                currentPreset.CharacterImageScale = CharacterImageScaleSlider.Value;
                currentPreset.IceImageScale = IceImageScaleSlider.Value;
                currentPreset.CharacterOffsetX = CharacterOffsetXSlider.Value;
                currentPreset.CharacterOffsetY = CharacterOffsetYSlider.Value;
                currentPreset.IceOffsetX = IceOffsetXSlider.Value;
                currentPreset.IceOffsetY = IceOffsetYSlider.Value;
                currentPreset.MaxAudioBlur = MaxAudioBlurSlider.Value;
                currentPreset.MinAudioVolume = MinAudioVolumeSlider.Value;
            }
            
            Config.AutoStart = AutoStartCheckBox.IsChecked ?? false;
            Config.PreventMinimizeOnWinD = PreventMinimizeOnWinDCheckBox.IsChecked ?? false;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

