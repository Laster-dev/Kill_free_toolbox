using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Kill_free_toolbox.Helper.C
{
    public partial class Bin2head
    {
        public static string Build_CS(string ident, byte[] data)
        {
            Random rng = new Random();
            byte seed = (byte)rng.Next(256);
            string seedStr = seed.ToString();

            int n = data?.Length ?? 0;
            int arrLen = n == 0 ? 1 : n;

            var ops = new List<byte>();
            var vals = new List<int>();
            var sb = new StringBuilder(n / 2);

            string id = NormalizeAsCIdent(ident);
            
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.Append("public static class ").AppendLine(id);
            sb.AppendLine("{");
            sb.Append("    private static byte[] a = new byte[").Append(arrLen).AppendLine("];");
            sb.Append("    private static readonly int b = ").Append(n).AppendLine(";");
            sb.AppendLine("    private static bool c = false;");
            sb.AppendLine();
            sb.AppendLine("    public static byte[] Data { get { D(").Append(seedStr).Append("); return a; } }");
            sb.AppendLine("    public static int Length { get { D(").Append(seedStr).Append("); return b; } }");
            sb.AppendLine();
            
            sb.AppendLine("    /* Usage:");
            sb.AppendLine("    using System;");
            sb.AppendLine("    using System.IO;");
            sb.AppendLine("    ");
            sb.AppendLine("    class Program");
            sb.AppendLine("    {");
            sb.AppendLine("        static void Main()");
            sb.AppendLine("        {");
            sb.Append("            byte[] data = ").Append(id).AppendLine(".Data;");
            sb.Append("            File.WriteAllBytes(\"out.bin\", data);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("    */");
            sb.AppendLine();

            if (n > 0)
            {
                // Generate operations and values (same logic as C version)
                ops.Add(0); // XOR for a[0]
                vals.Add(seed ^ data[0]);

                for (int i = 1; i < n; i++)
                {
                    byte prev = data[i - 1];
                    byte cur = data[i];
                    int delta = cur - prev;
                    byte xor_delta = (byte)(prev ^ cur);
                    List<(string op, int val, bool wrap)> opList = new List<(string, int, bool)>
                    {
                        ("diff", delta, rng.Next(2) == 0),
                        ("xor", xor_delta, rng.Next(2) == 0)
                    };

                    if (prev != 0 && cur % prev == 0)
                    {
                        int m = cur / prev;
                        if (m >= 2 && m <= 32) opList.Add(("mul", m, false));
                    }

                    if (cur != 0 && prev % cur == 0)
                    {
                        int m = prev / cur;
                        if (m >= 2 && m <= 32) opList.Add(("div", m, false));
                    }

                    var selectedOp = opList[rng.Next(opList.Count)];
                    byte opCode;
                    int val = selectedOp.val;

                    if (selectedOp.op == "diff")
                    {
                        opCode = selectedOp.wrap ? (byte)1 : (byte)2;
                    }
                    else if (selectedOp.op == "xor")
                    {
                        opCode = selectedOp.wrap ? (byte)3 : (byte)4;
                        if (selectedOp.wrap) val = (byte)(selectedOp.val ^ seed);
                    }
                    else if (selectedOp.op == "mul")
                    {
                        opCode = 5;
                    }
                    else // div
                    {
                        opCode = 6;
                    }

                    ops.Add(opCode);
                    vals.Add(val);
                }

                sb.Append("    private static readonly byte[] o = {");
                for (int i = 0; i < ops.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n        ");
                    sb.Append(ops[i]);
                }
                sb.AppendLine("\n    };");

                sb.Append("    private static readonly int[] v = {");
                for (int i = 0; i < vals.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n        ");
                    sb.Append(vals[i]);
                }
                sb.AppendLine("\n    };");
            }

            sb.AppendLine();
            sb.AppendLine("    private static void D(byte s)");
            sb.AppendLine("    {");
            sb.AppendLine("        if (c) return;");
            sb.AppendLine("        c = true;");
            if (n > 0)
            {
                sb.AppendLine("        for (int i = 0; i < ").Append(n).AppendLine("; i++)");
                sb.AppendLine("        {");
                sb.AppendLine("            if (i == 0) { a[0] = (byte)(s ^ v[0]); continue; }");
                sb.AppendLine("            switch (o[i])");
                sb.AppendLine("            {");
                sb.AppendLine("                case 1: a[i] = (byte)((a[i-1] + s) + v[i] - s); break;");
                sb.AppendLine("                case 2: a[i] = (byte)(a[i-1] + v[i]); break;");
                sb.AppendLine("                case 3: a[i] = (byte)((a[i-1] ^ s) ^ v[i]); break;");
                sb.AppendLine("                case 4: a[i] = (byte)(a[i-1] ^ v[i]); break;");
                sb.AppendLine("                case 5: a[i] = (byte)(a[i-1] * v[i]); break;");
                sb.AppendLine("                case 6: a[i] = (byte)(a[i-1] / v[i]); break;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");

            return sb.ToString();
        }


    }
}