using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NOVR.VrUi;

internal static class WindowsCursorClip
{
    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    private static extern bool ClipCursor(IntPtr lpRect);

    [DllImport("user32.dll", EntryPoint = "ClipCursor")]
    private static extern bool ClipCursorRect(ref RECT lpRect);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    public static void ConfineToGameWindow()
    {
        try
        {
            var hwnd = GetGameWindow();
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            if (GetClientRect(hwnd, out var client) && client.Right - client.Left > 8 && client.Bottom - client.Top > 8)
            {
                var topLeft = new POINT { X = client.Left, Y = client.Top };
                var bottomRight = new POINT { X = client.Right, Y = client.Bottom };
                if (ClientToScreen(hwnd, ref topLeft) && ClientToScreen(hwnd, ref bottomRight))
                {
                    var screen = new RECT
                    {
                        Left = topLeft.X,
                        Top = topLeft.Y,
                        Right = bottomRight.X,
                        Bottom = bottomRight.Y
                    };
                    ClipCursorRect(ref screen);
                    return;
                }
            }

            if (GetWindowRect(hwnd, out var window))
            {
                ClipCursorRect(ref window);
            }
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[NOVR] ClipCursor failed: {exception.Message}");
        }
    }

    public static void Release()
    {
        try
        {
            ClipCursor(IntPtr.Zero);
        }
        catch (Exception exception)
        {
            UnityEngine.Debug.LogWarning($"[NOVR] ClipCursor release failed: {exception.Message}");
        }
    }

    private static IntPtr GetGameWindow()
    {
        var processId = (uint)Process.GetCurrentProcess().Id;
        var foreground = GetForegroundWindow();
        if (foreground != IntPtr.Zero)
        {
            GetWindowThreadProcessId(foreground, out var foregroundPid);
            if (foregroundPid == processId)
            {
                return foreground;
            }
        }

        return Process.GetCurrentProcess().MainWindowHandle;
    }
}
