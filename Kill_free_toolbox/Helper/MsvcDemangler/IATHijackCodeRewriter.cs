using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Text;
using System.Runtime.InteropServices;

namespace Kill_free_toolbox.Helper.MsvcDemangler
{
    public class MemberInfo
    {
        public string Namespace { get; set; }
        public string ClassName { get; set; }
        public string MemberName { get; set; }
        public string ReturnType { get; set; }
        public string Parameters { get; set; }
        public string Declaration { get; set; }
        public string MemberType { get; set; } // function/constructor/destructor
        public bool IsVirtual { get; set; }
        public bool IsConst { get; set; }
        public bool IsStatic { get; set; }
        public string OriginalPrototype { get; set; }
    }

    public static class IATHijackCodeRewriter
    {
        [DllImport("dbghelp.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern uint UnDecorateSymbolName(
            string name,
            StringBuilder outputString,
            uint maxStringLength,
            uint flags);

        public static bool TryUndecorate(string decorated, out string undecorated)
        {
            const int MAX = 4096;
            var sb = new StringBuilder(MAX);
            uint r = UnDecorateSymbolName(decorated, sb, (uint)sb.Capacity, 0x0000);
            if (r != 0)
            {
                undecorated = sb.ToString();
                return true;
            }
            undecorated = string.Empty;
            return false;
        }

        // 提取所有的extern "C" __declspec(dllexport) 修饰名函数
        public static List<string> ExtractMangledFunctions(string code)
        {
            var result = new List<string>();
            var regex = new Regex(@"extern\s+""C""\s+__declspec\(dllexport\)\s+void\s+(?<mangled>\?[\?\w@]+)\s*\(\)\s*\{[^}]*\}", RegexOptions.Multiline);

            foreach (Match match in regex.Matches(code))
            {
                result.Add(match.Groups["mangled"].Value);
            }
            return result;
        }

        // 删除所有的extern "C" __declspec(dllexport) 函数定义
        public static string RemoveMangledFunctions(string code)
        {
            var regex = new Regex(@"extern\s+""C""\s+__declspec\(dllexport\)\s+void\s+\?[\?\w@]+\s*\(\)\s*\{[^}]*\}\s*", RegexOptions.Multiline);
            return regex.Replace(code, "");
        }

        // 解析还原后的C++原型
        public static MemberInfo ParseCppPrototype(string prototype)
        {
            var info = new MemberInfo { OriginalPrototype = prototype };

            // 简单成员函数格式（无访问修饰符）：long __cdecl CUpgrade::ForceUpgrade(void)
            var simpleFuncRegex = new Regex(@"^(.+?)\s+__\w+\s+(.+?)::([^\(]+)\(([^)]*)\)");
            var simpleFuncMatch = simpleFuncRegex.Match(prototype);
            if (simpleFuncMatch.Success)
            {
                var qualifiedType = simpleFuncMatch.Groups[2].Value;
                info.ReturnType = CleanType(simpleFuncMatch.Groups[1].Value);
                info.ClassName = ExtractClassName(qualifiedType);
                info.Namespace = ExtractNamespace(qualifiedType);
                info.MemberName = simpleFuncMatch.Groups[3].Value;
                info.Parameters = CleanParameters(simpleFuncMatch.Groups[4].Value);
                info.MemberType = "function";
                return info;
            }

            // 1. 处理全局函数: void __cdecl std::_Xlength_error(char const *)
            var globalFuncRegex = new Regex(@"^([^:]+)\s+__\w+\s+([^:]+)::([^\(]+)\(([^)]*)\)");
            var globalMatch = globalFuncRegex.Match(prototype);
            if (globalMatch.Success)
            {
                info.ReturnType = CleanType(globalMatch.Groups[1].Value);
                info.Namespace = globalMatch.Groups[2].Value;
                info.ClassName = ""; // 全局函数没有类名
                info.MemberName = globalMatch.Groups[3].Value;
                info.Parameters = CleanParameters(globalMatch.Groups[4].Value);
                info.MemberType = "function";
                return info;
            }

            // 2. 处理静态成员变量: class std::basic_ostream<wchar_t,struct std::char_traits<wchar_t> > std::wcerr
            var staticVarRegex = new Regex(@"^([^:]+)\s+([^:]+)::([^\(]+)$");
            var staticVarMatch = staticVarRegex.Match(prototype);
            if (staticVarMatch.Success && !prototype.Contains("("))
            {
                info.ReturnType = CleanType(staticVarMatch.Groups[1].Value);
                info.Namespace = staticVarMatch.Groups[2].Value;
                info.ClassName = ""; // 静态变量没有类名
                info.MemberName = staticVarMatch.Groups[3].Value;
                info.MemberType = "variable";
                return info;
            }

            // 3. 处理成员函数: public: class std::basic_ostream<wchar_t,struct std::char_traits<wchar_t> > & __thiscall std::basic_ostream<wchar_t,struct std::char_traits<wchar_t> >::flush(void)
            // 放宽对返回类型与类作用域的匹配，允许包含 :: 与 <>
            var memberFuncRegex = new Regex(@"(public|protected|private):\s+(static\s+)?(virtual\s+)?(.+)\s+__\w+\s+(.+)::([^\(]+)\(([^)]*)\)(.*)");
            var memberMatch = memberFuncRegex.Match(prototype);
            if (memberMatch.Success)
            {
                info.ReturnType = CleanType(memberMatch.Groups[4].Value);
                var qualifiedType = memberMatch.Groups[5].Value;
                info.ClassName = ExtractClassName(qualifiedType);
                info.Namespace = ExtractNamespace(qualifiedType);
                info.MemberName = memberMatch.Groups[6].Value;
                info.Parameters = CleanParameters(memberMatch.Groups[7].Value);
                info.IsVirtual = memberMatch.Groups[3].Value.Contains("virtual");
                info.IsStatic = memberMatch.Groups[2].Value.Contains("static");
                info.IsConst = memberMatch.Groups[8].Value.Contains("const");
                info.MemberType = "function";
                return info;
            }

            // 4. 处理构造函数: public: __cdecl std::_Lockit::_Lockit(int)
            var ctorRegex = new Regex(@"(public|protected|private):\s+__\w+\s+(.+)::([^:]+)\(([^)]*)\)");
            var ctorMatch = ctorRegex.Match(prototype);
            if (ctorMatch.Success && ExtractClassName(ctorMatch.Groups[2].Value) == ctorMatch.Groups[3].Value)
            {
                var qualifiedType = ctorMatch.Groups[2].Value;
                info.ClassName = ExtractClassName(qualifiedType);
                info.Namespace = ExtractNamespace(qualifiedType);
                info.MemberName = ctorMatch.Groups[3].Value;
                info.ReturnType = "";
                info.Parameters = CleanParameters(ctorMatch.Groups[4].Value);
                info.MemberType = "constructor";
                return info;
            }

            // 5. 处理析构函数: public: __thiscall std::_Lockit::~_Lockit(void)
            var dtorRegex = new Regex(@"(public|protected|private):\s+(virtual\s+)?__\w+\s+(.+)::~([^:]+)\(([^)]*)\)");
            var dtorMatch = dtorRegex.Match(prototype);
            if (dtorMatch.Success)
            {
                var qualifiedType = dtorMatch.Groups[3].Value;
                info.ClassName = ExtractClassName(qualifiedType);
                info.Namespace = ExtractNamespace(qualifiedType);
                info.MemberName = "~" + dtorMatch.Groups[4].Value;
                info.ReturnType = "";
                info.Parameters = "";
                info.IsVirtual = dtorMatch.Groups[2].Value.Contains("virtual");
                info.MemberType = "destructor";
                return info;
            }

            // 6. 处理操作符重载: public: __thiscall std::locale::id::operator unsigned int(void)
            var operatorRegex = new Regex(@"(public|protected|private):\s+__\w+\s+(.+)::([^:]+)::([^\(]+)\(([^)]*)\)");
            var operatorMatch = operatorRegex.Match(prototype);
            if (operatorMatch.Success && operatorMatch.Groups[4].Value.StartsWith("operator"))
            {
                var outerQualified = operatorMatch.Groups[2].Value;
                info.Namespace = ExtractNamespace(outerQualified);
                info.ClassName = operatorMatch.Groups[3].Value;
                info.MemberName = operatorMatch.Groups[4].Value;
                info.ReturnType = "";
                info.Parameters = CleanParameters(operatorMatch.Groups[5].Value);
                info.MemberType = "function";
                return info;
            }

            // 7. 处理嵌套类成员: public: static class std::locale::id std::ctype<wchar_t>::id
            var nestedClassRegex = new Regex(@"(public|protected|private):\s+(static\s+)?(.+)\s+(.+)::([^:]+)::([^\(]+)");
            var nestedMatch = nestedClassRegex.Match(prototype);
            if (nestedMatch.Success && !prototype.Contains("("))
            {
                info.ReturnType = CleanType(nestedMatch.Groups[3].Value);
                var outerQualified = nestedMatch.Groups[4].Value;
                info.Namespace = ExtractNamespace(outerQualified);
                info.ClassName = nestedMatch.Groups[5].Value; // 外层类
                info.MemberName = nestedMatch.Groups[6].Value; // 内层类或成员
                info.IsStatic = nestedMatch.Groups[2].Value.Contains("static");
                info.MemberType = "variable"; // 静态成员变量
                return info;
            }

            // 8. 处理简单的成员函数: public: void __thiscall std::basic_ostream<wchar_t,struct std::char_traits<wchar_t> >::_Osfx(void)
            var simpleMemberRegex = new Regex(@"(public|protected|private):\s+(.+)\s+__\w+\s+(.+)::([^\(]+)\(([^)]*)\)(.*)");
            var simpleMatch = simpleMemberRegex.Match(prototype);
            if (simpleMatch.Success)
            {
                info.ReturnType = CleanType(simpleMatch.Groups[2].Value);
                var qualifiedType = simpleMatch.Groups[3].Value;
                info.ClassName = ExtractClassName(qualifiedType);
                info.Namespace = ExtractNamespace(qualifiedType);
                info.MemberName = simpleMatch.Groups[4].Value;
                info.Parameters = CleanParameters(simpleMatch.Groups[5].Value);
                info.IsConst = simpleMatch.Groups[6].Value.Contains("const");
                info.MemberType = "function";
                return info;
            }

            return info;
        }

        // 从复杂的类型字符串中提取类名
        private static string ExtractClassName(string typeString)
        {
            // 处理模板类型，如: std::basic_ostream<wchar_t,struct std::char_traits<wchar_t> >
            var templateMatch = Regex.Match(typeString, @"([^<]+)<");
            if (templateMatch.Success)
            {
                var className = templateMatch.Groups[1].Value;
                // 提取最后一个::后面的部分
                var templateParts = className.Split(new[] { "::" }, StringSplitOptions.None);
                return templateParts[templateParts.Length - 1];
            }

            // 处理嵌套类型，如: std::locale::id
            var nestedMatch = Regex.Match(typeString, @"([^:]+)::([^:]+)$");
            if (nestedMatch.Success)
            {
                return nestedMatch.Groups[2].Value; // 返回内层类名
            }

            // 处理普通类型，如: std::_Lockit
            var normalParts = typeString.Split(new[] { "::" }, StringSplitOptions.None);
            return normalParts[normalParts.Length - 1];
        }

        // 从限定类型中提取命名空间（去掉类名部分）
        private static string ExtractNamespace(string qualifiedType)
        {
            if (string.IsNullOrWhiteSpace(qualifiedType)) return "";

            // 去掉模板参数
            var withoutTemplate = Regex.Replace(qualifiedType, "<[^>]*>", "");
            var parts = withoutTemplate.Split(new[] { "::" }, StringSplitOptions.None);
            if (parts.Length <= 1) return "";
            // 移除最后的类名，返回前面的命名空间链
            return string.Join("::", parts, 0, parts.Length - 1);
        }

        // 清理类型名称
        private static string CleanType(string type)
        {
            var cleaned = type.Trim()
                .Replace("class ", "")
                .Replace("struct ", "")
                .Replace("enum ", "")
                .Replace(" __ptr64", "")
                .Trim();

            // 简化复杂的模板类型
            if (cleaned.Contains("<") && cleaned.Contains(">"))
            {
                // 对于模板类型，只保留基本类型名
                var templateMatch = Regex.Match(cleaned, @"([^<]+)<");
                if (templateMatch.Success)
                {
                    var baseType = templateMatch.Groups[1].Value;
                    // 提取最后一个::后面的部分
                    var parts = baseType.Split(new[] { "::" }, StringSplitOptions.None);
                    return parts[parts.Length - 1];
                }
            }

            // 处理引用类型
            if (cleaned.EndsWith("&"))
            {
                cleaned = cleaned.TrimEnd('&').Trim() + "&";
            }

            // 处理指针类型
            if (cleaned.EndsWith("*"))
            {
                cleaned = cleaned.TrimEnd('*').Trim() + "*";
            }

            return cleaned;
        }

        // 清理参数列表
        private static string CleanParameters(string parameters)
        {
            if (string.IsNullOrWhiteSpace(parameters) || parameters == "void")
                return "";

            var cleaned = parameters
                .Replace("class ", "")
                .Replace("struct ", "")
                .Replace("enum ", "")
                .Replace(" __ptr64", "")
                .Replace(" *", "*")
                .Replace(" &", "&");

            // 简化参数名
            var paramParts = cleaned.Split(',');
            var result = new List<string>();

            foreach (var param in paramParts)
            {
                var trimmed = param.Trim();
                if (!string.IsNullOrEmpty(trimmed))
                {
                    // 简化复杂的模板类型
                    if (trimmed.Contains("<") && trimmed.Contains(">"))
                    {
                        var templateMatch = Regex.Match(trimmed, @"([^<]+)<");
                        if (templateMatch.Success)
                        {
                            var baseType = templateMatch.Groups[1].Value;
                            var parts = baseType.Split(new[] { "::" }, StringSplitOptions.None);
                            trimmed = parts[parts.Length - 1];
                        }
                    }
                    else if (trimmed.Contains("::"))
                    {
                        // 提取类型，移除复杂的命名空间前缀
                        var parts = trimmed.Split(new[] { "::" }, StringSplitOptions.None);
                        if (parts.Length > 1)
                        {
                            trimmed = parts[parts.Length - 1];
                        }
                    }
                    result.Add(trimmed);
                }
            }

            return string.Join(", ", result);
        }

        // 生成C++类声明代码
        public static string GenerateCppClasses(List<string> mangledNames)
        {
            var namespaceDict = new Dictionary<string, Dictionary<string, List<MemberInfo>>>();

            // 解析所有符号
            foreach (var mangled in mangledNames)
            {
                if (TryUndecorate(mangled, out var prototype))
                {
                    var memberInfo = ParseCppPrototype(prototype);

                    if (!string.IsNullOrEmpty(memberInfo.ClassName))
                    {
                        var ns = string.IsNullOrEmpty(memberInfo.Namespace) ? "Global" : memberInfo.Namespace;

                        if (!namespaceDict.ContainsKey(ns))
                            namespaceDict[ns] = new Dictionary<string, List<MemberInfo>>();

                        if (!namespaceDict[ns].ContainsKey(memberInfo.ClassName))
                            namespaceDict[ns][memberInfo.ClassName] = new List<MemberInfo>();

                        namespaceDict[ns][memberInfo.ClassName].Add(memberInfo);
                    }
                }
            }

            // 生成C++代码
            var sb = new StringBuilder();
            foreach (var nsKvp in namespaceDict)
            {
                sb.AppendLine($"namespace {nsKvp.Key} {{");

                foreach (var classKvp in nsKvp.Value)
                {
                    sb.AppendLine($"    class __declspec(dllexport) {classKvp.Key} {{");
                    sb.AppendLine("    public:");

                    // 先输出构造函数
                    foreach (var member in classKvp.Value)
                    {
                        if (member.MemberType == "constructor")
                        {
                            sb.AppendLine($"        // 原型: {member.OriginalPrototype}");
                            sb.AppendLine($"        {member.ClassName}({member.Parameters}) {{");
                            sb.AppendLine($"            MessageBoxA(0, \"{member.ClassName}::{member.ClassName}\", \"\", 0);");
                            sb.AppendLine("        }");
                            sb.AppendLine();
                        }
                    }

                    // 然后输出析构函数
                    foreach (var member in classKvp.Value)
                    {
                        if (member.MemberType == "destructor")
                        {
                            sb.AppendLine($"        // 原型: {member.OriginalPrototype}");
                            var virtualKeyword = member.IsVirtual ? "virtual " : "";
                            sb.AppendLine($"        {virtualKeyword}~{member.ClassName}() {{");
                            sb.AppendLine($"            MessageBoxA(0, \"{member.ClassName}::~{member.ClassName}\", \"\", 0);");
                            sb.AppendLine("        }");
                            sb.AppendLine();
                        }
                    }

                    // 最后输出普通成员函数
                    foreach (var member in classKvp.Value)
                    {
                        if (member.MemberType == "function")
                        {
                            sb.AppendLine($"        // 原型: {member.OriginalPrototype}");
                            var virtualKeyword = member.IsVirtual ? "virtual " : "";
                            var staticKeyword = member.IsStatic ? "static " : "";
                            var constKeyword = member.IsConst ? " const" : "";
                            var returnType = string.IsNullOrEmpty(member.ReturnType) ? "void" : member.ReturnType;

                            sb.AppendLine($"        {staticKeyword}{virtualKeyword}{returnType} {member.MemberName}({member.Parameters}){constKeyword} {{");
                            sb.AppendLine($"            MessageBoxA(0, \"{member.ClassName}::{member.MemberName}\", \"\", 0);");
                            if (returnType != "void" && !returnType.Contains("*"))
                            {
                                sb.AppendLine($"            return {GetDefaultReturnValue(returnType)};");
                            }
                            sb.AppendLine("        }");
                            sb.AppendLine();
                        }
                    }

                    sb.AppendLine("    };");
                    sb.AppendLine();
                }

                sb.AppendLine("}");
                sb.AppendLine();
            }

            return sb.ToString();
        }

        private static string GetDefaultReturnValue(string returnType)
        {
            if (returnType.Contains("bool") || returnType.Contains("_N"))
                return "false";
            if (returnType.Contains("int") || returnType.Contains("long") || returnType.Contains("short"))
                return "0";
            if (returnType.Contains("*"))
                return "nullptr";
            return $"{returnType}()";
        }

        // 主要的重写方法
        public static string RewriteIATHijackCode(string originalCode)
        {
            // 1. 提取所有修饰名
            var mangledNames = ExtractMangledFunctions(originalCode);

            // 2. 删除原有的extern "C"函数定义
            var cleanedCode = RemoveMangledFunctions(originalCode);

            // 3. 生成新的C++类声明
            var cppClasses = GenerateCppClasses(mangledNames);

            // 4. 组合结果
            var result = new StringBuilder();
            result.Append(cleanedCode);
            result.AppendLine(cppClasses);


            return result.ToString();
        }
    }
}