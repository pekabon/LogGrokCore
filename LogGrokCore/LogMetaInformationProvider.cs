using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LogGrokCore.Data;

namespace LogGrokCore
{
    internal static class LogMetaInformationProvider
    {
        private const int ProbeSizeLines = 256;
        private const int ProbeSizeBytes = 1024 * 1024;
        private const double DetectThreshold = 0.05;

        public static LogMetaInformation GetLogMetaInformation(string fileName,
            IEnumerable<LogFormat> logFormats)
        {
            var metas = logFormats
                .Select(m => new LogMetaInformation(m));   

            Dictionary<(string fileName, byte mask), IReadOnlyList<string>> cache = new();
            var bestFitter = metas
                .Select(meta => (meta, ratio: GetRatio(meta, fileName, cache)))
                .OrderByDescending(m => m.ratio)
                .Where(m => m.ratio > DetectThreshold)
                .Select(m => m.meta)
                .FirstOrDefault();

            return bestFitter ?? LogMetaInformation.CreateTextFileMetaInformation();                
        }

        private static double GetRatio(LogMetaInformation logMetaInformation, string fileName, 
            Dictionary<(string fileName, byte mask), IReadOnlyList<string>> cache)
        {
            var regex = new Regex(logMetaInformation.LineRegex);

            IEnumerable<string> ReadLines(string file, byte xorMask)
            {
                var logFile = new LogFile(file, xorMask);
                using var fileStream =  logFile.OpenForSequentialRead();
                var buffer = ArrayPool<byte>.Shared.Rent(ProbeSizeBytes);
                var bytesRead = fileStream.Read(buffer.AsSpan());
                using var memoryStream = new MemoryStream(buffer, 0, bytesRead);
                using var streamReader = new StreamReader(memoryStream);
                while (!streamReader.EndOfStream)
                {
                    yield return streamReader.ReadLine()!;
                }
                ArrayPool<byte>.Shared.Return(buffer);
            }

            var fileAndMask = (fileName, logMetaInformation.XorMask);
            if (!cache.TryGetValue(fileAndMask, out var sourceLines))
            {
                sourceLines = ReadLines(fileAndMask.fileName, fileAndMask.XorMask).Take(ProbeSizeLines).ToList();
                cache.Add(fileAndMask, sourceLines);
            }

            var lineGroups = sourceLines
                .GroupBy(line => regex.IsMatch(line)).ToList();

            var matched = lineGroups.FirstOrDefault(g => g.Key)?.Count() ?? 0;
            var unmatched = lineGroups.FirstOrDefault(g => !g.Key)?.Count() ?? 0;

            if (matched + unmatched == 0) return 0;
            return (double) matched / (matched + unmatched);
        }
    }
}