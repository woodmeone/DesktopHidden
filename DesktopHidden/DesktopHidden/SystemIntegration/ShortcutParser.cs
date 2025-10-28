using System.IO;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.InteropServices;
using System;

namespace DesktopHidden.SystemIntegration
{
    public class ShortcutParser
    {
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        [ClassInterface(ClassInterfaceType.None)]
        internal class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        internal interface IShellLinkW
        {
            void GetPath(            
                [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszFile,            
                int cchMaxPath,           
                ref WIN32_FIND_DATAW pfd,           
                uint fFlags); 
            void GetIDList(out IntPtr ppidl); 
            void SetIDList(IntPtr ppidl); 
            void GetDescription(            
                [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszName,            
                int cchMaxName); 
            void SetDescription(            
                [MarshalAs(UnmanagedType.LPWStr)] string pszName); 
            void GetWorkingDirectory(            
                [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszDir,            
                int cchMaxPath); 
            void SetWorkingDirectory(            
                [MarshalAs(UnmanagedType.LPWStr)] string pszDir); 
            void GetArguments(            
                [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszArgs,            
                int cchMaxPath); 
            void SetArguments(            
                [MarshalAs(UnmanagedType.LPWStr)] string pszArgs); 
            void GetHotkey(out ushort pwHotkey); 
            void SetHotkey(ushort wHotkey); 
            void GetShowCmd(out uint piShowCmd); 
            void SetShowCmd(uint iShowCmd); 
            void GetIconLocation(            
                [Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder pszIconPath,            
                int cchIconPath,            
                out int piIcon); 
            void SetIconLocation(            
                [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,            
                int iIcon); 
            void SetRelativePath(            
                [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,            
                uint dwReserved); 
            void Resolve(IntPtr hwnd, uint fFlags); 
            void SetPath(            
                [MarshalAs(UnmanagedType.LPWStr)] string pszFile); 
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        public static string? GetTargetOfShortcut(string shortcutPath) // 声明为可空
        {
            if (!File.Exists(shortcutPath))
            {
                return null;
            }

            IShellLinkW shellLink = (IShellLinkW)new ShellLink();
            if (shellLink is IPersistFile persistFile)
            {
                persistFile.Load(shortcutPath, 0);
            }
            else
            {
                return null; // 如果无法转换为IPersistFile，则返回null
            }
            
            System.Text.StringBuilder sb = new System.Text.StringBuilder(260);
            WIN32_FIND_DATAW data = new WIN32_FIND_DATAW();
            shellLink.GetPath(sb, sb.Capacity, ref data, 0); // SLGP_RAWPATH (0) or SLGP_UNCPRIORITY (2)

            return sb.ToString();
        }
    }

    [ComImport]
    [Guid("0000010c-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersist
    {
        void GetClassID(out Guid pClassID);
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    internal interface IPersistFile : IPersist
    {
        new void GetClassID(out Guid pClassID);
        void IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] System.Text.StringBuilder ppszFileName);
    }
}
