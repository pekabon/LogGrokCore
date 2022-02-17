using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace LogGrokCore.Data
{
    public class TransformationPerformer 
    {
        private readonly List<Regex> _transformations;

        public TransformationPerformer(string[] transformations)
        {
            _transformations = 
                transformations.Select(t => new Regex(t, RegexOptions.Compiled | RegexOptions.Singleline)).ToList();
        }

        public string Transform(string source)
        {
            var temp = source; 
            foreach (var transformation in _transformations)
            {
                var match = transformation.Match(temp);
                if (match.Success)
                {
                    var groups = match.Groups;
                    var builder = new StringBuilder();
                    var offset = 0;
                    for (var i = 1; i < groups.Count; i++)
                    {
                        var group = groups[i];
                        builder.Append(temp.Substring(offset, group.Index - offset));
                        builder.Append(ApplyTransform(group.Name, temp.Substring(group.Index, group.Length)));
                        offset = group.Index + group.Length;
                    }
                    
                    builder.Append(temp.Substring(offset, temp.Length - offset));
                    temp = builder.ToString();
                }
            }

            return temp;
        }
        
        private string ApplyTransform(string transformName, string input)
        {
            try
            {
                return transformName switch
                {
                    "Base64Decode" => Base64Decode(input),
                    // "Base64DecodeFormatJson" => FormatJsonText(Base64Decode(input)),
                    // "FormatJson" => FormatJsonText(input),
                    _ => input
                };
            }
            
            catch (Exception e)
            {
                Trace.TraceError(e.ToString());
                return input;
            }
        }

        static string FormatJsonText(string jsonString)
        {
            using var doc = JsonDocument.Parse(
                jsonString,
                new JsonDocumentOptions
                {
                    AllowTrailingCommas = true
                }
            );
            MemoryStream memoryStream = new MemoryStream();
            using (
                var utf8JsonWriter = new Utf8JsonWriter(
                    memoryStream,
                    new JsonWriterOptions
                    {
                        Indented = true
                    }
                )
            )
            {
                doc.WriteTo(utf8JsonWriter);
            }
            return new System.Text.UTF8Encoding()
                .GetString(memoryStream.ToArray());
        }

        private static string Base64Decode(string input)
        {
            var buffer = Encoding.UTF8.GetBytes(input);
            Base64.DecodeFromUtf8InPlace(buffer, out var bytesWritten);
            return Encoding.UTF8.GetString(buffer, 0, bytesWritten);
        }
    }
}