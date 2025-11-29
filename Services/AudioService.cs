using System;
using System.IO;
using System.Threading.Tasks;
using NAudio.Wave;
using NAudio.Dsp;

namespace NeroUnfreeze.Services
{
    /// <summary>
    /// 音频服务 - 处理音频播放、模糊效果和音量控制
    /// </summary>
    public class AudioService
    {
        /// <summary>
        /// 播放带模糊效果的音频
        /// </summary>
        /// <param name="audioPath">音频文件路径</param>
        /// <param name="blurAmount">模糊程度（0.0-1.0）</param>
        /// <param name="volume">音量（0.0-1.0）</param>
        /// <param name="onError">错误回调函数</param>
        public static void PlayAudioWithBlur(string audioPath, double blurAmount, double volume = 1.0, Action<string>? onError = null)
        {
            if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
            {
                onError?.Invoke("音频文件不存在或路径为空。");
                return;
            }

            // 异步播放，不阻塞UI
            Task.Run(() =>
            {
                WaveOutEvent? waveOut = null;
                AudioFileReader? audioFile = null;
                try
                {
                    audioFile = new AudioFileReader(audioPath);
                    
                    // 转换为32位浮点格式以便处理
                    var sampleProvider = audioFile.ToSampleProvider();
                    
                    // 创建低通滤波器来模拟模糊效果
                    // 增强模糊：使用更低的截止频率，并应用多次滤波
                    var sampleRate = audioFile.WaveFormat.SampleRate;
                    var maxCutoff = sampleRate * 0.5f;
                    // 使用平方函数使模糊效果更明显
                    var blurFactor = blurAmount * blurAmount;
                    var cutoffFrequency = (float)(maxCutoff * (1.0 - blurFactor) * 0.3); // 降低到30%以增强模糊
                    cutoffFrequency = Math.Max(50, Math.Min(cutoffFrequency, maxCutoff));

                    // 应用低通滤波器（应用两次以增强模糊效果）
                    var filteredProvider = new BiQuadFilterProvider(sampleProvider, cutoffFrequency, sampleRate, BiQuadFilterProvider.FilterType.LowPass);
                    // 如果需要更强的模糊，可以再次应用
                    if (blurAmount > 0.5)
                    {
                        filteredProvider = new BiQuadFilterProvider(filteredProvider, cutoffFrequency * 0.8f, sampleRate, BiQuadFilterProvider.FilterType.LowPass);
                    }
                    
                    // 应用音量控制
                    var volumeProvider = new VolumeSampleProvider(filteredProvider, (float)volume);
                    
                    // 转换回16位PCM格式
                    var pcmProvider = volumeProvider.ToWaveProvider16();
                    
                    waveOut = new WaveOutEvent();
                    waveOut.Init(pcmProvider);
                    waveOut.Play();
                    
                    // 等待播放完成
                    while (waveOut.PlaybackState == PlaybackState.Playing)
                    {
                        System.Threading.Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    // 通过回调处理错误
                    var errorMsg = $"播放音频失败: {ex.Message}";
                    onError?.Invoke(errorMsg);
                }
                finally
                {
                    // 确保资源被释放
                    if (waveOut != null)
                    {
                        try
                        {
                            if (waveOut.PlaybackState != PlaybackState.Stopped)
                            {
                                waveOut.Stop();
                            }
                            waveOut.Dispose();
                        }
                        catch
                        {
                            // 忽略释放时的错误
                        }
                    }
                    
                    if (audioFile != null)
                    {
                        try
                        {
                            audioFile.Dispose();
                        }
                        catch
                        {
                            // 忽略释放时的错误
                        }
                    }
                }
            });
        }
    }

    /// <summary>
    /// 双二阶滤波器提供者 - 用于实现音频低通滤波（模糊效果）
    /// </summary>
    public class BiQuadFilterProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private readonly NAudio.Dsp.BiQuadFilter leftFilter;
        private readonly NAudio.Dsp.BiQuadFilter rightFilter;

        public enum FilterType
        {
            LowPass,
            HighPass,
            BandPass
        }

        public BiQuadFilterProvider(ISampleProvider source, float cutoff, float sampleRate, FilterType filterType)
        {
            this.source = source;
            var q = 1.0f;
            
            switch (filterType)
            {
                case FilterType.LowPass:
                    leftFilter = NAudio.Dsp.BiQuadFilter.LowPassFilter(sampleRate, cutoff, q);
                    rightFilter = NAudio.Dsp.BiQuadFilter.LowPassFilter(sampleRate, cutoff, q);
                    break;
                case FilterType.HighPass:
                    leftFilter = NAudio.Dsp.BiQuadFilter.HighPassFilter(sampleRate, cutoff, q);
                    rightFilter = NAudio.Dsp.BiQuadFilter.HighPassFilter(sampleRate, cutoff, q);
                    break;
                case FilterType.BandPass:
                    leftFilter = NAudio.Dsp.BiQuadFilter.BandPassFilterConstantSkirtGain(sampleRate, cutoff, q);
                    rightFilter = NAudio.Dsp.BiQuadFilter.BandPassFilterConstantSkirtGain(sampleRate, cutoff, q);
                    break;
                default:
                    // 默认使用低通滤波器
                    leftFilter = NAudio.Dsp.BiQuadFilter.LowPassFilter(sampleRate, cutoff, q);
                    rightFilter = NAudio.Dsp.BiQuadFilter.LowPassFilter(sampleRate, cutoff, q);
                    break;
            }
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);
            var channels = WaveFormat.Channels;

            for (int i = 0; i < samplesRead; i += channels)
            {
                if (channels == 1)
                {
                    buffer[offset + i] = leftFilter.Transform(buffer[offset + i]);
                }
                else if (channels == 2)
                {
                    buffer[offset + i] = leftFilter.Transform(buffer[offset + i]);
                    buffer[offset + i + 1] = rightFilter.Transform(buffer[offset + i + 1]);
                }
            }

            return samplesRead;
        }
    }

    /// <summary>
    /// 音量控制提供者 - 用于调整音频播放音量
    /// </summary>
    public class VolumeSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider source;
        private float volume;

        public VolumeSampleProvider(ISampleProvider source, float volume)
        {
            this.source = source;
            this.volume = Math.Max(0.0f, Math.Min(1.0f, volume));
        }

        public WaveFormat WaveFormat => source.WaveFormat;

        public int Read(float[] buffer, int offset, int count)
        {
            var samplesRead = source.Read(buffer, offset, count);
            for (int i = 0; i < samplesRead; i++)
            {
                buffer[offset + i] *= volume;
            }
            return samplesRead;
        }
    }
}

