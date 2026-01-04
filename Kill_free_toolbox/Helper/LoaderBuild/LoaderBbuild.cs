using AsmResolver;
using AsmResolver.PE.File;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper
{
    class LoaderBbuild
    {
        static byte[] Combine(byte[] a1, byte[] a2)
        {
            byte[] ret = new byte[a1.Length + a2.Length];
            Array.Copy(a1, 0, ret, 0, a1.Length);
            Array.Copy(a2, 0, ret, a1.Length, a2.Length);
            return ret;
        }


        /// <summary>
        /// 创建 PE 文件。
        /// </summary>
        /// <param name="path">主要功能代码的文件路径 (将被膨胀)。</param>
        /// <param name="epOffset">入口点在主要功能代码内部的偏移量。</param>
        /// <param name="funcPaths">前置和后置 shellcode 的文件路径。</param>
        /// <param name="is64Bit">是否为 64 位。</param>
        /// <param name="imageBase">镜像基地址。</param>
        /// <returns>创建的 PEFile 对象。</returns>
        public static PEFile CreatePE(string path, uint epOffset, string[] funcPaths, bool is64Bit, ulong imageBase)
        {
            byte[] prefixShellcode = new byte[0];
            foreach (var funcPath in funcPaths)
            {
                prefixShellcode = Combine(prefixShellcode, File.ReadAllBytes(funcPath));
            }

            byte[] mainPayload = File.ReadAllBytes(path);

            byte[] suffixShellcode = new byte[0];
            foreach (var funcPath in funcPaths)
            {
                suffixShellcode = Combine(suffixShellcode, File.ReadAllBytes(funcPath));
            }

            // 组合最终的 shellcode
            byte[] finalShellcode = Combine(prefixShellcode, mainPayload);
            finalShellcode = Combine(finalShellcode, suffixShellcode);

            var pe = new PEFile();
            var text = new PESection(".text", SectionFlags.MemoryExecute | SectionFlags.MemoryRead);
            var pdata = new PESection(".pdata", SectionFlags.MemoryRead);
            var rdata = new PESection(".rdata", SectionFlags.MemoryRead);

            text.Contents = new DataSegment(finalShellcode);
            pdata.Contents = new DataSegment(Encoding.UTF8.GetBytes("This is a pdata section"));
            rdata.Contents = new DataSegment(Encoding.UTF8.GetBytes("This is a rdata section"));

            pe.Sections.Add(text);
            pe.Sections.Add(pdata);
            pe.Sections.Add(rdata);

            if (is64Bit)
            {
                pe.OptionalHeader.ImageBase = imageBase;
                pe.FileHeader.Machine = MachineType.Amd64;
                pe.FileHeader.Characteristics = Characteristics.Image
                                                | Characteristics.LocalSymsStripped
                                                | Characteristics.LineNumsStripped
                                                | Characteristics.RelocsStripped
                                                | Characteristics.LargeAddressAware;
                pe.OptionalHeader.Magic = OptionalHeaderMagic.PE64;
                pe.OptionalHeader.SubSystem = SubSystem.WindowsGui;
                pe.OptionalHeader.DllCharacteristics = DllCharacteristics.DynamicBase
                                                       | DllCharacteristics.NxCompat
                                                       | DllCharacteristics.TerminalServerAware;
            }
            else
            {
                pe.OptionalHeader.ImageBase = imageBase;
                pe.FileHeader.Machine = MachineType.I386;
                pe.FileHeader.Characteristics = Characteristics.Image
                                                | Characteristics.LocalSymsStripped
                                                | Characteristics.LineNumsStripped
                                                | Characteristics.RelocsStripped
                                                | Characteristics.Machine32Bit;
                pe.OptionalHeader.Magic = OptionalHeaderMagic.PE32;
                pe.OptionalHeader.SubSystem = SubSystem.WindowsGui;
                pe.OptionalHeader.DllCharacteristics = DllCharacteristics.DynamicBase
                                                       | DllCharacteristics.NxCompat
                                                       | DllCharacteristics.TerminalServerAware;
            }

            pe.UpdateHeaders();

            // 自动计算入口点：前置 shellcode 的长度 + 主要功能代码内部的偏移
            uint finalEpOffset = (uint)prefixShellcode.Length + epOffset;
            pe.OptionalHeader.AddressOfEntryPoint = text.Rva + finalEpOffset;

            return pe;
        }
    }
}