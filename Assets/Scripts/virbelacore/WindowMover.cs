using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WindowMover {
    #region DLL Imports
    private const string UnityWindowClassName = "UnityWndClass";

    [DllImport("kernel32.dll")]
    static extern uint GetCurrentThreadId();

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    static extern int GetClassName(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll", EntryPoint = "SetWindowPos")]
    public static extern IntPtr SetWindowPos(IntPtr hWnd, int hWndInsertAfter, int x, int Y, int cx, int cy, int wFlags);

    public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpEnumFunc, IntPtr lParam);
    #endregion

    #region Private fields
    private static IntPtr windowHandle = IntPtr.Zero;
    #endregion

    static bool Initialized { get { return windowHandle != IntPtr.Zero; } }

    static void Init()
    {
        uint threadId = GetCurrentThreadId();
        EnumThreadWindows(threadId, (hWnd, lParam) =>
        {
            var classText = new StringBuilder(UnityWindowClassName.Length + 1);
            GetClassName(hWnd, classText, classText.Capacity);
            if (classText.ToString() == UnityWindowClassName)
            {
                windowHandle = hWnd;
                return false;
            }
            return true;
        }, IntPtr.Zero);

    }

    public static void Move(int x, int y)
    {
        if (!Initialized)
            Init();
        SetWindowPos(windowHandle, 0, x, y, 500, 500, 0x0001);
    }

    public static void Resize(int w, int h)
    {
        if (!Initialized)
            Init();
        SetWindowPos(windowHandle, 0, 0, 0, w, h, 0x0002);
    }

    public static void MoveToTopCenter()
    {
        Move(Screen.currentResolution.width / 2 - Screen.width / 2, 0);
    }
}
