using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Drawing;

namespace NeroUnfreeze.Windows
{
    /// <summary>
    /// 图片显示窗口 - 在桌面右下角显示角色和冰块图片，支持透明度和位置调整
    /// </summary>
    public partial class ImageDisplayWindow : Window
    {
        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        const int GWL_EXSTYLE = -20;
        const uint WS_EX_LAYERED = 0x80000;
        const uint WS_EX_TRANSPARENT = 0x20;
        const uint WS_EX_NOACTIVATE = 0x08000000;
        const uint WS_EX_TOOLWINDOW = 0x00000080;
        static readonly IntPtr HWND_BOTTOM = new IntPtr(1);
        const uint SWP_NOMOVE = 0x0002;
        const uint SWP_NOSIZE = 0x0001;
        const uint SWP_NOACTIVATE = 0x0010;
        const uint SWP_SHOWWINDOW = 0x0040;
        const uint WM_SYSCOMMAND = 0x0112;
        const uint SC_MINIMIZE = 0xF020;

        private bool preventMinimize = false;
        private System.Windows.Interop.HwndSource? hwndSource;

        public ImageDisplayWindow()
        {
            InitializeComponent();
            Loaded += ImageDisplayWindow_Loaded;
            SourceInitialized += ImageDisplayWindow_SourceInitialized;
        }

        /// <summary>
        /// 设置是否防止Win+D时窗口被最小化
        /// </summary>
        public void SetPreventMinimize(bool prevent)
        {
            preventMinimize = prevent;
        }

        /// <summary>
        /// 窗口源初始化时，添加消息钩子以拦截最小化消息
        /// </summary>
        private void ImageDisplayWindow_SourceInitialized(object? sender, EventArgs e)
        {
            hwndSource = (System.Windows.Interop.HwndSource)PresentationSource.FromVisual(this);
            if (hwndSource != null)
            {
                hwndSource.AddHook(WndProc);
            }
        }

        /// <summary>
        /// 窗口消息处理函数，用于拦截最小化消息
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (preventMinimize && msg == WM_SYSCOMMAND && (wParam.ToInt32() & 0xFFF0) == SC_MINIMIZE)
            {
                handled = true;
                return IntPtr.Zero;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// 窗口加载时，设置窗口属性并定位到右下角
        /// </summary>
        private void ImageDisplayWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdatePosition();

            // 设置窗口属性：可穿透点击、不激活、工具窗口、置于最下层
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                var extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                extendedStyle |= (int)(WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
                SetWindowLong(hwnd, GWL_EXSTYLE, (uint)extendedStyle);
                
                // 将窗口置于最下层
                SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, 
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
            }
        }

        /// <summary>
        /// 更新窗口位置到屏幕右下角
        /// </summary>
        private void UpdatePosition()
        {
            var workingArea = SystemParameters.WorkArea;
            Left = workingArea.Right - ActualWidth - 20;
            Top = workingArea.Bottom - ActualHeight - 20;
        }

        /// <summary>
        /// 更新显示的图片，包括透明度、缩放和位置
        /// </summary>
        public void UpdateImages(string characterPath, string icePath, 
            double characterOpacity, double iceOpacity, 
            double characterScale, double iceScale,
            double characterOffsetX, double characterOffsetY,
            double iceOffsetX, double iceOffsetY)
        {
            try
            {
                var canvas = (System.Windows.Controls.Canvas)Content;
                var baseX = 0.0;
                var baseY = 0.0;
                var maxWidth = 0.0;
                var maxHeight = 0.0;

                // 加载角色图片
                if (!string.IsNullOrEmpty(characterPath) && System.IO.File.Exists(characterPath))
                {
                    // 确保路径是绝对路径，并转换为URI格式
                    var absolutePath = Path.IsPathRooted(characterPath) 
                        ? characterPath 
                        : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, characterPath));
                    var characterBitmap = new BitmapImage(new Uri(absolutePath, UriKind.Absolute));
                    CharacterImage.Source = characterBitmap;
                    CharacterImage.Opacity = characterOpacity;
                    var charWidth = characterBitmap.PixelWidth * characterScale;
                    var charHeight = characterBitmap.PixelHeight * characterScale;
                    CharacterImage.Width = charWidth;
                    CharacterImage.Height = charHeight;
                    
                    // 设置位置
                    System.Windows.Controls.Canvas.SetLeft(CharacterImage, baseX + characterOffsetX);
                    System.Windows.Controls.Canvas.SetTop(CharacterImage, baseY + characterOffsetY);
                    
                    maxWidth = Math.Max(maxWidth, baseX + characterOffsetX + charWidth);
                    maxHeight = Math.Max(maxHeight, baseY + characterOffsetY + charHeight);
                }

                // 加载冰块图片
                if (!string.IsNullOrEmpty(icePath) && System.IO.File.Exists(icePath))
                {
                    // 确保路径是绝对路径，并转换为URI格式
                    var absolutePath = Path.IsPathRooted(icePath) 
                        ? icePath 
                        : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, icePath));
                    var iceBitmap = new BitmapImage(new Uri(absolutePath, UriKind.Absolute));
                    IceImage.Source = iceBitmap;
                    IceImage.Opacity = iceOpacity;
                    var iceWidth = iceBitmap.PixelWidth * iceScale;
                    var iceHeight = iceBitmap.PixelHeight * iceScale;
                    IceImage.Width = iceWidth;
                    IceImage.Height = iceHeight;
                    
                    // 设置位置
                    System.Windows.Controls.Canvas.SetLeft(IceImage, baseX + iceOffsetX);
                    System.Windows.Controls.Canvas.SetTop(IceImage, baseY + iceOffsetY);
                    
                    maxWidth = Math.Max(maxWidth, baseX + iceOffsetX + iceWidth);
                    maxHeight = Math.Max(maxHeight, baseY + iceOffsetY + iceHeight);
                }

                // 更新窗口大小和位置
                Width = Math.Max(300, maxWidth + 40); // 留出边距
                Height = Math.Max(300, maxHeight + 40);
                UpdatePosition();

                // 确保窗口始终在最下层
                var hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    SetWindowPos(hwnd, HWND_BOTTOM, 0, 0, 0, 0, 
                        SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_SHOWWINDOW);
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"加载图片失败: {ex.Message}", "错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
        }
    }
}

