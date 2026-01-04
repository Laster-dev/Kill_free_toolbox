using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper
{
    public static partial class StringObf
    {
        public static string RustStringObf(string input)
        {
            if (input == null) input = string.Empty;

            // 生成符合Rust规范的变量名
            string variableName = GenerateValidVariableName(input, "rust");

            // 空串直接生成空字符串
            if (input.Length == 0)
                return $"let {variableName}: String = String::new();";

            // 将注释里的原文做最小清洗
            string comment = input.Replace("\r", "\\r").Replace("\n", "\\n").Replace("*/", "* /");

            var sb = new System.Text.StringBuilder();
            sb.Append("let ").Append(variableName).Append(": String = [");

            for (int i = 0; i < input.Length; i++)
            {
                int v = input[i]; // UTF-16 code unit
                if (v == 0)
                {
                    sb.Append("0u32");
                }
                else
                {
                    // 使用位运算混淆：v = Σ (1<<bit)
                    var part = new System.Text.StringBuilder();
                    part.Append("(");
                    bool first = true;
                    for (int b = 0; b < 16; b++)
                    {
                        if (((v >> b) & 1) != 0)
                        {
                            if (!first) part.Append(" + ");
                            part.Append("(1u32<<").Append(b).Append(")");
                            first = false;
                        }
                    }
                    part.Append(")");
                    sb.Append(part.ToString());
                }

                if (i != input.Length - 1)
                    sb.Append(", ");
            }

            sb.Append("].iter().map(|&x| char::from_u32(x).unwrap()).collect(); // ").Append(comment);
            return sb.ToString();
        }
    }
}
