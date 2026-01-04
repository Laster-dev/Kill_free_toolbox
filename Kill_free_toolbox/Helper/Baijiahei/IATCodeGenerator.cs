using Kill_free_toolbox.Helper.MsvcDemangler;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Kill_free_toolbox.Helper.Baijiahei
{
    /// <summary>
    /// IAT劫持代码生成器
    /// </summary>
    public class IATCodeGenerator
    {
        /// <summary>
        /// 异步生成IAT劫持代码
        /// </summary>
        /// <param name="dllName">DLL名称</param>
        /// <param name="functions">函数列表</param>
        /// <param name="targetFileName">目标文件名</param>
        /// <returns>生成的代码</returns>
        public static async Task<string> GenerateIATHijackingCodeAsync(string dllName, List<string> functions, string targetFileName = "", CancellationToken cancellationToken = default)
        {
            return await Task.Run(() => GenerateIATHijackingCode(dllName, functions, targetFileName, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// 异步生成IAT劫持代码（带架构）
        /// </summary>
        public static async Task<string> GenerateIATHijackingCodeAsync(string dllName, List<string> functions, string targetFileName, bool is64Bit, CancellationToken cancellationToken)
        {
            return await Task.Run(() => GenerateIATHijackingCode(dllName, functions, targetFileName, is64Bit, true, cancellationToken), cancellationToken);
        }

        /// <summary>
        /// 生成IAT劫持代码
        /// </summary>
        /// <param name="dllName">DLL名称</param>
        /// <param name="functions">函数列表</param>
        /// <param name="targetFileName">目标文件名</param>
        /// <returns>生成的代码</returns>
        public static string GenerateIATHijackingCode(string dllName, List<string> functions, string targetFileName = "", CancellationToken cancellationToken = default)
        {
            if (functions == null || functions.Count == 0)
            {
                return "// 没有找到要劫持的函数";
            }

            var sb = new StringBuilder();

            // 添加头部注释
            sb.AppendLine($"// IAT劫持代码 - {dllName}");
            if (!string.IsNullOrEmpty(targetFileName))
            {
                sb.AppendLine($"// 目标文件: {Path.GetFileName(targetFileName)}");
            }
            sb.AppendLine($"// 函数数量: {functions.Count}");
            sb.AppendLine();

            // 生成头文件包含
            sb.AppendLine("#include <windows.h>");
            sb.AppendLine();

            // 使用MsvcDemangler解析原始函数并生成C++类结构
            var namespaceDict = new Dictionary<string, Dictionary<string, List<MemberInfo>>>();

            foreach (var function in functions)
            {
                if (cancellationToken.IsCancellationRequested) return "// 已取消";
                
                sb.AppendLine($"// 处理函数: {function}");
                
                // 解析原始函数原型
                if (MsvcDemanglerHelper.TryUndecorate(function, out var undecorated))
                {
                    sb.AppendLine($"// 解析结果: {undecorated}");
                    var memberInfo = IATHijackCodeRewriter.ParseCppPrototype(undecorated);
                    
                    //sb.AppendLine($"// 类名: {memberInfo.ClassName}, 命名空间: {memberInfo.Namespace}");
                    
                    if (!string.IsNullOrEmpty(memberInfo.ClassName))
                    {
                        var ns = string.IsNullOrEmpty(memberInfo.Namespace) ? "Global" : memberInfo.Namespace;
                        
                        if (!namespaceDict.ContainsKey(ns))
                            namespaceDict[ns] = new Dictionary<string, List<MemberInfo>>();
                        
                        if (!namespaceDict[ns].ContainsKey(memberInfo.ClassName))
                            namespaceDict[ns][memberInfo.ClassName] = new List<MemberInfo>();
                        
                        namespaceDict[ns][memberInfo.ClassName].Add(memberInfo);
                    }
                    else
                    {
                        sb.AppendLine("// 警告: 无法解析出类名");
                    }
                }
                else
                {
                    sb.AppendLine("// 警告: 无法解析函数原型");
                }
                sb.AppendLine();
            }

            // 检查是否解析出了类结构
            if (namespaceDict.Count == 0)
            {
                sb.AppendLine("// 警告: 无法解析出任何类结构，使用fallback方案");
                sb.AppendLine();

                // 生成简单的C函数作为fallback
                foreach (var function in functions)
                {
                    if (cancellationToken.IsCancellationRequested) return "// 已取消";

                    string stubName = MakeValidIdentifier(function);
                    sb.AppendLine($"// 原始函数: {function}");
                    sb.AppendLine($"extern \"C\" void {stubName}()");
                    sb.AppendLine("{");
                    sb.AppendLine($"    MessageBoxA(0, \"{EscapeString(function)}\", \"\", 0);");
                    sb.AppendLine("}");
                    sb.AppendLine();
                }
            }
            else
            {
                // 生成C++ namespace声明和实现
                foreach (var nsKvp in namespaceDict)
                {
                    // 使用类名作为namespace名
                    foreach (var classKvp in nsKvp.Value)
                    {
                        sb.AppendLine($"namespace {classKvp.Key} {{");

                        // 生成函数声明
                        foreach (var member in classKvp.Value)
                        {
                            if (member.MemberType == "function")
                            {
                                var returnType = string.IsNullOrEmpty(member.ReturnType) ? "void" : member.ReturnType;
                                sb.AppendLine($"    {returnType} __cdecl {member.MemberName}({member.Parameters});");
                            }
                        }

                        sb.AppendLine("}");
                        sb.AppendLine();
                    }
                }

                // 生成函数实现
                foreach (var nsKvp in namespaceDict)
                {
                    foreach (var classKvp in nsKvp.Value)
                    {
                        foreach (var member in classKvp.Value)
                        {
                            if (member.MemberType == "function")
                            {
                                var returnType = string.IsNullOrEmpty(member.ReturnType) ? "void" : member.ReturnType;

                                sb.AppendLine($"{returnType} __cdecl {classKvp.Key}::{member.MemberName}({member.Parameters})");
                                sb.AppendLine("{");
                                // 显示消息框
                                sb.AppendLine($"    MessageBoxA(0, \"{EscapeString(member.OriginalPrototype)}\", \"{EscapeString(classKvp.Key)}::{EscapeString(member.MemberName)}\", 0);");

                                // 生成默认返回值
                                if (returnType != "void")
                                {
                                    sb.AppendLine($"    return {GetDefaultReturnValue(returnType)};");
                                }
                                else
                                {
                                    sb.AppendLine($"    // {member.OriginalPrototype}");
                                }

                                sb.AppendLine("}");
                                sb.AppendLine();
                            }
                        }
                    }
                }

                // 生成DLL入口点
                sb.AppendLine("BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)");
                sb.AppendLine("{");
                sb.AppendLine("    return TRUE;");
                sb.AppendLine("}");
                sb.AppendLine();
            }
            return sb.ToString();
            
        }

        /// <summary>
        /// 生成IAT劫持代码（带架构）
        /// </summary>
        public static string GenerateIATHijackingCode(string dllName, List<string> functions, string targetFileName, bool is64Bit, bool forwardToOriginal = true, CancellationToken cancellationToken = default)
        {
            if (is64Bit)
            {
                var sb = new StringBuilder();
                sb.AppendLine($"// IAT劫持代码 - {dllName}");
                if (!string.IsNullOrEmpty(targetFileName))
                {
                    sb.AppendLine($"// 目标文件: {Path.GetFileName(targetFileName)}");
                }
                sb.AppendLine($"// 函数数量: {functions?.Count ?? 0}");
                if (forwardToOriginal)
                {
                    string originalDllName = Path.GetFileNameWithoutExtension(dllName) + "_old";
                    sb.AppendLine($"// 转发模式：请将原始DLL文件重命名为 {originalDllName}.dll");
                }
                sb.AppendLine();
                sb.AppendLine("#include <windows.h>");
                sb.AppendLine("#include <mutex>");
                sb.AppendLine("#include <fstream>");
                sb.AppendLine("#include <sstream>");
                sb.AppendLine("#include <iomanip>");
                sb.AppendLine("#include <string>");
                sb.AppendLine("#include <dbghelp.h>");
                sb.AppendLine("#pragma comment(lib, \"dbghelp.lib\")");
                sb.AppendLine();
                sb.AppendLine("// 全局变量");
                sb.AppendLine("static std::mutex g_logMutex;                    // 日志文件互斥锁");
                sb.AppendLine($"static std::string g_logPath = \"{Path.GetDirectoryName(targetFileName).Replace("\\", "\\\\")}\\\\hijack_log.txt\";  // 日志文件路径");
                sb.AppendLine("static bool g_initialized = false;               // 初始化标志");
                sb.AppendLine("#ifdef _DEBUG");
                sb.AppendLine("static bool g_enableLogging = true;              // 调试模式启用日志");
                sb.AppendLine("#else");
                sb.AppendLine("static bool g_enableLogging = false;             // 发布模式禁用日志");
                sb.AppendLine("#endif");
                sb.AppendLine();
                sb.AppendLine("// 写入日志文件");
                sb.AppendLine("void WriteLog(const std::string& message)");
                sb.AppendLine("{");
                sb.AppendLine("#ifdef _DEBUG");
                sb.AppendLine("    if (!g_enableLogging) return;");
                sb.AppendLine("#endif");
                sb.AppendLine("    std::lock_guard<std::mutex> lock(g_logMutex);");
                sb.AppendLine();
                sb.AppendLine("    std::ofstream logFile(g_logPath, std::ios::app);");
                sb.AppendLine("    if (logFile.is_open())");
                sb.AppendLine("    {");
                sb.AppendLine("        logFile << message << std::endl;");
                sb.AppendLine("        logFile.close();");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine("// 初始化日志系统");
                sb.AppendLine("void InitializeLogging()");
                sb.AppendLine("{");
                sb.AppendLine("    if (g_initialized) return;");
                sb.AppendLine();
                sb.AppendLine($"    std::string logDir = \"{Path.GetDirectoryName(targetFileName).Replace("\\", "\\\\")}\";");
                sb.AppendLine("    CreateDirectoryA(logDir.c_str(), NULL);");
                sb.AppendLine();
                sb.AppendLine("    std::ostringstream oss;");
                sb.AppendLine($"    oss << \"=== IAT劫持DLL已加载 - 目标程序: {Path.GetFileName(targetFileName)} - PID: \" << GetCurrentProcessId() << \" ===\";");
                sb.AppendLine("    WriteLog(oss.str());");
                sb.AppendLine();
                sb.AppendLine("    g_initialized = true;");
                sb.AppendLine("}");
                sb.AppendLine();
                sb.AppendLine("// 通用日志函数");
                sb.AppendLine("void Log(const char* functionName)");
                sb.AppendLine("{");
                sb.AppendLine("    std::ostringstream oss;");
                sb.AppendLine("    oss << \"函数被调用: \" << functionName;");
                sb.AppendLine("    WriteLog(oss.str());");
                sb.AppendLine("}");
                sb.AppendLine();
                if (!forwardToOriginal)
                {
                    sb.AppendLine("// 劫持特定函数的处理逻辑");
                    sb.AppendLine("void FunctionCall(const char* functionName)");
                    sb.AppendLine("{");
                    sb.AppendLine("    Log(functionName);");
                    sb.AppendLine("    const char* targetFunction = \"这里写你想劫持的函数名称，从日志查看\";");
                    sb.AppendLine("    //这里写你想劫持的函数名称，从日志查看");
                    sb.AppendLine("    if (strcmp(functionName, targetFunction) == 0)");
                    sb.AppendLine("    {");
                    sb.AppendLine("        // 在这里添加你想要的特殊处理代码");
                    sb.AppendLine("        WriteLog(\"劫持成功：\" + std::string(functionName));");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");
                }
                sb.AppendLine();
                if (!forwardToOriginal)
                {
                    sb.AppendLine("// 宏定义批量生成 Stub 函数");
                    sb.AppendLine("#define DEFINE_STUB(funcName) \\");
                    sb.AppendLine("extern \"C\" void funcName() \\");
                    sb.AppendLine("{ \\");
                    sb.AppendLine("    FunctionCall(#funcName); \\");
                    sb.AppendLine("}");
                }
                sb.AppendLine();
                sb.AppendLine("#include \"STUB.h\"");
                sb.AppendLine();
                sb.AppendLine("BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved)");
                sb.AppendLine("{");
                sb.AppendLine("    switch (ul_reason_for_call)");
                sb.AppendLine("    {");
                sb.AppendLine("    case DLL_PROCESS_ATTACH:");
                sb.AppendLine("        InitializeLogging();");
                sb.AppendLine("        WriteLog(\"DLL_PROCESS_ATTACH - DLL已附加到进程 - PID: \" + std::to_string(GetCurrentProcessId()));");
                sb.AppendLine("        break;");
                sb.AppendLine("    case DLL_THREAD_ATTACH:");
                sb.AppendLine("        WriteLog(\"DLL_THREAD_ATTACH - 新线程已附加 - PID: \" + std::to_string(GetCurrentProcessId()));");
                sb.AppendLine("        break;");
                sb.AppendLine("    case DLL_THREAD_DETACH:");
                sb.AppendLine("        WriteLog(\"DLL_THREAD_DETACH - 线程即将分离 - PID: \" + std::to_string(GetCurrentProcessId()));");
                sb.AppendLine("        break;");
                sb.AppendLine("    case DLL_PROCESS_DETACH:");
                sb.AppendLine("        WriteLog(\"DLL_PROCESS_DETACH - DLL即将从进程分离 - PID: \" + std::to_string(GetCurrentProcessId()));");
                sb.AppendLine("        WriteLog(\"=== IAT劫持DLL已卸载 ===\\n\");");
                sb.AppendLine("        break;");
                sb.AppendLine("    }");
                sb.AppendLine("    return TRUE;");
                sb.AppendLine("}");
                sb.AppendLine();
                return sb.ToString();
            }
            // x86 走原有解析路径
            return GenerateIATHijackingCode(dllName, functions, targetFileName, cancellationToken);
        }

        private static bool IsValidCIdentifier(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (!(char.IsLetter(name[0]) || name[0] == '_')) return false;
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            }
            return true;
        }

        private static string MakeValidIdentifier(string name)
        {
            if (string.IsNullOrEmpty(name)) return "_f";
            var sb = new StringBuilder();
            if (char.IsLetter(name[0]) || name[0] == '_') sb.Append(name[0]);
            else sb.Append('_');
            for (int i = 1; i < name.Length; i++)
            {
                char c = name[i];
                sb.Append(char.IsLetterOrDigit(c) || c == '_' ? c : '_');
            }
            return sb.ToString();
        }

        private static string EscapeString(string s)
        {
            return (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static string EscapePragma(string s)
        {
            // pragma 内容不需要额外引号，但要转义双引号本身（我们不包含），保留原始名称
            return (s ?? string.Empty).Replace("\"", "");
        }

        /// <summary>
        /// 生成DEF文件内容
        /// </summary>
        /// <param name="functions">函数列表</param>
        /// <returns>DEF文件内容</returns>
        public static string GenerateDefFileContent(List<string> functions)
        {
            if (functions == null || functions.Count == 0)
            {
                return "EXPORTS";
            }

            var sb = new StringBuilder();
            sb.AppendLine("EXPORTS");
            foreach (var function in functions)
            {
                sb.AppendLine($"{function}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 生成vcxproj项目文件内容
        /// </summary>
        /// <param name="dllName">DLL名称</param>
        /// <param name="toolset">平台工具集，例如 v142/v143</param>
        /// <param name="platform">平台：Win32 或 x64</param>
        /// <param name="projectGuid">项目GUID，形如 {XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}</param>
        /// <returns>项目文件内容</returns>
        public static string GenerateProjectFileContent(string dllName, string toolset, string platform, string projectGuid, bool includeUnlook = false, bool forwardToOriginal = true)
        {
            var sb = new StringBuilder();
            string projectName = Path.GetFileNameWithoutExtension(dllName);
            sb.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.AppendLine("<Project DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
            sb.AppendLine("  <ItemGroup Label=\"ProjectConfigurations\">");
            sb.AppendLine($"    <ProjectConfiguration Include=\"Debug|{platform}\">");
            sb.AppendLine("      <Configuration>Debug</Configuration>");
            sb.AppendLine($"      <Platform>{platform}</Platform>");
            sb.AppendLine("    </ProjectConfiguration>");
            sb.AppendLine($"    <ProjectConfiguration Include=\"Release|{platform}\">");
            sb.AppendLine("      <Configuration>Release</Configuration>");
            sb.AppendLine($"      <Platform>{platform}</Platform>");
            sb.AppendLine("    </ProjectConfiguration>");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("  <PropertyGroup Label=\"Globals\">");
            sb.AppendLine("    <VCProjectVersion>16.0</VCProjectVersion>");
            sb.AppendLine($"    <ProjectGuid>{projectGuid}</ProjectGuid>");
            sb.AppendLine("    <Keyword>Win32Proj</Keyword>");
            sb.AppendLine($"    <RootNamespace>{projectName}</RootNamespace>");
            sb.AppendLine("    <WindowsTargetPlatformVersion>10.0</WindowsTargetPlatformVersion>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.Default.props\" />");
            sb.AppendLine($"  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|{platform}'\" Label=\"Configuration\">");
            sb.AppendLine("    <ConfigurationType>DynamicLibrary</ConfigurationType>");
            sb.AppendLine("    <UseDebugLibraries>true</UseDebugLibraries>");
            sb.AppendLine($"    <PlatformToolset>{toolset}</PlatformToolset>");
            sb.AppendLine("    <CharacterSet>Unicode</CharacterSet>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine($"  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|{platform}'\" Label=\"Configuration\">");
            sb.AppendLine("    <ConfigurationType>DynamicLibrary</ConfigurationType>");
            sb.AppendLine("    <UseDebugLibraries>false</UseDebugLibraries>");
            sb.AppendLine($"    <PlatformToolset>{toolset}</PlatformToolset>");
            sb.AppendLine("    <WholeProgramOptimization>true</WholeProgramOptimization>");
            sb.AppendLine("    <CharacterSet>Unicode</CharacterSet>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.props\" />");
            sb.AppendLine("  <ImportGroup Label=\"ExtensionSettings\">");
            sb.AppendLine("  </ImportGroup>");
            sb.AppendLine("  <ImportGroup Label=\"Shared\">");
            sb.AppendLine("  </ImportGroup>");
            sb.AppendLine($"  <ImportGroup Label=\"PropertySheets\" Condition=\"'$(Configuration)|$(Platform)'=='Debug|{platform}'\">");
            sb.AppendLine("    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />");
            sb.AppendLine("  </ImportGroup>");
            sb.AppendLine($"  <ImportGroup Label=\"PropertySheets\" Condition=\"'$(Configuration)|$(Platform)'=='Release|{platform}'\">");
            sb.AppendLine("    <Import Project=\"$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props\" Condition=\"exists('$(UserRootDir)\\Microsoft.Cpp.$(Platform).user.props')\" Label=\"LocalAppDataPlatform\" />");
            sb.AppendLine("  </ImportGroup>");
            sb.AppendLine("  <PropertyGroup Label=\"UserMacros\" />");
            sb.AppendLine($"  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|{platform}'\">");
            sb.AppendLine("    <LinkIncremental>true</LinkIncremental>");
            sb.AppendLine("    <OutDir>$(SolutionDir)$(Platform)\\$(Configuration)\\</OutDir>");
            sb.AppendLine("    <IntDir>$(Platform)\\$(Configuration)\\</IntDir>");
            sb.AppendLine($"    <TargetName>{projectName}</TargetName>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine($"  <PropertyGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|{platform}'\">");
            sb.AppendLine("    <LinkIncremental>false</LinkIncremental>");
            sb.AppendLine("    <OutDir>$(SolutionDir)$(Platform)\\$(Configuration)\\</OutDir>");
            sb.AppendLine("    <IntDir>$(Platform)\\$(Configuration)\\</IntDir>");
            sb.AppendLine($"    <TargetName>{projectName}</TargetName>");
            sb.AppendLine("  </PropertyGroup>");
            sb.AppendLine($"  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Debug|{platform}'\">");
            sb.AppendLine("    <ClCompile>");
            sb.AppendLine("      <WarningLevel>TurnOffAllWarnings</WarningLevel>");
            sb.AppendLine("      <SDLCheck>false</SDLCheck>");
            sb.AppendLine("      <PreprocessorDefinitions>_DEBUG;IATHIJACK_EXPORTS;_WINDOWS;_USRDLL;%(PreprocessorDefinitions)</PreprocessorDefinitions>");
            sb.AppendLine("      <ConformanceMode>true</ConformanceMode>");
            sb.AppendLine("    </ClCompile>");
            sb.AppendLine("    <Link>");
            sb.AppendLine("      <SubSystem>Windows</SubSystem>");
            sb.AppendLine("      <GenerateDebugInformation>true</GenerateDebugInformation>");
            sb.AppendLine("      <EnableUAC>false</EnableUAC>");
            if (!forwardToOriginal)
            {
                sb.AppendLine($"      <ModuleDefinitionFile>{projectName}.def</ModuleDefinitionFile>");
            }
            sb.AppendLine("    </Link>");
            sb.AppendLine("  </ItemDefinitionGroup>");
            sb.AppendLine($"  <ItemDefinitionGroup Condition=\"'$(Configuration)|$(Platform)'=='Release|{platform}'\">");
            sb.AppendLine("    <ClCompile>");
            sb.AppendLine("      <WarningLevel>TurnOffAllWarnings</WarningLevel>");
            sb.AppendLine("      <FunctionLevelLinking>true</FunctionLevelLinking>");
            sb.AppendLine("      <IntrinsicFunctions>true</IntrinsicFunctions>");
            sb.AppendLine("      <SDLCheck>false</SDLCheck>");
            sb.AppendLine("      <PreprocessorDefinitions>NDEBUG;IATHIJACK_EXPORTS;_WINDOWS;_USRDLL;%(PreprocessorDefinitions)</PreprocessorDefinitions>");
            sb.AppendLine("      <ConformanceMode>true</ConformanceMode>");
            sb.AppendLine("    </ClCompile>");
            sb.AppendLine("    <Link>");
            sb.AppendLine("      <SubSystem>Windows</SubSystem>");
            sb.AppendLine("      <EnableCOMDATFolding>true</EnableCOMDATFolding>");
            sb.AppendLine("      <OptimizeReferences>true</OptimizeReferences>");
            sb.AppendLine("      <GenerateDebugInformation>true</GenerateDebugInformation>");
            sb.AppendLine("      <EnableUAC>false</EnableUAC>");
            if (!forwardToOriginal)
            {
                sb.AppendLine($"      <ModuleDefinitionFile>{projectName}.def</ModuleDefinitionFile>");
            }
            sb.AppendLine("    </Link>");
            sb.AppendLine("  </ItemDefinitionGroup>");
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine($"    <ClCompile Include=\"{projectName}.cpp\" />");
            if (!forwardToOriginal)
            {
                sb.AppendLine($"    <None Include=\"{projectName}.def\" />");
            }
            sb.AppendLine("  </ItemGroup>");
            if (includeUnlook)
            {
                sb.AppendLine("  <ItemGroup>");
                sb.AppendLine("    <ClInclude Include=\"UNLOOK.h\" />");
                sb.AppendLine("  </ItemGroup>");
            }
            sb.AppendLine("  <ItemGroup>");
            sb.AppendLine("    <ClInclude Include=\"STUB.h\" />");
            sb.AppendLine("  </ItemGroup>");
            sb.AppendLine("  <Import Project=\"$(VCTargetsPath)\\Microsoft.Cpp.targets\" />");
            sb.AppendLine("  <ImportGroup Label=\"ExtensionTargets\">");
            sb.AppendLine("  </ImportGroup>");
            sb.AppendLine("</Project>");
            
            return sb.ToString();
        }

        /// <summary>
        /// 生成默认的IATHijack.cpp源码内容
        /// </summary>
        public static string GenerateCppFileContent(string dllName, List<string> functions, string targetFileName, bool includeUnlook)
        {
            var code = GenerateIATHijackingCode(dllName, functions, targetFileName);
            if (includeUnlook)
            {
                code = EnsureUnlookHeaderIncluded(code);
                code = RewriteDllMainWithUnlook(code);
            }
            return code;
        }

        /// <summary>
        /// 生成默认的IATHijack.cpp源码内容（带架构）
        /// </summary>
        public static string GenerateCppFileContent(string dllName, List<string> functions, string targetFileName, bool is64Bit, bool includeUnlook = false, bool forwardToOriginal = true)
        {
            var code = GenerateIATHijackingCode(dllName, functions, targetFileName, is64Bit, forwardToOriginal);
            if (includeUnlook)
            {
                code = EnsureUnlookHeaderIncluded(code);
                code = RewriteDllMainWithUnlook(code);
            }
            return code;
        }

        private static string EnsureUnlookHeaderIncluded(string code)
        {
            var includeLine = "#include \"UNLOOK.h\"";
            if (code.Contains(includeLine)) return code;
            // insert after windows.h include if present, otherwise at top
            var windowsIdx = code.IndexOf("#include <windows.h>", StringComparison.OrdinalIgnoreCase);
            if (windowsIdx >= 0)
            {
                var lineEnd = code.IndexOf('\n', windowsIdx);
                if (lineEnd >= 0)
                {
                    return code.Insert(lineEnd + 1, includeLine + "\n\n");
                }
            }
            return includeLine + "\n\n" + code;
        }

        private static string RewriteDllMainWithUnlook(string code)
        {
            var newDllMain =
                "BOOL APIENTRY DllMain(HMODULE hModule, DWORD  ul_reason_for_call, LPVOID lpReserved)\r\n" +
                "{\r\n" +
                "    switch (ul_reason_for_call)\r\n" +
                "    {\r\n" +
                "    case DLL_PROCESS_ATTACH:\r\n" +
                "        UNLOOK();\r\n\r\n" +
                "        MessageBoxA(NULL, \"已解决死锁并劫持\", \"Info\", MB_OK);\r\n" +
                "        //在这里直接可以加载你的shellcode上线了;\r\n" +
                "        exit(0);//避免后续代码影响\r\n" +
                "    }\r\n" +
                "    return TRUE;\r\n" +
                "}\r\n";

            // Replace any existing DllMain block regardless of whitespace/newlines
            var regex = new System.Text.RegularExpressions.Regex(
                @"BOOL\s+APIENTRY\s+DllMain\s*\([^\)]*\)\s*\{[\s\S]*?\}",
                System.Text.RegularExpressions.RegexOptions.Singleline);
            if (regex.IsMatch(code))
            {
                return regex.Replace(code, newDllMain);
            }
            // If not found, append a new DllMain at the end
            return code + "\r\n" + newDllMain;
        }

        /// <summary>
        /// 生成.def文件内容
        /// </summary>
        /// <param name="dllName">DLL名称</param>
        /// <param name="functions">函数列表</param>
        /// <returns>.def文件内容</returns>
        public static string GenerateDefFileContent(string dllName, List<string> functions)
        {
            if (functions == null || functions.Count == 0)
            {
                return "EXPORTS\n";
            }

            var sb = new StringBuilder();
            sb.AppendLine("LIBRARY");
            sb.AppendLine("EXPORTS");
            
            foreach (var function in functions)
            {
                sb.AppendLine($"    {function}");
            }
            
            return sb.ToString();
        }

        /// <summary>
        /// 生成.def文件内容（带架构）
        /// </summary>
        public static string GenerateDefFileContent(string dllName, List<string> functions, bool is64Bit, bool forwardToOriginal = true)
        {
            if (functions == null || functions.Count == 0)
            {
                return "EXPORTS\n";
            }

            var sb = new StringBuilder();
            sb.AppendLine("LIBRARY");
            sb.AppendLine("EXPORTS");
            foreach (var function in functions)
            {
                if (is64Bit)
                {
                    if (forwardToOriginal)
                    {
                        // 转发到原始DLL：函数名=dll_old.dll.函数名
                        string originalDllName = Path.GetFileNameWithoutExtension(dllName) + "_old.dll";
                        sb.AppendLine($"    {function}={originalDllName}.{function}");
                    }
                    else
                    {
                        // 不转发，使用带Func_前缀的有效C++函数名
                        string validFuncName = "Func_" + MakeValidIdentifier(function);
                        sb.AppendLine($"    {function}={validFuncName}");
                    }
                }
                else
                {
                    sb.AppendLine($"    {function}");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// 获取默认返回值
        /// </summary>
        /// <param name="returnType">返回类型</param>
        /// <returns>默认返回值</returns>
        private static string GetDefaultReturnValue(string returnType)
        {
            if (returnType.Contains("bool") || returnType.Contains("_N"))
                return "false";
            if (returnType.Contains("int") || returnType.Contains("long") || returnType.Contains("short"))
                return "0";
            if (returnType.Contains("float") || returnType.Contains("double"))
                return "0.0";
            if (returnType.Contains("*"))
                return "nullptr";
            if (returnType.Contains("void"))
                return "";
            return $"{returnType}()";
        }

        /// <summary>
        /// 生成STUB.h头文件内容
        /// </summary>
        public static string GenerateStubHeaderContent(List<string> functions, bool forwardToOriginal = true, string dllName = "")
        {
            var sb = new StringBuilder();
            sb.AppendLine("#pragma once");
            sb.AppendLine();
            
            if (forwardToOriginal)
            {
                // 转发模式：使用pragma comment定义转发
                string originalDllName = Path.GetFileNameWithoutExtension(dllName) + "_old";
                sb.AppendLine("// 使用pragma comment定义转发到原始DLL");
                sb.AppendLine($"// 注意：请将原始DLL文件重命名为 {originalDllName}.dll 以确保转发正常工作");
                sb.AppendLine();
                foreach (var function in functions)
                {
                    sb.AppendLine($"#pragma comment(linker, \"/export:{function}={originalDllName}.dll.{function}\")");
                }
            }
            else
            {
                // 非转发模式：生成有效的C++函数名，并添加Func_前缀
                sb.AppendLine("// 批量定义所有函数的 Stub");
                foreach (var function in functions)
                {
                    string validFuncName = "Func_" + MakeValidIdentifier(function);
                    sb.AppendLine($"DEFINE_STUB({validFuncName})");
                }
            }
            return sb.ToString();
        }
    }
}
