using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Kill_free_toolbox.Helper.C
{
    public partial class Bin2head
    {
        public static string Build_JS(string ident, byte[] data)
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
            
            sb.AppendLine("/* Usage:");
            sb.AppendLine("// In browser:");
            sb.Append("const data = ").Append(id).AppendLine(".getData();");
            sb.AppendLine("const blob = new Blob([data], {type: 'application/octet-stream'});");
            sb.AppendLine("const url = URL.createObjectURL(blob);");
            sb.AppendLine("const a = document.createElement('a');");
            sb.AppendLine("a.href = url; a.download = 'out.bin'; a.click();");
            sb.AppendLine();
            sb.AppendLine("// In Node.js:");
            sb.AppendLine("const fs = require('fs');");
            sb.Append("const data = ").Append(id).AppendLine(".getData();");
            sb.AppendLine("fs.writeFileSync('out.bin', Buffer.from(data));");
            sb.AppendLine("*/");
            sb.AppendLine();
            
            sb.AppendLine("(function() {");
            sb.Append("    let a = new Uint8Array(").Append(arrLen).AppendLine(");");
            sb.Append("    const b = ").Append(n).AppendLine(";");
            sb.AppendLine("    let c = false;");
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

                sb.Append("    const o = [");
                for (int i = 0; i < ops.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n        ");
                    sb.Append(ops[i]);
                }
                sb.AppendLine("\n    ];");

                sb.Append("    const v = [");
                for (int i = 0; i < vals.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n        ");
                    sb.Append(vals[i]);
                }
                sb.AppendLine("\n    ];");
            }

            sb.AppendLine();
            sb.AppendLine("    function d(s) {");
            sb.AppendLine("        if (c) return;");
            sb.AppendLine("        c = true;");
            if (n > 0)
            {
                sb.Append("        for (let i = 0; i < ").Append(n).AppendLine("; i++) {");
                sb.AppendLine("            if (i === 0) {");
                sb.AppendLine("                a[0] = (s ^ v[0]) & 0xFF;");
                sb.AppendLine("                continue;");
                sb.AppendLine("            }");
                sb.AppendLine("            switch (o[i]) {");
                sb.AppendLine("                case 1: a[i] = ((a[i-1] + s) + v[i] - s) & 0xFF; break;");
                sb.AppendLine("                case 2: a[i] = (a[i-1] + v[i]) & 0xFF; break;");
                sb.AppendLine("                case 3: a[i] = ((a[i-1] ^ s) ^ v[i]) & 0xFF; break;");
                sb.AppendLine("                case 4: a[i] = (a[i-1] ^ v[i]) & 0xFF; break;");
                sb.AppendLine("                case 5: a[i] = (a[i-1] * v[i]) & 0xFF; break;");
                sb.AppendLine("                case 6: a[i] = Math.floor(a[i-1] / v[i]) & 0xFF; break;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.Append("    window.").Append(id).AppendLine(" = {");
            sb.AppendLine("        getData: function() {");
            sb.Append("            d(").Append(seedStr).AppendLine(");");
            sb.AppendLine("            return a;");
            sb.AppendLine("        },");
            sb.AppendLine("        getLength: function() {");
            sb.Append("            return ").Append(n).AppendLine(";");
            sb.AppendLine("        }");
            sb.AppendLine("    };");
            sb.AppendLine("})();");

            return sb.ToString();
        }


    }
}