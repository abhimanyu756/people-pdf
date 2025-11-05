using System;
using System.IO;
using System.Reflection;

namespace people_pdf
{
    public class DllChecker
    {
        public static void CheckDllArchitecture(string dllPath)
        {
            try
            {
                Console.WriteLine($"Checking: {Path.GetFileName(dllPath)}");

                if (!File.Exists(dllPath))
                {
                    Console.WriteLine("  ✗ File not found!");
                    return;
                }

                // Check if it's a .NET assembly
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(dllPath);
                    Console.WriteLine($"  ✓ .NET Assembly");
                    Console.WriteLine($"  Architecture: {assemblyName.ProcessorArchitecture}");
                    Console.WriteLine($"  Version: {assemblyName.Version}");
                    Console.WriteLine($"  Culture: {assemblyName.CultureName ?? "neutral"}");
                    Console.WriteLine($"  PublicKeyToken: {BitConverter.ToString(assemblyName.GetPublicKeyToken() ?? new byte[0])}");
                }
                catch (BadImageFormatException)
                {
                    Console.WriteLine("  Native DLL (not a .NET assembly)");

                    // Check PE header for native DLL
                    using (var stream = new FileStream(dllPath, FileMode.Open, FileAccess.Read))
                    using (var reader = new BinaryReader(stream))
                    {
                        // DOS header
                        stream.Seek(0x3C, SeekOrigin.Begin);
                        int peOffset = reader.ReadInt32();

                        // PE header
                        stream.Seek(peOffset, SeekOrigin.Begin);
                        uint peSignature = reader.ReadUInt32();

                        if (peSignature != 0x00004550) // "PE\0\0"
                        {
                            Console.WriteLine("  ✗ Invalid PE signature");
                            return;
                        }

                        // COFF header
                        ushort machine = reader.ReadUInt16();

                        string architecture = machine switch
                        {
                            0x014c => "x86 (32-bit)",
                            0x8664 => "x64 (64-bit)",
                            0x0200 => "IA64",
                            0x01c4 => "ARM",
                            0xaa64 => "ARM64",
                            _ => $"Unknown (0x{machine:X4})"
                        };

                        Console.WriteLine($"  Architecture: {architecture}");
                    }
                }

                // Check current process architecture
                Console.WriteLine($"\nCurrent Process:");
                Console.WriteLine($"  Architecture: {(Environment.Is64BitProcess ? "x64 (64-bit)" : "x86 (32-bit)")}");
                Console.WriteLine($"  OS: {(Environment.Is64BitOperatingSystem ? "x64 (64-bit)" : "x86 (32-bit)")}");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ Error: {ex.Message}");
            }

            Console.WriteLine();
        }
    }
}