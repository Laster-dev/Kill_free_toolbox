using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper
{
    public partial class StringObf
    {
        /// <summary>
        /// 生成依赖前一位赋值的 char 数组运行时代码（最后一行有注释，自动规范变量名）。
        /// </summary>
        public static string CStringObfA(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "// 输入字符串不能为空";

            // 生成符合C语言规范的变量名
            string varName = GenerateValidVariableName(input, "c");

            var builder = new StringBuilder();
            builder.Append($"char {varName}[{input.Length + 1}]; ");

            // 第一位直接赋值
            int prevCode = input[0];
            builder.Append($"{varName}[0]=0x{prevCode:X2};");

            // 后续每位依赖前一位
            for (int i = 1; i < input.Length; i++)
            {
                int code = input[i];
                int offset = code - prevCode;
                string op = offset >= 0 ? "+" : "-";
                int absOffset = Math.Abs(offset);
                builder.Append($"{varName}[{i}]={varName}[{i - 1}]{op}{absOffset};");
                prevCode = code;
            }
            builder.Append($"{varName}[{input.Length}]=0x00; // {input}");
            return builder.ToString();
        }
        /// <summary>
        /// 生成依赖前一位赋值的 wchar_t 数组运行时代码（最后一行有注释，自动规范变量名）。
        /// </summary>
        public static string CStringObfW(string input)
        {
            if (string.IsNullOrEmpty(input))
                return "// 输入字符串不能为空";

            // 生成符合C语言规范的变量名
            string varName = GenerateValidVariableName(input, "c");

            var builder = new StringBuilder();
            builder.Append($"wchar_t {varName}[{input.Length + 1}]; ");

            // 第一位直接赋值
            int prevCode = input[0];
            builder.Append($"{varName}[0]=0x{prevCode:X4};");

            // 后续每位依赖前一位
            for (int i = 1; i < input.Length; i++)
            {
                int code = input[i];
                int offset = code - prevCode;
                string op = offset >= 0 ? "+" : "-";
                int absOffset = Math.Abs(offset);
                builder.Append($"{varName}[{i}]={varName}[{i - 1}]{op}{absOffset};");
                prevCode = code;
            }
            builder.Append($"{varName}[{input.Length}]=0x0000; // {input}");
            return builder.ToString();
        }
    }
}
