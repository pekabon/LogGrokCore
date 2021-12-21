using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using LogGrokCore.Data;

namespace LogGrokCore
{
    internal static class LogMetaInformationProvider
    {
        private const int ProbeSize = 256;
        private const double DetectThreshold = 0.05;

        public static LogMetaInformation GetLogMetaInformation(string fileName,
            IEnumerable<LogFormat> logFormats)
        {
            var metas = logFormats
                .Select(m => new LogMetaInformation(m));   

            var bestFitter = metas
                .Select(meta => (meta, ratio: GetRatio(meta, fileName)))
                .OrderByDescending(m => m.ratio)
                .Where(m => m.ratio > DetectThreshold)
                .Select(m => m.meta)
                .FirstOrDefault();

            return bestFitter ?? LogMetaInformation.CreateTextFileMetaInformation();                
        }

        private static double GetRatio(LogMetaInformation logMetaInformation, string fileName)
        {
            var regex = new Regex(logMetaInformation.LineRegex);

            IEnumerable<string> ReadLines()
            {
                var logFile = new LogFile(fileName, logMetaInformation.XorMask);
                using var fileStream =  logFile.OpenForSequentialRead();
                using var streamReader = new StreamReader(fileStream);
                while (!streamReader.EndOfStream)
                {
                    yield return streamReader.ReadLine()!;
                }
            }

            var lines = ReadLines()
                .Take(ProbeSize)
                .GroupBy(line => regex.IsMatch(line)).ToList();

            var matched = lines.FirstOrDefault(g => g.Key)?.Count() ?? 0;
            var unmatched = lines.FirstOrDefault(g => !g.Key)?.Count() ?? 0;

            if (matched + unmatched == 0) return 0;
            return (double) matched / (matched + unmatched);
        }
    }
}