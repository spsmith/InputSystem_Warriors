using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MachineNameAsWindowName : MonoBehaviour
{
    [DllImport("user32.dll", EntryPoint = "SetWindowText")]
    public static extern bool SetWindowText(System.IntPtr hwnd, System.String lpString);
    [DllImport("user32.dll", EntryPoint = "GetActiveWindow")]
    public static extern System.IntPtr GetActiveWindow();

    [SerializeField]
    string WindowName = string.Empty;

    void Start()
    {
        //https://answers.unity.com/questions/148723/how-can-i-change-the-title-of-the-standalone-playe.html
        //get handle for the active window
        System.IntPtr windowPtr = GetActiveWindow();

        //set window title to the current machine name
        SetWindowText(windowPtr, WindowName + " | " + UniCAVE.Util.GetMachineName());
    }
}
