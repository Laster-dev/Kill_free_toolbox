using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

namespace Kill_free_toolbox.Helper.C
{
    public partial class Bin2head
    {
        public static string Build_Rust(string ident, byte[] data)
        {
            Random rng = new Random();
            byte seed = (byte)rng.Next(256);
            string seedStr = seed.ToString();

            int n = data?.Length ?? 0;
            int arrLen = n == 0 ? 1 : n;

            var ops = new List<byte>();
            var vals = new List<int>();
            var sb = new StringBuilder(n / 2);

            string id = NormalizeAsCIdent(ident).ToLower();
            
            sb.AppendLine("use std::sync::Once;");
            sb.AppendLine();
            
            sb.AppendLine("/* Usage:");
            sb.AppendLine("use std::fs;");
            sb.AppendLine();
            sb.AppendLine("fn main() -> std::io::Result<()> {");
            sb.Append("    let data = get_").Append(id).AppendLine("_data();");
            sb.AppendLine("    fs::write(\"out.bin\", data)?;");
            sb.AppendLine("    Ok(())");
            sb.AppendLine("}");
            sb.AppendLine("*/");
            sb.AppendLine();
            sb.Append("static mut ").Append(id.ToUpper()).Append("_DATA: [u8; ").Append(arrLen).AppendLine("] = [0; ").Append(arrLen).Append("];");
            sb.Append("static ").Append(id.ToUpper()).Append("_LENGTH: usize = ").Append(n).AppendLine(";");
            sb.Append("static ").Append(id.ToUpper()).AppendLine("_ONCE: Once = Once::new();");
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

                sb.Append("static ").Append(id.ToUpper()).Append("_OPS: [u8; ").Append(ops.Count).Append("] = [");
                for (int i = 0; i < ops.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n    ");
                    sb.Append(ops[i]);
                }
                sb.AppendLine("\n];");

                sb.Append("static ").Append(id.ToUpper()).Append("_VALS: [i32; ").Append(vals.Count).Append("] = [");
                for (int i = 0; i < vals.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    if (i % 16 == 0) sb.Append("\n    ");
                    sb.Append(vals[i]);
                }
                sb.AppendLine("\n];");
            }

            sb.AppendLine();
            sb.Append("fn init_").Append(id).AppendLine("(s: u8) {");
            sb.AppendLine("    unsafe {");
            if (n > 0)
            {
                sb.Append("        for i in 0..").Append(n).AppendLine(" {");
                sb.AppendLine("            if i == 0 {");
                sb.Append("                ").Append(id.ToUpper()).Append("_DATA[0] = s ^ (").Append(id.ToUpper()).AppendLine("_VALS[0] as u8);");
                sb.AppendLine("                continue;");
                sb.AppendLine("            }");
                sb.Append("            match ").Append(id.ToUpper()).AppendLine("_OPS[i] {");
                sb.AppendLine("                1 => {");
                sb.Append("                    ").Append(id.ToUpper()).Append("_DATA[i] = (").Append(id.ToUpper()).Append("_DATA[i-1].wrapping_add(s)).wrapping_add(").Append(id.ToUpper()).AppendLine("_VALS[i] as u8).wrapping_sub(s);");
                sb.AppendLine("                },");
                sb.AppendLine("                2 => {");
                sb.Append("                    ").Append(id.ToUpper()).Append("_DATA[i] = ").Append(id.ToUpper()).Append("_DATA[i-1].wrapping_add(").Append(id.ToUpper()).AppendLine("_VALS[i] as u8);");
                sb.AppendLine("                },");
                sb.AppendLine("                3 => {");
                sb.Append("                    ").Append(id.ToUpper()).Append("_DATA[i] = (").Append(id.ToUpper()).Append("_DATA[i-1] ^ s) ^ (").Append(id.ToUpper()).AppendLine("_VALS[i] as u8);");
                sb.AppendLine("                },");
                sb.AppendLine("                4 => {");
                sb.Append("                    ").Append(id.ToUpper()).Append("_DATA[i] = ").Append(id.ToUpper()).Append("_DATA[i-1] ^ (").Append(id.ToUpper()).AppendLine("_VALS[i] as u8);");
                sb.AppendLine("                },");
                sb.AppendLine("                5 => {");
                sb.Append("                    ").Append(id.ToUpper()).Append("_DATA[i] = ").Append(id.ToUpper()).Append("_DATA[i-1].wrapping_mul(").Append(id.ToUpper()).AppendLine("_VALS[i] as u8);");
                sb.AppendLine("                },");
                sb.AppendLine("                6 => {");
                sb.Append("                    ").Append(id.ToUpper()).Append("_DATA[i] = ").Append(id.ToUpper()).Append("_DATA[i-1] / (").Append(id.ToUpper()).AppendLine("_VALS[i] as u8);");
                sb.AppendLine("                },");
                sb.AppendLine("                _ => {}");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
            }
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.Append("pub fn get_").Append(id).AppendLine("_data() -> &'static [u8] {");
            sb.Append("    ").Append(id.ToUpper()).Append("_ONCE.call_once(|| {");
            sb.Append("\n        init_").Append(id).Append("(").Append(seedStr).AppendLine(");");
            sb.AppendLine("    });");
            sb.AppendLine("    unsafe {");
            sb.Append("        &").Append(id.ToUpper()).Append("_DATA[..").Append(id.ToUpper()).AppendLine("_LENGTH]");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine();
            sb.Append("pub fn get_").Append(id).AppendLine("_length() -> usize {");
            sb.Append("    ").Append(id.ToUpper()).AppendLine("_LENGTH");
            sb.AppendLine("}");

            return sb.ToString();
        }


    }
}