using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Kill_free_toolbox.Helper.LoaderBuild
{
    partial class LoaderBbuild
    {
        /// <summary>
        /// 异步执行反优化程序，并捕获其输出。
        /// </summary>
        /// <param name="shellcodepath">需要处理的 shellcode 文件路径。</param>
        /// <param name="Arch">目标架构 (例如 "x64")。</param>
        /// <param name="i">重复执行的次数。</param>
        /// <returns>一个元组，包含是否成功 (Success) 以及程序的输出/错误信息 (Log)。</returns>
        public static Task<(bool Success, string Log)> deopAsync(string shellcodepath, string Arch, int i)
        {
            return Task.Run(() =>
            {
                string deopexepath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".exe");
                var logBuilder = new StringBuilder();

                try
                {
                    // 确保资源存在
                    if (Properties.Resources.deoptimizer == null)
                    {
                        return (false, "错误：资源 'deoptimizer' 未找到。");
                    }
                    File.WriteAllBytes(deopexepath, Properties.Resources.deoptimizer);

                    if (!File.Exists(deopexepath))
                    {
                        return (false, "错误：无法创建临时可执行文件。");
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = deopexepath,
                        Arguments = $"-a x86 -b {Arch} -f \"{shellcodepath}\" -o \"{shellcodepath}\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    // 串行执行 i 次，确保每次都执行完毕再进行下一次
                    for (int j = 0; j < i; j++)
                    {
                        logBuilder.AppendLine($"--- 第 {j + 1}/{i} 次执行 ---");

                        using (Process p = Process.Start(startInfo))
                        {
                            if (p == null)
                            {
                                logBuilder.AppendLine("错误：无法启动进程。");
                                continue;
                            }

                            // 读取标准输出和标准错误流
                            string output = p.StandardOutput.ReadToEnd();
                            string error = p.StandardError.ReadToEnd();

                            // 等待进程执行完毕
                            p.WaitForExit();

                            logBuilder.AppendLine($"退出码: {p.ExitCode}");
                            if (!string.IsNullOrWhiteSpace(output))
                            {
                                logBuilder.AppendLine("标准输出:");
                                logBuilder.AppendLine(output);
                            }
                            if (!string.IsNullOrWhiteSpace(error))
                            {
                                logBuilder.AppendLine("错误输出:");
                                logBuilder.AppendLine(error);
                            }

                            // 如果任何一次执行失败（退出码非0），则终止并报告失败
                            if (p.ExitCode != 0)
                            {
                                //MessageBox.Show(logBuilder.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                                return (false, logBuilder.ToString());
                            }
                        }
                    }
                    //MessageBox.Show(logBuilder.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return (true, logBuilder.ToString());
                }
                catch (Exception ex)
                {
                    // 捕获并记录详细异常信息
                    logBuilder.AppendLine($"发生严重异常: {ex.Message}");
                    logBuilder.AppendLine(ex.StackTrace);
                    //MessageBox.Show(logBuilder.ToString(), "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return (false, logBuilder.ToString());
                }
                finally
                {
                    try
                    {
                        if (File.Exists(deopexepath))
                            File.Delete(deopexepath);
                    }
                    catch (Exception ex)
                    {
                        // 记录删除文件时的异常，但不影响最终结果
                        Debug.WriteLine($"删除临时文件失败: {ex.Message}");
                    }
                }
            });
        }
    }
}