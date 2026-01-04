using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Kill_free_toolbox.Helper.C
{
    public partial class Bin2head
    {
        public static string Build_GO(string ident, byte[] data)
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
            
            sb.AppendLine("package main");
            sb.AppendLine();
            sb.AppendLine("import \"sync\"");
            sb.AppendLine();
            
            sb.AppendLine("/* Usage:");
            sb.AppendLine("package main");
            sb.AppendLine();
            sb.AppendLine("import (");
            sb.AppendLine("    \"os\"");
            sb.AppendLine(")");
            sb.AppendLine();
            sb.AppendLine("func main() {");
            sb.Append("    data := Get").Append(id).AppendLine("()");
            sb.AppendLine("    os.WriteFile(\"out.bin\", data, 0644)");
            sb.AppendLine("}");
            sb.AppendLine("*/");
            sb.AppendLine();
            sb.Append("var ").Append(id.ToLower()).Append("Data = make([]byte, ").Append(arrLen).AppendLine(")");
            sb.Append("var ").Append(id.ToLower()).Append("Length = ").AppendLine(n.ToString());
            sb.Append("var ").Append(id.ToLower()).AppendLine("Once sync.Once");
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

                sb.Append("var ").Append(id.ToLower()).Append("Ops = []byte{");
                for (int i = 0; i < ops.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n\t");
                    sb.Append(ops[i]);
                }
                sb.AppendLine("\n}");

                sb.Append("var ").Append(id.ToLower()).Append("Vals = []int{");
                for (int i = 0; i < vals.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n\t");
                    sb.Append(vals[i]);
                }
                sb.AppendLine("\n}");
            }

            sb.AppendLine();
            sb.Append("func Get").Append(id).AppendLine("() []byte {");
            sb.Append("\t").Append(id.ToLower()).Append("Once.Do(func() {");
            sb.Append("\n\t\tinit").Append(id).Append("(").Append(seedStr).AppendLine(")");
            sb.AppendLine("\t})");
            sb.Append("\treturn ").Append(id.ToLower()).AppendLine("Data");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.Append("func Get").Append(id).AppendLine("Length() int {");
            sb.Append("\treturn ").Append(id.ToLower()).AppendLine("Length");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.Append("func init").Append(id).AppendLine("(s byte) {");
            if (n > 0)
            {
                sb.Append("\tfor i := 0; i < ").Append(n).AppendLine("; i++ {");
                sb.AppendLine("\t\tif i == 0 {");
                sb.Append("\t\t\t").Append(id.ToLower()).Append("Data[0] = s ^ byte(").Append(id.ToLower()).AppendLine("Vals[0])");
                sb.AppendLine("\t\t\tcontinue");
                sb.AppendLine("\t\t}");
                sb.Append("\t\tswitch ").Append(id.ToLower()).AppendLine("Ops[i] {");
                sb.AppendLine("\t\tcase 1:");
                sb.Append("\t\t\t").Append(id.ToLower()).Append("Data[i] = (").Append(id.ToLower()).Append("Data[i-1] + s) + byte(").Append(id.ToLower()).AppendLine("Vals[i]) - s");
                sb.AppendLine("\t\tcase 2:");
                sb.Append("\t\t\t").Append(id.ToLower()).Append("Data[i] = ").Append(id.ToLower()).Append("Data[i-1] + byte(").Append(id.ToLower()).AppendLine("Vals[i])");
                sb.AppendLine("\t\tcase 3:");
                sb.Append("\t\t\t").Append(id.ToLower()).Append("Data[i] = (").Append(id.ToLower()).Append("Data[i-1] ^ s) ^ byte(").Append(id.ToLower()).AppendLine("Vals[i])");
                sb.AppendLine("\t\tcase 4:");
                sb.Append("\t\t\t").Append(id.ToLower()).Append("Data[i] = ").Append(id.ToLower()).Append("Data[i-1] ^ byte(").Append(id.ToLower()).AppendLine("Vals[i])");
                sb.AppendLine("\t\tcase 5:");
                sb.Append("\t\t\t").Append(id.ToLower()).Append("Data[i] = ").Append(id.ToLower()).Append("Data[i-1] * byte(").Append(id.ToLower()).AppendLine("Vals[i])");
                sb.AppendLine("\t\tcase 6:");
                sb.Append("\t\t\t").Append(id.ToLower()).Append("Data[i] = ").Append(id.ToLower()).Append("Data[i-1] / byte(").Append(id.ToLower()).AppendLine("Vals[i])");
                sb.AppendLine("\t\t}");
                sb.AppendLine("\t}");
            }
            sb.AppendLine("}");

            return sb.ToString();
        }


    }
}