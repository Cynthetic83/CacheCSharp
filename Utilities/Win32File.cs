using System;
using System.Collections.Generic;
using System.Linq;

namespace CacheCSharp
{
    public static class Win32File
    {
        public static IntPtr Open(string filePath)
        {
            IntPtr handle = Kernel32.CreateFile(filePath, Kernel32.GENERIC_READ, Kernel32.FILE_SHARE_READ | Kernel32.FILE_SHARE_WRITE | Kernel32.FILE_SHARE_DELETE, IntPtr.Zero, Kernel32.OPEN_EXISTING, 0, IntPtr.Zero);
            return handle;
        }

        public static void Close(IntPtr handle)
        {
            if (IsHandleInvalid(handle))
                return;
            Kernel32.CloseHandle(handle);
        }

        public static uint NumberOfLinks(IntPtr handle)
        {
            if (IsHandleInvalid(handle))
                return 0;

            Kernel32.BY_HANDLE_FILE_INFORMATION fileInformation;
            if (!Kernel32.GetFileInformationByHandle(handle, out fileInformation))
                return 0;

            return fileInformation.NumberOfLinks;
        }

        private static bool IsHandleInvalid(IntPtr handle)
        {
            return handle == IntPtr.Zero || handle.ToInt32() == Kernel32.INVALID_HANDLE_VALUE;
        }
    }
}
