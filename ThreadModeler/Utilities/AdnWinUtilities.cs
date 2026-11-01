////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Copyright (c) Autodesk, Inc. All rights reserved 
// Written by Jan Liska & Philippe Leefsma 2011 - ADN/Developer Technical Services
//
// Permission to use, copy, modify, and distribute this software in
// object code form for any purpose and without fee is hereby granted, 
// provided that the above copyright notice appears in all copies and 
// that both that copyright notice and the limited warranty and
// restricted rights notice below appear in all supporting 
// documentation.
//
// AUTODESK PROVIDES THIS PROGRAM "AS IS" AND WITH ALL FAULTS. 
// AUTODESK SPECIFICALLY DISCLAIMS ANY IMPLIED WARRANTY OF
// MERCHANTABILITY OR FITNESS FOR A PARTICULAR USE.  AUTODESK, INC. 
// DOES NOT WARRANT THAT THE OPERATION OF THE PROGRAM WILL BE
// UNINTERRUPTED OR ERROR FREE.
////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ThreadModeler.Utilities
{
    /////////////////////////////////////////////////////////////
    // Use: a small utility class to create child dialogs
    //
    /////////////////////////////////////////////////////////////
    public class WindowWrapper : System.Windows.Forms.IWin32Window
    {
        private IntPtr mHwnd;

        public WindowWrapper(IntPtr handle)
        {
            mHwnd = handle;
        }

        public IntPtr Handle
        {
            get
            {
                return mHwnd;
            }
        }
    }

    //////////////////////////////////////////////////////////////////////////////////////////////
    // Description:
    //
    //////////////////////////////////////////////////////////////////////////////////////////////
    public sealed class PictureDispConverter
    {
        [DllImport("OleAut32.dll",
            EntryPoint = "OleCreatePictureIndirect",
            ExactSpelling = true,
            PreserveSig = false)]
        private static extern stdole.IPictureDisp
            OleCreatePictureIndirect(
                [MarshalAs(UnmanagedType.AsAny)] object picdesc,
                ref Guid iid,
                [MarshalAs(UnmanagedType.Bool)] bool fOwn);

        static Guid iPictureDispGuid = typeof(stdole.IPictureDisp).GUID;

        private static class PICTDESC
        {
            //Picture Types
            public const short PICTYPE_UNINITIALIZED = -1;
            public const short PICTYPE_NONE = 0;
            public const short PICTYPE_BITMAP = 1;
            public const short PICTYPE_METAFILE = 2;
            public const short PICTYPE_ICON = 3;
            public const short PICTYPE_ENHMETAFILE = 4;

            [StructLayout(LayoutKind.Sequential)]
            public class Icon
            {
                internal int cbSizeOfStruct = Marshal.SizeOf(typeof(PICTDESC.Icon));
                internal int picType = PICTDESC.PICTYPE_ICON;
                internal IntPtr hicon = IntPtr.Zero;
                internal int unused1;
                internal int unused2;

                internal Icon(System.Drawing.Icon icon)
                {
                    this.hicon = icon.ToBitmap().GetHicon();
                }
            }

            [StructLayout(LayoutKind.Sequential)]
            public class Bitmap
            {
                internal int cbSizeOfStruct = Marshal.SizeOf(typeof(PICTDESC.Bitmap));
                internal int picType = PICTDESC.PICTYPE_BITMAP;
                internal IntPtr hbitmap = IntPtr.Zero;
                internal IntPtr hpal = IntPtr.Zero;
                internal int unused;

                internal Bitmap(System.Drawing.Bitmap bitmap)
                {
                    this.hbitmap = bitmap.GetHbitmap();
                }
            }
        }

        public static stdole.IPictureDisp ToIPictureDisp(System.Drawing.Icon icon)
        {
            PICTDESC.Icon pictIcon = new PICTDESC.Icon(icon);

            return OleCreatePictureIndirect(pictIcon, ref iPictureDispGuid, true);
        }

        public static stdole.IPictureDisp ToIPictureDisp(System.Drawing.Bitmap bmp)
        {
            PICTDESC.Bitmap pictBmp = new PICTDESC.Bitmap(bmp);

            return OleCreatePictureIndirect(pictBmp, ref iPictureDispGuid, true);
        }
    }

    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Use: 
    //
    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    public class AdnTimer
    {
        DateTime _previous;

        public AdnTimer()
        {
            _previous = DateTime.Now;
        }

        public double ElapsedSeconds
        {
            get
            {
                DateTime Now = DateTime.Now;
                TimeSpan Elaspsed = Now.Subtract(_previous);

                _previous = Now;

                return Elaspsed.TotalSeconds;
            }
        }
    }
}


