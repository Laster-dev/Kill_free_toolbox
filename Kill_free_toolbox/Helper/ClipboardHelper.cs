using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper
{
    public static class ClipboardHelper
    {
        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr hMem);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr hMem);

        /// <summary>
        /// 将指定文本放入剪切板（Unicode）。如果剪切板被占用，会尝试重试若干次。
        /// 返回 true 表示成功。
        /// </summary>
        /// <param name="text">要放入剪切板的文本（null 当作空字符串）</param>
        /// <param name="retry">当 OpenClipboard 失败时的重试次数</param>
        /// <param name="retryDelayMilliseconds">重试间隔（毫秒）</param>
        /// <returns>是否成功写入剪切板</returns>
        public static bool SetText(string text, int retry = 10, int retryDelayMilliseconds = 100)
        {
            if (text == null) text = string.Empty;

            // 将字符串按 Unicode 编码转换为字节数组，并添加末尾的两个字节的空字符
            byte[] bytes = Encoding.Unicode.GetBytes(text + '\0');

            // 先尝试打开剪切板（可能被其他进程短暂占用），做重试
            bool opened = false;
            for (int i = 0; i < retry; i++)
            {
                if (OpenClipboard(IntPtr.Zero))
                {
                    opened = true;
                    break;
                }
                Thread.Sleep(retryDelayMilliseconds);
            }

            if (!opened)
            {
                return false;
            }

            IntPtr hGlobal = IntPtr.Zero;
            try
            {
                if (!EmptyClipboard())
                {
                    // 不能清空剪贴板，但仍然继续尝试写入（可根据需要改为失败）
                }

                // 分配全局内存（可移动）
                hGlobal = GlobalAlloc(GMEM_MOVEABLE, new UIntPtr((uint)bytes.Length));
                if (hGlobal == IntPtr.Zero)
                {
                    return false;
                }

                // 锁定内存并拷贝数据
                IntPtr target = GlobalLock(hGlobal);
                if (target == IntPtr.Zero)
                {
                    // 锁定失败，释放内存并返回
                    GlobalFree(hGlobal);
                    hGlobal = IntPtr.Zero;
                    return false;
                }

                try
                {
                    Marshal.Copy(bytes, 0, target, bytes.Length);
                }
                finally
                {
                    // 解锁内存
                    GlobalUnlock(hGlobal);
                }

                // 把内存句柄放到剪切板，Windows 接管该句柄的释放
                IntPtr setResult = SetClipboardData(CF_UNICODETEXT, hGlobal);
                if (setResult == IntPtr.Zero)
                {
                    // 放入失败，释放自己分配的内存
                    GlobalFree(hGlobal);
                    hGlobal = IntPtr.Zero;
                    return false;
                }

                // 成功，Windows 接管内存句柄，所以不要再释放 hGlobal
                hGlobal = IntPtr.Zero;
                return true;
            }
            finally
            {
                // 关闭剪切板句柄
                CloseClipboard();

                // 如果在失败路径我们还拥有 hGlobal，需要释放避免泄漏
                if (hGlobal != IntPtr.Zero)
                {
                    GlobalFree(hGlobal);
                }
            }
        }
    }
}
