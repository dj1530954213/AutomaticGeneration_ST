using System;
using System.Collections.Concurrent;
using System.IO;
using AutomaticGeneration_ST.Models;

namespace AutomaticGeneration_ST.Services
{
    /// <summary>
    /// Provides cached access to template metadata to avoid repeated disk parsing.
    /// </summary>
    public static class TemplateMetadataCache
    {
        private static readonly ConcurrentDictionary<string, TemplateMetadata?> Cache =
            new(StringComparer.OrdinalIgnoreCase);

        private static readonly object ParserLock = new();
        private static TemplateMetadataParser? _parser;

        private static TemplateMetadataParser Parser
        {
            get
            {
                if (_parser != null)
                {
                    return _parser;
                }

                lock (ParserLock)
                {
                    _parser ??= new TemplateMetadataParser();
                    return _parser;
                }
            }
        }

        /// <summary>
        /// Returns metadata for the specified template path, parsing and caching on first use.
        /// </summary>
        public static TemplateMetadata? GetMetadata(string templatePath)
        {
            if (string.IsNullOrWhiteSpace(templatePath))
            {
                return null;
            }

            try
            {
                var fullPath = Path.GetFullPath(templatePath);
                return Cache.GetOrAdd(fullPath, path =>
                {
                    if (!File.Exists(path))
                    {
                        return null;
                    }

                    return Parser.ParseTemplate(path);
                });
            }
            catch
            {
                // Path.GetFullPath 或解析失败时返回 null，调用侧自行处理
                return null;
            }
        }

        /// <summary>
        /// Convenience helper to fetch ProgramName from metadata.
        /// </summary>
        public static string? GetProgramName(string templatePath)
        {
            var metadata = GetMetadata(templatePath);
            var programName = metadata?.ProgramName;

            return string.IsNullOrWhiteSpace(programName)
                ? null
                : programName.Trim();
        }
    }
}
