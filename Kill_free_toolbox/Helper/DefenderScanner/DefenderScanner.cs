using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper.DefenderScanner
{
    public class DefenderScanner
    {
        public event Action<string> OnOutput;
        public event Action<string> OnStatusChanged;
        public event Action<bool> OnScanCompleted;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _debugMode;

        public async Task StartScanAsync(string targetFile, bool debugMode = false)
        {
            _debugMode = debugMode;
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                await Task.Run(() => PerformScan(targetFile, _cancellationTokenSource.Token));
            }
            catch (OperationCanceledException)
            {
                OnOutput?.Invoke("扫描已被用户取消。\n");
                OnStatusChanged?.Invoke("已取消");
            }
            catch (Exception ex)
            {
                OnOutput?.Invoke($"扫描过程中发生错误: {ex.Message}\n");
                OnStatusChanged?.Invoke("错误");
            }
            finally
            {
                OnScanCompleted?.Invoke(true);
            }
        }

        public void StopScan()
        {
            _cancellationTokenSource?.Cancel();
        }

        private void PerformScan(string targetFile, CancellationToken cancellationToken)
        {
            if (!File.Exists(targetFile))
            {
                OnOutput?.Invoke("[-] 无法访问目标文件\n");
                OnStatusChanged?.Invoke("文件不存在");
                return;
            }

            OnOutput?.Invoke($"开始扫描文件: {Path.GetFileName(targetFile)}\n");
            OnStatusChanged?.Invoke("正在进行初始扫描...");

            string originalFileDetectionStatus = Scan(targetFile).ToString();
            if (originalFileDetectionStatus.Equals("NoThreatFound"))
            {
                if (_debugMode) OnOutput?.Invoke("首次扫描整个文件\n");
                OnOutput?.Invoke("[+] 提交的文件中未发现威胁！\n");
                OnStatusChanged?.Invoke("未发现威胁");
                return;
            }

            OnOutput?.Invoke("[-] 文件被检测为威胁，开始分析具体位置...\n");

            if (!Directory.Exists(@"C:\Temp"))
            {
                OnOutput?.Invoke(@"[-] C:\Temp 不存在，正在创建...\n");
                Directory.CreateDirectory(@"C:\Temp");
            }

            string testfilepath = @"C:\Temp\testfile.exe";
            byte[] originalfilecontents = File.ReadAllBytes(targetFile);
            int originalfilesize = originalfilecontents.Length;

            OnOutput?.Invoke($"目标文件大小: {originalfilecontents.Length} 字节\n");
            OnOutput?.Invoke("正在分析...\n\n");
            OnStatusChanged?.Invoke("正在分析威胁位置...");

            byte[] splitarray1 = new byte[originalfilesize / 2];
            Buffer.BlockCopy(originalfilecontents, 0, splitarray1, 0, originalfilecontents.Length / 2);
            int lastgood = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                if (_debugMode) OnOutput?.Invoke($"测试 {splitarray1.Length} 字节\n");

                File.WriteAllBytes(testfilepath, splitarray1);
                string detectionStatus = Scan(testfilepath).ToString();

                if (detectionStatus.Equals("ThreatFound"))
                {
                    if (_debugMode) OnOutput?.Invoke("发现威胁，继续二分查找...\n");
                    byte[] temparray = HalfSplitter(splitarray1, lastgood, testfilepath);
                    if (temparray == null) break; // 找到了具体位置
                    Array.Resize(ref splitarray1, temparray.Length);
                    Array.Copy(temparray, splitarray1, temparray.Length);
                }
                else if (detectionStatus.Equals("NoThreatFound"))
                {
                    if (_debugMode) OnOutput?.Invoke("未发现威胁，增加50%的当前大小。\n");
                    lastgood = splitarray1.Length;
                    byte[] temparray = Overshot(originalfilecontents, splitarray1.Length);
                    if (temparray == null) break; // 搜索完毕
                    Array.Resize(ref splitarray1, temparray.Length);
                    Buffer.BlockCopy(temparray, 0, splitarray1, 0, temparray.Length);
                }
            }

            // 清理临时文件
            if (File.Exists(testfilepath))
            {
                File.Delete(testfilepath);
            }
        }

        private byte[] HalfSplitter(byte[] originalarray, int lastgood, string testfilepath)
        {
            byte[] splitarray = new byte[(originalarray.Length - lastgood) / 2 + lastgood];
            if (originalarray.Length == splitarray.Length + 1)
            {
                OnOutput?.Invoke($"[!] 在原文件偏移 0x{originalarray.Length:X} 处识别出恶意字节的结束位置\n");
                OnStatusChanged?.Invoke("正在获取威胁签名...");

                Scan(testfilepath, true);
                byte[] offendingBytes = new byte[256];

                if (originalarray.Length < 256)
                {
                    Array.Resize(ref offendingBytes, originalarray.Length);
                    Buffer.BlockCopy(originalarray, 0, offendingBytes, 0, originalarray.Length);
                }
                else
                {
                    Buffer.BlockCopy(originalarray, originalarray.Length - 256, offendingBytes, 0, 256);
                }

                OnOutput?.Invoke("\n恶意字节的十六进制转储:\n");
                HexDump(offendingBytes, 16);
                OnStatusChanged?.Invoke("分析完成");
                return null;
            }
            Array.Copy(originalarray, splitarray, splitarray.Length);
            return splitarray;
        }

        private byte[] Overshot(byte[] originalarray, int splitarraysize)
        {
            int newsize = (originalarray.Length - splitarraysize) / 2 + splitarraysize;
            if (newsize.Equals(originalarray.Length - 1))
            {
                OnOutput?.Invoke("搜索完毕。该二进制文件看起来可以正常使用！\n");
                OnStatusChanged?.Invoke("搜索完毕");
                return null;
            }
            byte[] newarray = new byte[newsize];
            Buffer.BlockCopy(originalarray, 0, newarray, 0, newarray.Length);
            return newarray;
        }

        private ScanResult Scan(string file, bool getsig = false)
        {
            if (!File.Exists(file))
            {
                return ScanResult.FileNotFound;
            }

            var process = new Process();
            var mpcmdrun = new ProcessStartInfo(@"C:\Program Files\Windows Defender\MpCmdRun.exe")
            {
                Arguments = $"-Scan -ScanType 3 -File \"{file}\" -DisableRemediation -Trace -Level 0x10",
                CreateNoWindow = true,
                ErrorDialog = false,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            process.StartInfo = mpcmdrun;
            process.Start();
            process.WaitForExit(30000); // 等待30秒

            if (!process.HasExited)
            {
                process.Kill();
                return ScanResult.Timeout;
            }

            if (getsig)
            {
                string stdout;
                while ((stdout = process.StandardOutput.ReadLine()) != null)
                {
                    if (stdout.Contains("Threat  "))
                    {
                        string[] sig = stdout.Split(' ');
                        if (sig.Length > 19)
                        {
                            string sigName = sig[19];
                            OnOutput?.Invoke($"文件匹配签名: \"{sigName}\"\n\n");
                        }
                        break;
                    }
                }
            }

            switch (process.ExitCode)
            {
                case 0:
                    return ScanResult.NoThreatFound;
                case 2:
                    return ScanResult.ThreatFound;
                default:
                    return ScanResult.Error;
            }
        }

        private void HexDump(byte[] bytes, int bytesPerLine = 16)
        {
            if (bytes == null)
            {
                OnOutput?.Invoke("[-] 提供了空数组。出现了问题...\n");
                return;
            }

            int bytesLength = bytes.Length;
            char[] HexChars = "0123456789ABCDEF".ToCharArray();

            int firstHexColumn = 8 + 3; // 8个字符用于地址 + 3个空格
            int firstCharColumn = firstHexColumn + bytesPerLine * 3 + (bytesPerLine - 1) / 8 + 2;
            int lineLength = firstCharColumn + bytesPerLine + Environment.NewLine.Length;

            char[] line = (new String(' ', lineLength - Environment.NewLine.Length) + Environment.NewLine).ToCharArray();
            int expectedLines = (bytesLength + bytesPerLine - 1) / bytesPerLine;
            StringBuilder result = new StringBuilder(expectedLines * lineLength);

            for (int i = 0; i < bytesLength; i += bytesPerLine)
            {
                line[0] = HexChars[(i >> 28) & 0xF];
                line[1] = HexChars[(i >> 24) & 0xF];
                line[2] = HexChars[(i >> 20) & 0xF];
                line[3] = HexChars[(i >> 16) & 0xF];
                line[4] = HexChars[(i >> 12) & 0xF];
                line[5] = HexChars[(i >> 8) & 0xF];
                line[6] = HexChars[(i >> 4) & 0xF];
                line[7] = HexChars[(i >> 0) & 0xF];

                int hexColumn = firstHexColumn;
                int charColumn = firstCharColumn;

                for (int j = 0; j < bytesPerLine; j++)
                {
                    if (j > 0 && (j & 7) == 0) hexColumn++;
                    if (i + j >= bytesLength)
                    {
                        line[hexColumn] = ' ';
                        line[hexColumn + 1] = ' ';
                        line[charColumn] = ' ';
                    }
                    else
                    {
                        byte b = bytes[i + j];
                        line[hexColumn] = HexChars[(b >> 4) & 0xF];
                        line[hexColumn + 1] = HexChars[b & 0xF];
                        line[charColumn] = (b < 32 ? '·' : (char)b);
                    }
                    hexColumn += 3;
                    charColumn++;
                }
                result.Append(line);
            }

            OnOutput?.Invoke(result.ToString());
        }
    }

    public enum ScanResult
    {
        [Description("未发现威胁")]
        NoThreatFound,
        [Description("发现威胁")]
        ThreatFound,
        [Description("找不到文件")]
        FileNotFound,
        [Description("超时")]
        Timeout,
        [Description("错误")]
        Error
    }
}
