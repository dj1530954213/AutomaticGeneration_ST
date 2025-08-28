using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ParserTool
{
    internal static class Program
    {
        private static readonly string[] Anchors = new[]
        {
            "CLDNetwork", "CLDBox", "CLDElement", "CLDAssign",
            // 常见块/引脚关键字：
            "EN", "ENO"
        };

        private static void Main(string[] args)
        {
            // 默认目标文件：POU/Main - 副本.pou（相对当前工作目录）
            var targetPath = args.Length > 0 ? args[0] : Path.Combine("..", "Main - 副本.pou");
            if (!File.Exists(targetPath))
            {
                Console.Error.WriteLine($"Target file not found: {targetPath}");
                Environment.ExitCode = 2;
                return;
            }

            var bytes = File.ReadAllBytes(targetPath);
            var result = new ParseResult
            {
                File = Path.GetFullPath(targetPath),
                FileSize = bytes.Length,
            };

            // 1) 提取可见 ASCII 字符串（长度>=3），构建字符串表
            result.Strings = ExtractAsciiStrings(bytes, minLen: 3, maxLen: 128)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            // 2) 扫描锚点位置
            result.Markers = new();
            foreach (var anchor in Anchors.Distinct())
            {
                var offs = FindAll(bytes, Encoding.ASCII.GetBytes(anchor));
                result.Markers[anchor] = offs;
            }

            // 3) 基于常见功能块名/词干，做一个轻量“块名采样”
            var likelyBlockNames = new[] { "AI_Convert", "VALVE_CTRL", "DI_MAPPING", "DO_MAPPING" };
            result.Blocks = result.Strings
                .Where(s => likelyBlockNames.Contains(s) || LooksLikeIdentifier(s))
                .Where(s => s.Length >= 3 && s.Length <= 64)
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            // 4) 在每个锚点附近抓取一个上下文窗口，便于后验分析
            result.Contexts = new();
            foreach (var kv in result.Markers)
            {
                foreach (var off in kv.Value.Take(100)) // 防止输出过大，仅取前100个
                {
                    var ctx = new ContextWindow
                    {
                        Anchor = kv.Key,
                        Offset = off,
                        HexBefore = DumpHex(bytes, Math.Max(0, off - 32), off),
                        HexAfter = DumpHex(bytes, off, Math.Min(bytes.Length, off + 96))
                    };
                    result.Contexts.Add(ctx);
                }
            }

            // 5) 输出 JSON 到同级目录：POU/Main - 副本.pou.json
            var outPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(targetPath)!, Path.GetFileName(targetPath) + ".json"));
            var jsonOpts = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            var json = JsonSerializer.Serialize(result, jsonOpts);
            File.WriteAllText(outPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            Console.WriteLine($"Done. JSON written: {outPath}");
        }

        private static List<string> ExtractAsciiStrings(byte[] data, int minLen, int maxLen)
        {
            var list = new List<string>();
            int i = 0;
            var sb = new StringBuilder();

            bool IsAsciiVisible(byte b) => b >= 0x20 && b <= 0x7E; // 可见 ASCII

            while (i < data.Length)
            {
                sb.Clear();
                while (i < data.Length && IsAsciiVisible(data[i]) && sb.Length < maxLen)
                {
                    sb.Append((char)data[i]);
                    i++;
                }
                if (sb.Length >= minLen)
                {
                    list.Add(sb.ToString());
                }
                i++;
            }
            return list;
        }

        private static List<int> FindAll(byte[] haystack, byte[] needle)
        {
            var offs = new List<int>();
            if (needle.Length == 0 || haystack.Length < needle.Length) return offs;
            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                int j = 0;
                for (; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j]) break;
                }
                if (j == needle.Length) offs.Add(i);
            }
            return offs;
        }

        private static string DumpHex(byte[] data, int start, int end)
        {
            var sb = new StringBuilder();
            for (int i = start; i < end; i++)
            {
                sb.Append(data[i].ToString("X2"));
                if ((i - start + 1) % 16 == 0) sb.Append('\n'); else sb.Append(' ');
            }
            return sb.ToString().TrimEnd();
        }

        private static bool LooksLikeIdentifier(string s)
        {
            // 简单的块名/变量名判定：包含字母/数字/下划线，不能全是数字，且不包含空格
            if (string.IsNullOrWhiteSpace(s)) return false;
            if (s.Any(ch => char.IsWhiteSpace(ch))) return false;
            if (!s.Any(ch => char.IsLetter(ch))) return false;
            if (s.All(ch => char.IsDigit(ch))) return false;
            // 排除明显非标记性字符串
            if (s.Length > 64) return false;
            return Regex.IsMatch(s, "^[A-Za-z0-9_\-\.]+$");
        }
    }

    internal sealed class ParseResult
    {
        public string File { get; set; } = string.Empty;
        public int FileSize { get; set; }
        public Dictionary<string, List<int>> Markers { get; set; } = new();
        public List<string> Strings { get; set; } = new();
        public List<string> Blocks { get; set; } = new();
        public List<ContextWindow> Contexts { get; set; } = new();
    }

    internal sealed class ContextWindow
    {
        public string Anchor { get; set; } = string.Empty;
        public int Offset { get; set; }
        public string HexBefore { get; set; } = string.Empty;
        public string HexAfter { get; set; } = string.Empty;
    }
}
