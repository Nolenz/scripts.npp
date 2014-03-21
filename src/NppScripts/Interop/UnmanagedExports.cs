﻿using NppPlugin.DllExport;
using System;
using System.Runtime.InteropServices;

namespace NppScripts
{
    class UnmanagedExports
    {
        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static bool isUnicode()
        {
            return true;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void setInfo(NppData notepadPlusData)
        {
            Plugin.NppData = notepadPlusData;
            Bootstrapper.Init();
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getFuncsArray(ref int nbF)
        {
            nbF = Plugin.FuncItems.Items.Count;
            return Plugin.FuncItems.NativePointer;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static uint messageProc(uint Message, IntPtr wParam, IntPtr lParam)
        {
            return 1;
        }

        static IntPtr _ptrPluginName = IntPtr.Zero;

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static IntPtr getName()
        {
            if (_ptrPluginName == IntPtr.Zero)
                _ptrPluginName = Marshal.StringToHGlobalUni(Plugin.PluginName);
            return _ptrPluginName;
        }

        [DllExport(CallingConvention = CallingConvention.Cdecl)]
        static void beNotified(IntPtr notifyCode)
        {
            try
            {
                SCNotification nc = (SCNotification)Marshal.PtrToStructure(notifyCode, typeof(SCNotification));
                if (nc.nmhdr.code == (uint)NppMsg.NPPN_TBMODIFICATION)
                {
                    Plugin.FuncItems.RefreshItems();
                    Plugin.RefreshToolbarImages();
                }
                else if (nc.nmhdr.code == (uint)SciMsg.SCN_CHARADDED)
                {
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_READY)
                {
                    CSScriptIntegration.Initialize();
                    Plugin.InitView();
                }
                else if (nc.nmhdr.code == (uint)NppMsg.NPPN_SHUTDOWN)
                {
                    Marshal.FreeHGlobal(_ptrPluginName);
                    Plugin.CleanUp();
                }

                Plugin.OnNotification(nc);
            }
            catch { }//this is indeed the last line of defense as all CS-S calls have the error handling inside 
        }
    }
}