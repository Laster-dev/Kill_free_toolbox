using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper
{
    public static partial class StringObf
    {
        public static string JavaScriptStringObf(string input)
        {
            if (input == null) input = string.Empty;

            // 生成符合JavaScript规范的变量名
            string variableName = GenerateValidVariableName(input, "javascript");

            // 空串直接生成空字符串
            if (input.Length == 0)
                return $"const {variableName} = \"\";";

            // 将注释里的原文做最小清洗
            string comment = input.Replace("\r", "\\r").Replace("\n", "\\n").Replace("*/", "* /");

            var sb = new System.Text.StringBuilder();
            sb.Append("const ").Append(variableName).Append(" = String.fromCharCode(");

            for (int i = 0; i < input.Length; i++)
            {
                int v = input[i]; // UTF-16 code unit
                if (v == 0)
                {
                    sb.Append("0");
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
                            part.Append("(1<<").Append(b).Append(")");
                            first = false;
                        }
                    }
                    part.Append(")");
                    sb.Append(part.ToString());
                }

                if (i != input.Length - 1)
                    sb.Append(", ");
            }

            sb.Append("); // ").Append(comment);
            return sb.ToString();
        }
    }
}
