using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Kill_free_toolbox.Helper.C
{
    public partial class Bin2head
    {
        public static string Build_C(string ident, byte[] data)
        {
            Random rng = new Random();
            byte seed = (byte)rng.Next(256);
            string seedStr = seed.ToString();

            int n = data?.Length ?? 0;
            int arrLen = n == 0 ? 1 : n;

            var ops = new List<byte>();
            var vals = new List<int>();
            var sb = new StringBuilder(n / 2);

            sb.AppendLine("#pragma once");
            sb.AppendLine("#include <stdint.h>");
            sb.AppendLine("#include <stddef.h>");

            string id = NormalizeAsCIdent(ident);
            sb.Append("static uint8_t a[").Append(arrLen).AppendLine("];");
            sb.Append("static const size_t b=").Append(n).AppendLine(";");
            sb.AppendLine("static int c=0;");
            sb.AppendLine("static void d(uint8_t s);");
            sb.Append("#define ").Append(id).Append(" (d(").Append(seedStr).Append("),a)\n");
            sb.Append("#define ").Append(id).Append("_LEN (d(").Append(seedStr).Append("),b)\n");

            sb.AppendLine("/* Usage:");
            sb.AppendLine("#include <stdio.h>");
            sb.Append("#include \"").Append(id).AppendLine(".h\"");
            sb.AppendLine("int main(){");
            sb.Append("FILE* f=fopen(\"out.bin\",\"wb\");\nfwrite(")
              .Append(id).Append(",1,").Append(id).AppendLine("_LEN,f);fclose(f);\n}\n");
            sb.AppendLine("*/");



            if (n > 0)
            {
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

                sb.Append("static const uint8_t o[]={");
                for (int i = 0; i < ops.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(ops[i]);
                    if ((i + 1) % 16 == 0) sb.AppendLine();
                    else sb.Append(' ');
                }
                if (ops.Count % 16 != 0) sb.AppendLine();
                sb.AppendLine("};");

                sb.Append("static const int v[]={");
                for (int i = 0; i < vals.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(vals[i]);
                    if ((i + 1) % 16 == 0) sb.AppendLine();
                    else sb.Append(' ');
                }
                if (vals.Count % 16 != 0) sb.AppendLine();
                sb.AppendLine("};");
            }

            sb.AppendLine("static void d(uint8_t s){");
            sb.AppendLine("if(c)return;");
            sb.AppendLine("c=1;");
            if (n > 0)
            {
                sb.AppendLine("for(size_t i=0;i<").Append(n).Append(";i++){");
                sb.AppendLine("if(i==0){a[0]=s^v[0];continue;}");
                sb.AppendLine("switch(o[i]){");
                sb.AppendLine("case 1:a[i]=(a[i-1]+s)+v[i]-s;break;"); // Wrapped diff
                sb.AppendLine("case 2:a[i]=a[i-1]+v[i];break;"); // Diff
                sb.AppendLine("case 3:a[i]=(a[i-1]^s)^v[i];break;"); // Wrapped XOR
                sb.AppendLine("case 4:a[i]=a[i-1]^v[i];break;"); // XOR
                sb.AppendLine("case 5:a[i]=a[i-1]*v[i];break;"); // Mul
                sb.AppendLine("case 6:a[i]=a[i-1]/v[i];break;"); // Div
                sb.AppendLine("}");
                sb.AppendLine("}");
            }
            sb.AppendLine("}");



            return sb.ToString();
        }

        public static string NormalizeAsCIdent(string s)// 规范化为 C 语言标识符
        {
            if (string.IsNullOrEmpty(s)) return "hidden";
            string t = Regex.Replace(s, "[^a-zA-Z0-9_]", "_");
            if (char.IsDigit(t[0])) t = "_" + t;
            if (Regex.IsMatch(t, "^_*$")) t = "hidden";
            return t;
        }
    }
}