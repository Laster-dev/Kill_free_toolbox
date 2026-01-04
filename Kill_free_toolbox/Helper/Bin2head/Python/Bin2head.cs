using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Kill_free_toolbox.Helper.C
{
    public partial class Bin2head
    {
        public static string Build_Py(string ident, byte[] data)
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
            
            sb.AppendLine("import threading");
            sb.AppendLine();
            
            sb.AppendLine("\"\"\" Usage:");
            sb.Append("data = get_").Append(id.ToLower()).AppendLine("_data()");
            sb.AppendLine("with open('out.bin', 'wb') as f:");
            sb.AppendLine("    f.write(data)");
            sb.AppendLine("\"\"\"");
            sb.AppendLine();
            sb.Append("_").Append(id.ToLower()).Append("_data = bytearray(").Append(arrLen).AppendLine(")");
            sb.Append("_").Append(id.ToLower()).Append("_length = ").AppendLine(n.ToString());
            sb.Append("_").Append(id.ToLower()).AppendLine("_initialized = False");
            sb.Append("_").Append(id.ToLower()).AppendLine("_lock = threading.Lock()");
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

                sb.Append("_").Append(id.ToLower()).Append("_ops = [");
                for (int i = 0; i < ops.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n    ");
                    sb.Append(ops[i]);
                }
                sb.AppendLine("\n]");

                sb.Append("_").Append(id.ToLower()).Append("_vals = [");
                for (int i = 0; i < vals.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n    ");
                    sb.Append(vals[i]);
                }
                sb.AppendLine("\n]");
            }

            sb.AppendLine();
            sb.Append("def _init_").Append(id.ToLower()).AppendLine("(s):");
            sb.Append("    global _").Append(id.ToLower()).AppendLine("_initialized");
            sb.Append("    if _").Append(id.ToLower()).AppendLine("_initialized:");
            sb.AppendLine("        return");
            sb.Append("    _").Append(id.ToLower()).AppendLine("_initialized = True");
            if (n > 0)
            {
                sb.Append("    for i in range(").Append(n).AppendLine("):");
                sb.AppendLine("        if i == 0:");
                sb.Append("            _").Append(id.ToLower()).Append("_data[0] = (s ^ _").Append(id.ToLower()).AppendLine("_vals[0]) & 0xFF");
                sb.AppendLine("            continue");
                sb.Append("        op = _").Append(id.ToLower()).AppendLine("_ops[i]");
                sb.Append("        val = _").Append(id.ToLower()).AppendLine("_vals[i]");
                sb.AppendLine("        if op == 1:");
                sb.Append("            _").Append(id.ToLower()).Append("_data[i] = ((_").Append(id.ToLower()).AppendLine("_data[i-1] + s) + val - s) & 0xFF");
                sb.AppendLine("        elif op == 2:");
                sb.Append("            _").Append(id.ToLower()).Append("_data[i] = (_").Append(id.ToLower()).AppendLine("_data[i-1] + val) & 0xFF");
                sb.AppendLine("        elif op == 3:");
                sb.Append("            _").Append(id.ToLower()).Append("_data[i] = ((_").Append(id.ToLower()).AppendLine("_data[i-1] ^ s) ^ val) & 0xFF");
                sb.AppendLine("        elif op == 4:");
                sb.Append("            _").Append(id.ToLower()).Append("_data[i] = (_").Append(id.ToLower()).AppendLine("_data[i-1] ^ val) & 0xFF");
                sb.AppendLine("        elif op == 5:");
                sb.Append("            _").Append(id.ToLower()).Append("_data[i] = (_").Append(id.ToLower()).AppendLine("_data[i-1] * val) & 0xFF");
                sb.AppendLine("        elif op == 6:");
                sb.Append("            _").Append(id.ToLower()).Append("_data[i] = (_").Append(id.ToLower()).AppendLine("_data[i-1] // val) & 0xFF");
            }
            sb.AppendLine();
            sb.Append("def get_").Append(id.ToLower()).AppendLine("_data():");
            sb.Append("    with _").Append(id.ToLower()).AppendLine("_lock:");
            sb.Append("        _init_").Append(id.ToLower()).Append("(").Append(seedStr).AppendLine(")");
            sb.Append("    return _").Append(id.ToLower()).AppendLine("_data");
            sb.AppendLine();
            sb.Append("def get_").Append(id.ToLower()).AppendLine("_length():");
            sb.Append("    return _").Append(id.ToLower()).AppendLine("_length");

            return sb.ToString();
        }


    }
}