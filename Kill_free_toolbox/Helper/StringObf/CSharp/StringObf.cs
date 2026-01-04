using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper
{
    public static partial class StringObf
    {
        /// <summary>
        /// 生成符合编程语言规范的变量名，保留原来的下划线连接方式
        /// </summary>
        /// <param name="input">输入字符串</param>
        /// <param name="language">编程语言类型</param>
        /// <returns>清理后的变量名</returns>
        private static string GenerateValidVariableName(string input, string language = "csharp")
        {
            if (string.IsNullOrEmpty(input))
                return "var";

            // 如果字符串长度大于8，使用精简的随机变量名
            if (input.Length > 8)
            {
                return GenerateRandomVariableName();
            }

            // 先清理每个字符，然后保持原来的下划线连接方式
            var cleanedChars = new List<string>();
            
            foreach (char c in input)
            {
                if (char.IsLetterOrDigit(c))
                {
                    // 字母和数字直接保留
                    cleanedChars.Add(c.ToString());
                }
                else if (c == '_')
                {
                    // 下划线保留
                    cleanedChars.Add(c.ToString());
                }
                else
                {
                    // 其他特殊字符转换为下划线
                    cleanedChars.Add("_");
                }
            }

            // 用下划线连接所有字符，保持原来的逻辑
            string result = string.Join("_", cleanedChars);

            // 确保变量名不为空
            if (string.IsNullOrEmpty(result))
                result = "var";

            // 确保第一个字符符合语言规范
            if (char.IsDigit(result[0]))
            {
                result = "_" + result;
            }

            // 语言特定的处理
            switch (language.ToLower())
            {
                case "csharp":
                case "c":
                case "javascript":
                case "python":
                case "golang":
                case "rust":
                    // 这些语言都支持字母、数字、下划线
                    break;
            }

            return result;
        }

        /// <summary>
        /// 生成三个随机字符的变量名
        /// </summary>
        /// <returns>随机变量名</returns>
        private static string GenerateRandomVariableName()
        {
            var random = new Random();
            var chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var result = new char[3];
            
            for (int i = 0; i < 3; i++)
            {
                result[i] = chars[random.Next(chars.Length)];
            }
            
            return new string(result);
        }

        public static string CSStringObf(string input)
        {
            if (input == null) input = string.Empty;

            // 生成符合C#规范的变量名
            string variableName = GenerateValidVariableName(input, "csharp");

            // 空串直接生成 string.Empty
            if (input.Length == 0)
                return $"string {variableName} = string.Empty;";

            // 将注释里的原文做最小清洗，避免影响粘贴（可按需删掉）
            string comment = input.Replace("\r", "\\r").Replace("\n", "\\n").Replace("*/", "* /");

            var sb = new System.Text.StringBuilder();
            sb.Append("string ").Append(variableName).Append(" = string.Concat(");

            for (int i = 0; i < input.Length; i++)
            {
                int v = input[i]; // UTF-16 code unit
                if (v == 0)
                {
                    sb.Append("(char)0");
                }
                else
                {
                    // v = Σ (1<<bit)
                    // 例：'A'(65) = (1<<6) + (1<<0)
                    var part = new System.Text.StringBuilder();
                    part.Append("(char)(");
                    bool first = true;
                    for (int b = 0; b < 16; b++)
                    {
                        if (((v >> b) & 1) != 0)
                        {
                            if (!first) part.Append("+");
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

            sb.Append("); //").Append(comment);
            return sb.ToString();
        }
    }
}
