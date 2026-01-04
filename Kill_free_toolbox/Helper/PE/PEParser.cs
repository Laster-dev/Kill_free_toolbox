using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Kill_free_toolbox.Helper.PE
{
    #region PE文件结构定义

    // DOS头结构
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DOS_HEADER
    {
        public ushort e_magic;       // Magic number (0x5A4D "MZ")
        public ushort e_cblp;        // Bytes on last page of file
        public ushort e_cp;          // Pages in file
        public ushort e_crlc;        // Relocations
        public ushort e_cparhdr;     // Size of header in paragraphs
        public ushort e_minalloc;    // Minimum extra paragraphs needed
        public ushort e_maxalloc;    // Maximum extra paragraphs needed
        public ushort e_ss;          // Initial (relative) SS value
        public ushort e_sp;          // Initial SP value
        public ushort e_csum;        // Checksum
        public ushort e_ip;          // Initial IP value
        public ushort e_cs;          // Initial (relative) CS value
        public ushort e_lfarlc;      // File address of relocation table
        public ushort e_ovno;        // Overlay number
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] e_res1;      // Reserved words
        public ushort e_oemid;       // OEM identifier (for e_oeminfo)
        public ushort e_oeminfo;     // OEM information; e_oemid specific
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        public ushort[] e_res2;      // Reserved words
        public int e_lfanew;         // File address of new exe header
    }

    // 文件头结构
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_FILE_HEADER
    {
        public ushort Machine;               // 目标机器类型
        public ushort NumberOfSections;      // 节数量
        public uint TimeDateStamp;           // 创建时间戳
        public uint PointerToSymbolTable;    // 符号表指针
        public uint NumberOfSymbols;         // 符号数量
        public ushort SizeOfOptionalHeader;  // 可选头大小
        public ushort Characteristics;       // 文件特性
    }

    // 数据目录结构
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_DATA_DIRECTORY
    {
        public uint VirtualAddress;  // RVA
        public uint Size;            // 大小
    }

    // 32位可选头
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_OPTIONAL_HEADER32
    {
        public ushort Magic;                     // 魔数 (0x10B)
        public byte MajorLinkerVersion;          // 链接器主版本
        public byte MinorLinkerVersion;          // 链接器次版本
        public uint SizeOfCode;                  // 代码段大小
        public uint SizeOfInitializedData;       // 已初始化数据大小
        public uint SizeOfUninitializedData;     // 未初始化数据大小
        public uint AddressOfEntryPoint;         // 入口点RVA
        public uint BaseOfCode;                  // 代码基址
        public uint BaseOfData;                  // 数据基址
        public uint ImageBase;                   // 映像基址
        public uint SectionAlignment;            // 节对齐
        public uint FileAlignment;               // 文件对齐
        public ushort MajorOperatingSystemVersion;   // 操作系统主版本
        public ushort MinorOperatingSystemVersion;   // 操作系统次版本
        public ushort MajorImageVersion;         // 映像主版本
        public ushort MinorImageVersion;         // 映像次版本
        public ushort MajorSubsystemVersion;     // 子系统主版本
        public ushort MinorSubsystemVersion;     // 子系统次版本
        public uint Win32VersionValue;           // Win32版本
        public uint SizeOfImage;                 // 映像大小
        public uint SizeOfHeaders;               // 头部大小
        public uint CheckSum;                    // 校验和
        public ushort Subsystem;                 // 子系统
        public ushort DllCharacteristics;        // DLL特性
        public uint SizeOfStackReserve;          // 栈保留大小
        public uint SizeOfStackCommit;           // 栈提交大小
        public uint SizeOfHeapReserve;           // 堆保留大小
        public uint SizeOfHeapCommit;            // 堆提交大小
        public uint LoaderFlags;                 // 加载器标志
        public uint NumberOfRvaAndSizes;         // 数据目录项数量
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public IMAGE_DATA_DIRECTORY[] DataDirectory;  // 数据目录
    }

    // 64位可选头
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_OPTIONAL_HEADER64
    {
        public ushort Magic;                     // 魔数 (0x20B)
        public byte MajorLinkerVersion;          // 链接器主版本
        public byte MinorLinkerVersion;          // 链接器次版本
        public uint SizeOfCode;                  // 代码段大小
        public uint SizeOfInitializedData;       // 已初始化数据大小
        public uint SizeOfUninitializedData;     // 未初始化数据大小
        public uint AddressOfEntryPoint;         // 入口点RVA
        public uint BaseOfCode;                  // 代码基址
        public ulong ImageBase;                  // 映像基址 (64位)
        public uint SectionAlignment;            // 节对齐
        public uint FileAlignment;               // 文件对齐
        public ushort MajorOperatingSystemVersion;   // 操作系统主版本
        public ushort MinorOperatingSystemVersion;   // 操作系统次版本
        public ushort MajorImageVersion;         // 映像主版本
        public ushort MinorImageVersion;         // 映像次版本
        public ushort MajorSubsystemVersion;     // 子系统主版本
        public ushort MinorSubsystemVersion;     // 子系统次版本
        public uint Win32VersionValue;           // Win32版本
        public uint SizeOfImage;                 // 映像大小
        public uint SizeOfHeaders;               // 头部大小
        public uint CheckSum;                    // 校验和
        public ushort Subsystem;                 // 子系统
        public ushort DllCharacteristics;        // DLL特性
        public ulong SizeOfStackReserve;         // 栈保留大小 (64位)
        public ulong SizeOfStackCommit;          // 栈提交大小 (64位)
        public ulong SizeOfHeapReserve;          // 堆保留大小 (64位)
        public ulong SizeOfHeapCommit;           // 堆提交大小 (64位)
        public uint LoaderFlags;                 // 加载器标志
        public uint NumberOfRvaAndSizes;         // 数据目录项数量
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public IMAGE_DATA_DIRECTORY[] DataDirectory;  // 数据目录
    }

    // NT头结构 (32位)
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_NT_HEADERS32
    {
        public uint Signature;                   // PE签名 ("PE\0\0")
        public IMAGE_FILE_HEADER FileHeader;     // 文件头
        public IMAGE_OPTIONAL_HEADER32 OptionalHeader;  // 32位可选头
    }

    // NT头结构 (64位)
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_NT_HEADERS64
    {
        public uint Signature;                   // PE签名 ("PE\0\0")
        public IMAGE_FILE_HEADER FileHeader;     // 文件头
        public IMAGE_OPTIONAL_HEADER64 OptionalHeader;  // 64位可选头
    }

    // 节头结构
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_SECTION_HEADER
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Name;                  // 节名称
        public uint VirtualSize;             // 虚拟大小
        public uint VirtualAddress;          // 虚拟地址 (RVA)
        public uint SizeOfRawData;           // 原始数据大小
        public uint PointerToRawData;        // 原始数据指针
        public uint PointerToRelocations;    // 重定位指针
        public uint PointerToLinenumbers;    // 行号指针
        public ushort NumberOfRelocations;   // 重定位数量
        public ushort NumberOfLinenumbers;   // 行号数量
        public uint Characteristics;         // 节特性
    }

    // 导入描述符结构
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_IMPORT_DESCRIPTOR
    {
        public uint OriginalFirstThunk;      // 导入查找表RVA
        public uint TimeDateStamp;           // 时间戳
        public uint ForwarderChain;          // 转发链
        public uint Name;                    // DLL名称RVA
        public uint FirstThunk;              // 导入地址表RVA
    }

    // 32位导入查找表项
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_THUNK_DATA32
    {
        public uint Function;                // 函数RVA或序号
    }

    // 64位导入查找表项
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_THUNK_DATA64
    {
        public ulong Function;               // 函数RVA或序号
    }

    // 按名称导入结构
    [StructLayout(LayoutKind.Sequential)]
    public struct IMAGE_IMPORT_BY_NAME
    {
        public ushort Hint;                  // 函数序号提示
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public byte[] Name;                  // 函数名称 (可变长度)
    }

    #endregion


    /// <summary>
    /// PE文件解析器
    /// </summary>
    public class PEParser
    {
        private string _filePath;
        private byte[] _fileData;
        private IMAGE_DOS_HEADER _dosHeader;
        private bool _is64Bit;
        private IMAGE_NT_HEADERS32 _ntHeaders32;
        private IMAGE_NT_HEADERS64 _ntHeaders64;
        private IMAGE_SECTION_HEADER[] _sectionHeaders;

        // 导入目录索引
        private const int IMAGE_DIRECTORY_ENTRY_IMPORT = 1;

        // 机器类型常量
        private const ushort IMAGE_FILE_MACHINE_I386 = 0x014c;  // x86
        private const ushort IMAGE_FILE_MACHINE_AMD64 = 0x8664; // x64

        // 可选头Magic常量
        private const ushort IMAGE_NT_OPTIONAL_HDR32_MAGIC = 0x10B; // 32位
        private const ushort IMAGE_NT_OPTIONAL_HDR64_MAGIC = 0x20B; // 64位

        // 导入表标志
        private const uint IMAGE_ORDINAL_FLAG32 = 0x80000000;
        private const ulong IMAGE_ORDINAL_FLAG64 = 0x8000000000000000;

        public PEParser(string filePath)
        {
            _filePath = filePath;
            LoadFile();
            ParsePEHeaders();
        }

        public bool Is64Bit => _is64Bit;

        /// <summary>
        /// 加载PE文件
        /// </summary>
        private void LoadFile()
        {
            if (!File.Exists(_filePath))
            {
                throw new FileNotFoundException("找不到指定的PE文件", _filePath);
            }

            _fileData = File.ReadAllBytes(_filePath);

            if (_fileData.Length < Marshal.SizeOf<IMAGE_DOS_HEADER>())
            {
                throw new InvalidOperationException("文件太小，不是有效的PE文件");
            }
        }

        /// <summary>
        /// 解析PE文件头
        /// </summary>
        private void ParsePEHeaders()
        {
            // 解析DOS头
            _dosHeader = BytesToStructure<IMAGE_DOS_HEADER>(_fileData, 0);

            // 检查DOS签名 (MZ)
            if (_dosHeader.e_magic != 0x5A4D)
            {
                throw new InvalidOperationException("不是有效的PE文件：缺少DOS签名(MZ)");
            }

            // 获取PE头偏移
            int ntHeaderOffset = _dosHeader.e_lfanew;

            if (ntHeaderOffset < 0 || ntHeaderOffset > _fileData.Length - 4)
            {
                throw new InvalidOperationException("无效的PE头偏移");
            }

            // 检查PE签名
            uint signature = BitConverter.ToUInt32(_fileData, ntHeaderOffset);
            if (signature != 0x00004550) // "PE\0\0"
            {
                throw new InvalidOperationException("不是有效的PE文件：缺少PE签名");
            }

            // 读取文件头以确定是32位还是64位
            var fileHeader = BytesToStructure<IMAGE_FILE_HEADER>(_fileData, ntHeaderOffset + 4);

            // 确定是32位还是64位
            ushort machine = fileHeader.Machine;
            if (machine == IMAGE_FILE_MACHINE_AMD64)
            {
                _is64Bit = true;
                _ntHeaders64 = BytesToStructure<IMAGE_NT_HEADERS64>(_fileData, ntHeaderOffset);

                if (_ntHeaders64.OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR64_MAGIC)
                {
                    throw new InvalidOperationException("无效的PE文件：64位可选头Magic不匹配");
                }
            }
            else if (machine == IMAGE_FILE_MACHINE_I386)
            {
                _is64Bit = false;
                _ntHeaders32 = BytesToStructure<IMAGE_NT_HEADERS32>(_fileData, ntHeaderOffset);

                if (_ntHeaders32.OptionalHeader.Magic != IMAGE_NT_OPTIONAL_HDR32_MAGIC)
                {
                    throw new InvalidOperationException("无效的PE文件：32位可选头Magic不匹配");
                }
            }
            else
            {
                throw new InvalidOperationException($"不支持的PE文件类型：Machine = 0x{machine:X4}");
            }

            // 解析节表
            ushort numberOfSections = fileHeader.NumberOfSections;
            int sectionHeaderOffset = ntHeaderOffset +
                                      Marshal.SizeOf<uint>() + // PE签名
                                      Marshal.SizeOf<IMAGE_FILE_HEADER>() + // 文件头
                                      fileHeader.SizeOfOptionalHeader; // 可选头大小

            _sectionHeaders = new IMAGE_SECTION_HEADER[numberOfSections];
            for (int i = 0; i < numberOfSections; i++)
            {
                int offset = sectionHeaderOffset + i * Marshal.SizeOf<IMAGE_SECTION_HEADER>();
                if (offset + Marshal.SizeOf<IMAGE_SECTION_HEADER>() <= _fileData.Length)
                {
                    _sectionHeaders[i] = BytesToStructure<IMAGE_SECTION_HEADER>(_fileData, offset);
                }
                else
                {
                    throw new InvalidOperationException("解析节表时超出文件范围");
                }
            }

            Console.WriteLine($"PE文件类型: {(_is64Bit ? "64位(x64)" : "32位(x86)")}");
            Console.WriteLine($"节数量: {numberOfSections}");
        }

        /// <summary>
        /// 解析导入表
        /// </summary>
        public void ParseImportTable()
        {
            // 获取导入表目录
            IMAGE_DATA_DIRECTORY importDirectory;
            if (_is64Bit)
            {
                importDirectory = _ntHeaders64.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
            }
            else
            {
                importDirectory = _ntHeaders32.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
            }

            if (importDirectory.VirtualAddress == 0 || importDirectory.Size == 0)
            {
                Console.WriteLine("该PE文件没有导入表");
                return;
            }

            // 转换RVA到文件偏移
            uint importTableOffset = RvaToOffset(importDirectory.VirtualAddress);
            if (importTableOffset == 0)
            {
                throw new InvalidOperationException("无法将导入表RVA转换为文件偏移");
            }

            Console.WriteLine("\n=== 导入表解析结果 ===\n");

            // 解析导入描述符数组
            uint currentOffset = importTableOffset;
            int dllCount = 0;

            while (true)
            {
                if (currentOffset + Marshal.SizeOf<IMAGE_IMPORT_DESCRIPTOR>() > _fileData.Length)
                {
                    throw new InvalidOperationException("解析导入表时超出文件范围");
                }

                var importDescriptor = BytesToStructure<IMAGE_IMPORT_DESCRIPTOR>(_fileData, (int)currentOffset);

                // 检查是否到达数组末尾 (全0的描述符)
                if (importDescriptor.Name == 0)
                {
                    break;
                }

                // 获取DLL名称
                uint nameOffset = RvaToOffset(importDescriptor.Name);
                string dllName = ReadNullTerminatedString(_fileData, (int)nameOffset);

                dllCount++;
                Console.WriteLine($"[DLL {dllCount}] {dllName}");

                // 解析导入函数
                ParseImportedFunctions(importDescriptor, dllName);

                // 移动到下一个导入描述符
                currentOffset += (uint)Marshal.SizeOf<IMAGE_IMPORT_DESCRIPTOR>();
            }

            Console.WriteLine($"\n总共导入 {dllCount} 个DLL");
        }

        /// <summary>
        /// 解析导入函数
        /// </summary>
        private void ParseImportedFunctions(IMAGE_IMPORT_DESCRIPTOR importDescriptor, string dllName)
        {
            // 获取导入查找表
            uint thunkRVA = importDescriptor.OriginalFirstThunk != 0
                          ? importDescriptor.OriginalFirstThunk
                          : importDescriptor.FirstThunk;

            uint thunkOffset = RvaToOffset(thunkRVA);
            if (thunkOffset == 0)
            {
                Console.WriteLine($"  无法解析 {dllName} 的导入函数");
                return;
            }

            int functionCount = 0;

            if (_is64Bit)
            {
                // 64位导入查找表解析
                uint currentOffset = thunkOffset;

                while (true)
                {
                    if (currentOffset + Marshal.SizeOf<IMAGE_THUNK_DATA64>() > _fileData.Length)
                    {
                        break;
                    }

                    var thunkData = BytesToStructure<IMAGE_THUNK_DATA64>(_fileData, (int)currentOffset);

                    // 检查是否到达数组末尾 (全0的Thunk)
                    if (thunkData.Function == 0)
                    {
                        break;
                    }

                    functionCount++;

                    // 检查是否按序号导入
                    if ((thunkData.Function & IMAGE_ORDINAL_FLAG64) != 0)
                    {
                        // 按序号导入
                        ushort ordinal = (ushort)(thunkData.Function & 0xFFFF);
                        Console.WriteLine($"  [{functionCount}] 序号: {ordinal}");
                    }
                    else
                    {
                        // 按名称导入
                        uint nameOffset = RvaToOffset((uint)(thunkData.Function & 0xFFFFFFFF));
                        if (nameOffset != 0 && nameOffset + 2 < _fileData.Length)
                        {
                            ushort hint = BitConverter.ToUInt16(_fileData, (int)nameOffset);
                            string functionName = ReadNullTerminatedString(_fileData, (int)nameOffset + 2);
                            Console.WriteLine($"  [{functionCount}] 名称: {functionName} (提示: {hint})");
                        }
                        else
                        {
                            Console.WriteLine($"  [{functionCount}] 无法解析函数名称");
                        }
                    }

                    // 移动到下一个Thunk
                    currentOffset += (uint)Marshal.SizeOf<IMAGE_THUNK_DATA64>();
                }
            }
            else
            {
                // 32位导入查找表解析
                uint currentOffset = thunkOffset;

                while (true)
                {
                    if (currentOffset + Marshal.SizeOf<IMAGE_THUNK_DATA32>() > _fileData.Length)
                    {
                        break;
                    }

                    var thunkData = BytesToStructure<IMAGE_THUNK_DATA32>(_fileData, (int)currentOffset);

                    // 检查是否到达数组末尾 (全0的Thunk)
                    if (thunkData.Function == 0)
                    {
                        break;
                    }

                    functionCount++;

                    // 检查是否按序号导入
                    if ((thunkData.Function & IMAGE_ORDINAL_FLAG32) != 0)
                    {
                        // 按序号导入
                        ushort ordinal = (ushort)(thunkData.Function & 0xFFFF);
                        Console.WriteLine($"  [{functionCount}] 序号: {ordinal}");
                    }
                    else
                    {
                        // 按名称导入
                        uint nameOffset = RvaToOffset(thunkData.Function);
                        if (nameOffset != 0 && nameOffset + 2 < _fileData.Length)
                        {
                            ushort hint = BitConverter.ToUInt16(_fileData, (int)nameOffset);
                            string functionName = ReadNullTerminatedString(_fileData, (int)nameOffset + 2);
                            Console.WriteLine($"  [{functionCount}] 名称: {functionName} (提示: {hint})");
                        }
                        else
                        {
                            Console.WriteLine($"  [{functionCount}] 无法解析函数名称");
                        }
                    }

                    // 移动到下一个Thunk
                    currentOffset += (uint)Marshal.SizeOf<IMAGE_THUNK_DATA32>();
                }
            }

            Console.WriteLine($"  总共导入 {functionCount} 个函数\n");
        }

        /// <summary>
        /// 将RVA转换为文件偏移
        /// </summary>
        private uint RvaToOffset(uint rva)
        {
            // 查找包含此RVA的节
            foreach (var section in _sectionHeaders)
            {
                if (rva >= section.VirtualAddress &&
                    rva < section.VirtualAddress + Math.Max(section.VirtualSize, section.SizeOfRawData))
                {
                    return section.PointerToRawData + (rva - section.VirtualAddress);
                }
            }

            return 0; // 未找到匹配的节
        }

        /// <summary>
        /// 读取C风格的以null结尾的字符串
        /// </summary>
        private string ReadNullTerminatedString(byte[] data, int offset)
        {
            if (offset < 0 || offset >= data.Length)
            {
                return string.Empty;
            }

            int endOffset = offset;
            while (endOffset < data.Length && data[endOffset] != 0)
            {
                endOffset++;
            }

            return Encoding.ASCII.GetString(data, offset, endOffset - offset);
        }

        /// <summary>
        /// 获取导入表信息 (用于IAT劫持)
        /// </summary>
        /// <returns>返回导入的DLL及其函数的字典</returns>
        public Dictionary<string, List<string>> GetImportTable()
        {
            var importTable = new Dictionary<string, List<string>>();

            try
            {
                // 获取导入表目录
                IMAGE_DATA_DIRECTORY importDirectory;
                if (_is64Bit)
                {
                    importDirectory = _ntHeaders64.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
                }
                else
                {
                    importDirectory = _ntHeaders32.OptionalHeader.DataDirectory[IMAGE_DIRECTORY_ENTRY_IMPORT];
                }

                if (importDirectory.VirtualAddress == 0 || importDirectory.Size == 0)
                {
                    return importTable; // 没有导入表
                }

                // 转换RVA到文件偏移
                uint importTableOffset = RvaToOffset(importDirectory.VirtualAddress);
                if (importTableOffset == 0)
                {
                    return importTable;
                }

                // 解析导入描述符数组
                uint currentOffset = importTableOffset;

                while (true)
                {
                    if (currentOffset + Marshal.SizeOf<IMAGE_IMPORT_DESCRIPTOR>() > _fileData.Length)
                    {
                        break;
                    }

                    var importDescriptor = BytesToStructure<IMAGE_IMPORT_DESCRIPTOR>(_fileData, (int)currentOffset);

                    // 检查是否到达数组末尾 (全0的描述符)
                    if (importDescriptor.Name == 0)
                    {
                        break;
                    }

                    // 获取DLL名称
                    uint nameOffset = RvaToOffset(importDescriptor.Name);
                    string dllName = ReadNullTerminatedString(_fileData, (int)nameOffset);

                    if (!string.IsNullOrEmpty(dllName))
                    {
                        // 获取该DLL的导入函数
                        var functions = GetImportedFunctions(importDescriptor);
                        if (functions.Count > 0)
                        {
                            importTable[dllName.ToLower()] = functions;
                        }
                    }

                    // 移动到下一个导入描述符
                    currentOffset += (uint)Marshal.SizeOf<IMAGE_IMPORT_DESCRIPTOR>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析导入表时出错: {ex.Message}");
            }

            return importTable;
        }

        /// <summary>
        /// 获取指定导入描述符的函数列表
        /// </summary>
        private List<string> GetImportedFunctions(IMAGE_IMPORT_DESCRIPTOR importDescriptor)
        {
            var functions = new List<string>();

            try
            {
                // 获取导入查找表
                uint thunkRVA = importDescriptor.OriginalFirstThunk != 0
                              ? importDescriptor.OriginalFirstThunk
                              : importDescriptor.FirstThunk;

                uint thunkOffset = RvaToOffset(thunkRVA);
                if (thunkOffset == 0)
                {
                    return functions;
                }

                if (_is64Bit)
                {
                    // 64位导入查找表解析
                    uint currentOffset = thunkOffset;

                    while (true)
                    {
                        if (currentOffset + Marshal.SizeOf<IMAGE_THUNK_DATA64>() > _fileData.Length)
                        {
                            break;
                        }

                        var thunkData = BytesToStructure<IMAGE_THUNK_DATA64>(_fileData, (int)currentOffset);

                        // 检查是否到达数组末尾 (全0的Thunk)
                        if (thunkData.Function == 0)
                        {
                            break;
                        }

                        // 检查是否按序号导入
                        if ((thunkData.Function & IMAGE_ORDINAL_FLAG64) != 0)
                        {
                            // 按序号导入
                            ushort ordinal = (ushort)(thunkData.Function & 0xFFFF);
                            functions.Add($"Ordinal_{ordinal}");
                        }
                        else
                        {
                            // 按名称导入
                            uint nameOffset = RvaToOffset((uint)(thunkData.Function & 0xFFFFFFFF));
                            if (nameOffset != 0 && nameOffset + 2 < _fileData.Length)
                            {
                                string functionName = ReadNullTerminatedString(_fileData, (int)nameOffset + 2);
                                if (!string.IsNullOrEmpty(functionName))
                                {
                                    functions.Add(functionName);
                                }
                            }
                        }

                        // 移动到下一个Thunk
                        currentOffset += (uint)Marshal.SizeOf<IMAGE_THUNK_DATA64>();
                    }
                }
                else
                {
                    // 32位导入查找表解析
                    uint currentOffset = thunkOffset;

                    while (true)
                    {
                        if (currentOffset + Marshal.SizeOf<IMAGE_THUNK_DATA32>() > _fileData.Length)
                        {
                            break;
                        }

                        var thunkData = BytesToStructure<IMAGE_THUNK_DATA32>(_fileData, (int)currentOffset);

                        // 检查是否到达数组末尾 (全0的Thunk)
                        if (thunkData.Function == 0)
                        {
                            break;
                        }

                        // 检查是否按序号导入
                        if ((thunkData.Function & IMAGE_ORDINAL_FLAG32) != 0)
                        {
                            // 按序号导入
                            ushort ordinal = (ushort)(thunkData.Function & 0xFFFF);
                            functions.Add($"Ordinal_{ordinal}");
                        }
                        else
                        {
                            // 按名称导入
                            uint nameOffset = RvaToOffset(thunkData.Function);
                            if (nameOffset != 0 && nameOffset + 2 < _fileData.Length)
                            {
                                string functionName = ReadNullTerminatedString(_fileData, (int)nameOffset + 2);
                                if (!string.IsNullOrEmpty(functionName))
                                {
                                    functions.Add(functionName);
                                }
                            }
                        }

                        // 移动到下一个Thunk
                        currentOffset += (uint)Marshal.SizeOf<IMAGE_THUNK_DATA32>();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"解析导入函数时出错: {ex.Message}");
            }

            return functions;
        }

        /// <summary>
        /// 检查是否为.Net程序集
        /// </summary>
        public bool IsDotNetAssembly()
        {
            try
            {
                // 检查是否有CLR头
                IMAGE_DATA_DIRECTORY clrDirectory;
                if (_is64Bit)
                {
                    clrDirectory = _ntHeaders64.OptionalHeader.DataDirectory[14]; // IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR
                }
                else
                {
                    clrDirectory = _ntHeaders32.OptionalHeader.DataDirectory[14]; // IMAGE_DIRECTORY_ENTRY_COM_DESCRIPTOR
                }

                bool isDotNet = clrDirectory.VirtualAddress != 0 && clrDirectory.Size > 0;
                
                // 调试信息
                if (isDotNet)
                {
                    System.Diagnostics.Debug.WriteLine($"检测到.Net程序: {Path.GetFileName(_filePath)} - CLR RVA: 0x{clrDirectory.VirtualAddress:X8}, Size: {clrDirectory.Size}");
                }
                
                return isDotNet;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"检查.Net程序时出错: {Path.GetFileName(_filePath)} - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断是否为GUI应用程序
        /// </summary>
        public bool IsGuiApplication()
        {
            try
            {
                ushort subsystem;
                if (_is64Bit)
                {
                    subsystem = _ntHeaders64.OptionalHeader.Subsystem;
                }
                else
                {
                    subsystem = _ntHeaders32.OptionalHeader.Subsystem;
                }

                // IMAGE_SUBSYSTEM_WINDOWS_GUI = 2
                return subsystem == 2;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 判断是否为控制台应用程序
        /// </summary>
        public bool IsConsoleApplication()
        {
            try
            {
                ushort subsystem;
                if (_is64Bit)
                {
                    subsystem = _ntHeaders64.OptionalHeader.Subsystem;
                }
                else
                {
                    subsystem = _ntHeaders32.OptionalHeader.Subsystem;
                }

                // IMAGE_SUBSYSTEM_WINDOWS_CUI = 3
                return subsystem == 3;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取.Net版本
        /// </summary>
        public string GetDotNetVersion()
        {
            try
            {
                if (!IsDotNetAssembly())
                {
                    return "非.Net程序";
                }

                // 获取CLR头
                IMAGE_DATA_DIRECTORY clrDirectory;
                if (_is64Bit)
                {
                    clrDirectory = _ntHeaders64.OptionalHeader.DataDirectory[14];
                }
                else
                {
                    clrDirectory = _ntHeaders32.OptionalHeader.DataDirectory[14];
                }

                if (clrDirectory.VirtualAddress == 0)
                {
                    return "未知";
                }

                // 转换RVA到文件偏移
                uint clrOffset = RvaToOffset(clrDirectory.VirtualAddress);
                if (clrOffset == 0)
                {
                    return "未知";
                }

                // 读取CLR头信息
                if (clrOffset + 8 <= _fileData.Length)
                {
                    uint majorVersion = BitConverter.ToUInt32(_fileData, (int)clrOffset);
                    uint minorVersion = BitConverter.ToUInt32(_fileData, (int)clrOffset + 4);
                    
                    return $".Net {majorVersion}.{minorVersion}";
                }

                return "未知";
            }
            catch
            {
                return "未知";
            }
        }

        /// <summary>
        /// 将字节数组转换为结构
        /// </summary>
        private static T BytesToStructure<T>(byte[] bytes, int offset) where T : struct
        {
            int size = Marshal.SizeOf<T>();

            if (offset + size > bytes.Length)
            {
                throw new ArgumentException($"字节数组太小，无法容纳{typeof(T).Name}结构");
            }

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                IntPtr ptr = handle.AddrOfPinnedObject() + offset;
                return Marshal.PtrToStructure<T>(ptr);
            }
            finally
            {
                handle.Free();
            }
        }

    }
}
