using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Image2Item
{ 
    public class OpenDialog
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class OpenFileName
        {
            public int lStructSize;
            public IntPtr hwndOwner = IntPtr.Zero;
            public IntPtr hInstance = IntPtr.Zero;
            public string lpstrFilter;
            public string lpstrCustomFilter;
            public int nMaxCustFilter;
            public int nFilterIndex;
            public string lpstrFile;
            public int nMaxFile;
            public string lpstrFileTitle;
            public int nMaxFileTitle;
            public string lpstrInitialDir;
            public string lpstrTitle;
            public int Flags;
            public short nFileOffset;
            public short nFileExtension;
            public string lpstrDefExt;
            public IntPtr lCustData = IntPtr.Zero;
            public IntPtr lpfnHook = IntPtr.Zero;
            public string lpTemplateName;
            public IntPtr pvReserved = IntPtr.Zero;
            public int dwReserved;
            public int FlagsEx;
        }

        [DllImport("Comdlg32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName openFileName);

        private const int MAX_PATH = 260;
        private const int OFN_EXPLORER = 0x00080000;
        private const int OFN_FILEMUSTEXIST = 0x00001000;
        private const int OFN_NOCHANGEDIR = 0x00000008;
        private const int OFN_PATHMUSTEXIST = 0x00000800;

        OpenFileName openFileName;
        public OpenDialog()
        {
            openFileName = new();
            openFileName.lStructSize = Marshal.SizeOf(openFileName);
            openFileName.lpstrFile = new string(new char[MAX_PATH]);
            openFileName.nMaxFile = openFileName.lpstrFile.Length;
            openFileName.lpstrFileTitle = new string(new char[64]);
            openFileName.nMaxFileTitle = openFileName.lpstrFileTitle.Length;
            openFileName.Flags = OFN_EXPLORER | OFN_FILEMUSTEXIST | OFN_NOCHANGEDIR | OFN_PATHMUSTEXIST;
        }

        public bool ShowDialog()
        {
            using (var process = System.Diagnostics.Process.GetCurrentProcess())
            {
                openFileName.hwndOwner = process.MainWindowHandle;
                process.Close();
            }

            if (GetOpenFileName(openFileName))
            {
                FileName = openFileName.lpstrFile;
                return true;
            }
            return false;
        }

        public string FileName { get; private set; }
        public string Filter { get => openFileName.lpstrFilter; set => openFileName.lpstrFilter = value; }
        public string Title { get => openFileName.lpstrTitle; set => openFileName.lpstrTitle = value; }
    }
}