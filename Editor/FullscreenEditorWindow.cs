#if UNITY_EDITOR_WIN
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEditor;
using UnityEngine;

public class FullscreenEditorWindow
{
    #region Windows API

    // Window constants
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint SWP_FRAMECHANGED = 0x0020;
    const int GWL_STYLE = -16;
    const int WS_CAPTION = 0x00C00000;
    const int WS_THICKFRAME = 0x00040000;
    const int WS_MINIMIZEBOX = 0x00020000;
    const int WS_MAXIMIZEBOX = 0x00010000;
    const int WS_SYSMENU = 0x00080000;

    // SystemMetrics constants
    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;

    struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    #endregion

    const string PREFS_KEY = "UnityFullscreenState";

    static bool isFullscreen;
    static int originalStyle;
    static RECT originalRect;

    [Serializable]
    struct EditorState
    {
        public bool isFullscreen;
        public int originalStyle;
        public int rectLeft;
        public int rectTop;
        public int rectRight;
        public int rectBottom;
    }

    // Will be reset when the editor is played or stopped
    static bool needLoadState = true;


    // https://docs.unity3d.com/ScriptReference/MenuItem.html
    [MenuItem("Window/Toggle Fullscreen _F11", false, -1001)]
    public static void ToggleFullscreen()
    {
        if (needLoadState)
            LoadState();

        IntPtr hWnd = GetMainWindowHandle();

        if (hWnd == IntPtr.Zero)
        {
            UnityEngine.Debug.LogError("Main Unity window not found!");
            return;
        }

        if (!isFullscreen)
        {
            GetWindowRect(hWnd, out originalRect);
            originalStyle = GetWindowLong(hWnd, GWL_STYLE);

            SetWindowLong(hWnd, GWL_STYLE,
                originalStyle & ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX | WS_SYSMENU));

            int width = GetSystemMetrics(SM_CXSCREEN);
            int height = GetSystemMetrics(SM_CYSCREEN);

            SetWindowPos(hWnd, IntPtr.Zero, 0, 0, width, height, SWP_SHOWWINDOW | SWP_FRAMECHANGED);

            isFullscreen = true;
        }
        else
        {
            SetWindowLong(hWnd, GWL_STYLE, originalStyle);

            int x = originalRect.Left;
            int y = originalRect.Top;
            int width = originalRect.Right - originalRect.Left;
            int height = originalRect.Bottom - originalRect.Top;

            SetWindowPos(hWnd, IntPtr.Zero, x, y, width, height, SWP_SHOWWINDOW | SWP_FRAMECHANGED);

            isFullscreen = false;
        }

        SaveState();
    }

    static IntPtr GetMainWindowHandle() =>
        Process.GetCurrentProcess().MainWindowHandle;

    static void SaveState()
    {
        EditorState state = new()
        {
            isFullscreen = isFullscreen,
            originalStyle = originalStyle,
            rectLeft = originalRect.Left,
            rectTop = originalRect.Top,
            rectRight = originalRect.Right,
            rectBottom = originalRect.Bottom
        };

        EditorPrefs.SetString(PREFS_KEY, JsonUtility.ToJson(state));
    }

    static void LoadState()
    {
        string json = EditorPrefs.GetString(PREFS_KEY, string.Empty);

        if (!string.IsNullOrEmpty(json))
        {
            EditorState state = JsonUtility.FromJson<EditorState>(json);

            isFullscreen = state.isFullscreen;
            originalStyle = state.originalStyle;
            originalRect = new RECT
            {
                Left = state.rectLeft,
                Top = state.rectTop,
                Right = state.rectRight,
                Bottom = state.rectBottom
            };
        }
    }
}
#endif