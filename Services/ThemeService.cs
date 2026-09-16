using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;

namespace AgyToolbox.Services;

public enum AppThemeMode
{
    Light,
    Dark,
    System
}

public class ThemeService
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1 = 19;
    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public AppThemeMode CurrentMode { get; private set; } = AppThemeMode.System;
    public bool IsDarkActive { get; private set; }

    public event Action<bool>? ThemeChanged;

    private Window? _trackedWindow;

    public void Initialize(Window mainWindow)
    {
        _trackedWindow = mainWindow;

        // 监听 Windows 系统的深色/浅色偏好变更
        SystemEvents.UserPreferenceChanged += (s, e) =>
        {
            if (e.Category == UserPreferenceCategory.General && CurrentMode == AppThemeMode.System)
            {
                mainWindow.Dispatcher.Invoke(() => ApplyTheme(AppThemeMode.System));
            }
        };

        // 窗口 Handle 创建或加载完成后应用 DWM 沉浸式暗黑标题栏
        mainWindow.SourceInitialized += (s, e) =>
        {
            ApplyTheme(CurrentMode);
        };

        // 初始应用
        ApplyTheme(AppThemeMode.System);
    }

    public void ApplyTheme(AppThemeMode mode)
    {
        CurrentMode = mode;
        bool isDark = mode switch
        {
            AppThemeMode.Dark => true,
            AppThemeMode.Light => false,
            AppThemeMode.System => IsWindowsSystemInDarkMode(),
            _ => false
        };

        IsDarkActive = isDark;

        // 1. 更新全局 DynamicResource 调色板
        UpdatePaletteResources(isDark);

        // 2. 更新 Windows 沉浸式标题栏属性
        if (_trackedWindow != null)
        {
            UpdateImmersiveTitleBar(_trackedWindow, isDark);
        }

        ThemeChanged?.Invoke(isDark);
    }

    public static bool IsWindowsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            var val = key?.GetValue("AppsUseLightTheme");
            if (val is int intVal)
            {
                return intVal == 0; // 0 为暗黑模式，1 为亮色模式
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"读取系统暗黑模式注册表异常: {ex.Message}");
        }
        return false;
    }

    private static void UpdateImmersiveTitleBar(Window window, bool isDark)
    {
        try
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero) return;

            int useDarkMode = isDark ? 1 : 0;
            if (DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref useDarkMode, sizeof(int)) != 0)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE_BEFORE_20H1, ref useDarkMode, sizeof(int));
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"设置 DWM 沉浸式标题栏失败: {ex.Message}");
        }
    }

    private static void UpdatePaletteResources(bool isDark)
    {
        var res = Application.Current?.Resources;
        if (res == null) return;

        if (isDark)
        {
            // ── 深色模式 (护眼炭黑 / Slate 暗夜蓝) ──
            res["BrushWindowBg"] = new SolidColorBrush(Color.FromRgb(0x0B, 0x0F, 0x19));
            res["BrushCardBg"] = new SolidColorBrush(Color.FromRgb(0x13, 0x1B, 0x2E));
            res["BrushCardBgAlt"] = new SolidColorBrush(Color.FromRgb(0x18, 0x22, 0x38));
            res["BrushCardBorder"] = new SolidColorBrush(Color.FromRgb(0x23, 0x2F, 0x48));
            res["BrushSidebarBg"] = new SolidColorBrush(Color.FromRgb(0x0D, 0x14, 0x24));
            res["BrushSidebarBorder"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x24, 0x3B));
            res["BrushSidebarItemHover"] = new SolidColorBrush(Color.FromRgb(0x1A, 0x24, 0x3B));
            res["BrushSidebarItemSelected"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x8A));
            res["BrushSidebarItemSelectedText"] = new SolidColorBrush(Color.FromRgb(0x38, 0xBD, 0xF8));

            res["BrushTextPrimary"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            res["BrushTextSecondary"] = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));
            res["BrushTextMuted"] = new SolidColorBrush(Color.FromRgb(0x64, 0x74, 0x8B));

            res["BrushSubTabBg"] = new SolidColorBrush(Color.FromRgb(0x0D, 0x14, 0x24));
            res["BrushSubTabSelectedBg"] = new SolidColorBrush(Color.FromRgb(0x13, 0x1B, 0x2E));
            res["BrushSubTabHoverBg"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));

            res["BrushInputBg"] = new SolidColorBrush(Color.FromRgb(0x0D, 0x14, 0x24));
            res["BrushInputBorder"] = new SolidColorBrush(Color.FromRgb(0x2A, 0x38, 0x54));

            res["BrushSecondaryBtnBg"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            res["BrushSecondaryBtnHover"] = new SolidColorBrush(Color.FromRgb(0x33, 0x41, 0x55));
            res["BrushSecondaryBtnText"] = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));

            res["BrushCodeBg"] = new SolidColorBrush(Color.FromRgb(0x0B, 0x0F, 0x19));
            res["BrushCodeBorder"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            res["BrushTableBorder"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));
            res["BrushTableHeaderBg"] = new SolidColorBrush(Color.FromRgb(0x0D, 0x14, 0x24));

            // ── 语义警示与高亮背景 (深色护眼) ──
            res["BrushAlertRedBg"] = new SolidColorBrush(Color.FromRgb(0x3B, 0x12, 0x19));
            res["BrushAlertRedBorder"] = new SolidColorBrush(Color.FromRgb(0x7F, 0x1D, 0x1D));
            res["BrushAlertRedText"] = new SolidColorBrush(Color.FromRgb(0xFC, 0xA5, 0xA5));

            res["BrushAlertBlueBg"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x22, 0x47));
            res["BrushAlertBlueBorder"] = new SolidColorBrush(Color.FromRgb(0x1D, 0x4E, 0xD8));
            res["BrushAlertBlueText"] = new SolidColorBrush(Color.FromRgb(0x93, 0xC5, 0xFD));

            res["BrushAlertYellowBg"] = new SolidColorBrush(Color.FromRgb(0x3B, 0x2E, 0x0B));
            res["BrushAlertYellowBorder"] = new SolidColorBrush(Color.FromRgb(0xA1, 0x62, 0x07));
            res["BrushAlertYellowText"] = new SolidColorBrush(Color.FromRgb(0xFD, 0xE0, 0x47));

            res["BrushAlertGreenBg"] = new SolidColorBrush(Color.FromRgb(0x0B, 0x2F, 0x18));
            res["BrushAlertGreenBorder"] = new SolidColorBrush(Color.FromRgb(0x15, 0x80, 0x3D));
            res["BrushAlertGreenText"] = new SolidColorBrush(Color.FromRgb(0x86, 0xEF, 0xAC));

            res["BrushAlertPurpleBg"] = new SolidColorBrush(Color.FromRgb(0x26, 0x12, 0x3D));
            res["BrushAlertPurpleBorder"] = new SolidColorBrush(Color.FromRgb(0x7E, 0x22, 0xCE));
            res["BrushAlertPurpleText"] = new SolidColorBrush(Color.FromRgb(0xD8, 0xB4, 0xFE));
        }
        else
        {
            // ── 浅色模式 (现代 Slate 浅色清爽) ──
            res["BrushWindowBg"] = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
            res["BrushCardBg"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            res["BrushCardBgAlt"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            res["BrushCardBorder"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            res["BrushSidebarBg"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            res["BrushSidebarBorder"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            res["BrushSidebarItemHover"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            res["BrushSidebarItemSelected"] = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF));
            res["BrushSidebarItemSelectedText"] = new SolidColorBrush(Color.FromRgb(0x02, 0x84, 0xC7));

            res["BrushTextPrimary"] = new SolidColorBrush(Color.FromRgb(0x0F, 0x17, 0x2A));
            res["BrushTextSecondary"] = new SolidColorBrush(Color.FromRgb(0x47, 0x55, 0x69));
            res["BrushTextMuted"] = new SolidColorBrush(Color.FromRgb(0x94, 0xA3, 0xB8));

            res["BrushSubTabBg"] = new SolidColorBrush(Color.FromRgb(0xF1, 0xF5, 0xF9));
            res["BrushSubTabSelectedBg"] = new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF));
            res["BrushSubTabHoverBg"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));

            res["BrushInputBg"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            res["BrushInputBorder"] = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));

            res["BrushSecondaryBtnBg"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            res["BrushSecondaryBtnHover"] = new SolidColorBrush(Color.FromRgb(0xCB, 0xD5, 0xE1));
            res["BrushSecondaryBtnText"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x29, 0x3B));

            res["BrushCodeBg"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));
            res["BrushCodeBorder"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            res["BrushTableBorder"] = new SolidColorBrush(Color.FromRgb(0xE2, 0xE8, 0xF0));
            res["BrushTableHeaderBg"] = new SolidColorBrush(Color.FromRgb(0xF8, 0xFA, 0xFC));

            // ── 语义警示与高亮背景 (浅色清爽) ──
            res["BrushAlertRedBg"] = new SolidColorBrush(Color.FromRgb(0xFE, 0xF2, 0xF2));
            res["BrushAlertRedBorder"] = new SolidColorBrush(Color.FromRgb(0xFE, 0xCA, 0xCA));
            res["BrushAlertRedText"] = new SolidColorBrush(Color.FromRgb(0x99, 0x1B, 0x1B));

            res["BrushAlertBlueBg"] = new SolidColorBrush(Color.FromRgb(0xEF, 0xF6, 0xFF));
            res["BrushAlertBlueBorder"] = new SolidColorBrush(Color.FromRgb(0xBF, 0xDB, 0xFE));
            res["BrushAlertBlueText"] = new SolidColorBrush(Color.FromRgb(0x1E, 0x40, 0xAF));

            res["BrushAlertYellowBg"] = new SolidColorBrush(Color.FromRgb(0xFE, 0xFC, 0xE8));
            res["BrushAlertYellowBorder"] = new SolidColorBrush(Color.FromRgb(0xFE, 0xF0, 0x8A));
            res["BrushAlertYellowText"] = new SolidColorBrush(Color.FromRgb(0x85, 0x4D, 0x0E));

            res["BrushAlertGreenBg"] = new SolidColorBrush(Color.FromRgb(0xF0, 0xFD, 0xF4));
            res["BrushAlertGreenBorder"] = new SolidColorBrush(Color.FromRgb(0xBB, 0xF7, 0xD0));
            res["BrushAlertGreenText"] = new SolidColorBrush(Color.FromRgb(0x16, 0x65, 0x34));

            res["BrushAlertPurpleBg"] = new SolidColorBrush(Color.FromRgb(0xFA, 0xF5, 0xFF));
            res["BrushAlertPurpleBorder"] = new SolidColorBrush(Color.FromRgb(0xE9, 0xD5, 0xFF));
            res["BrushAlertPurpleText"] = new SolidColorBrush(Color.FromRgb(0x6B, 0x21, 0xA8));
        }
    }
}
