using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper.MsvcDemangler
{
    public static class MsvcDemanglerHelper
    {
        // 参考 https://learn.microsoft.com/windows/win32/api/dbghelp/nf-dbghelp-undecoratesymbolname
        [DllImport("dbghelp.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern uint UnDecorateSymbolName(
            string name,
            StringBuilder outputString,
            uint maxStringLength,
            uint flags);

        public static bool TryUndecorate(string decorated, out string undecorated)
        {
            const int MAX = 4096;
            var sb = new StringBuilder(MAX);
            uint r = UnDecorateSymbolName(decorated, sb, (uint)sb.Capacity, 0x0000); // UNDNAME_COMPLETE
            if (r != 0)
            {
                undecorated = sb.ToString();
                return true;
            }
            undecorated = string.Empty;
            return false;
        }
        public static string Demangle(string decorated)
        {
            if (TryUndecorate(decorated, out var undec))
            {
                return undec;
            }
            return decorated;
        }
    }
}
