using System;
using System.Collections.Generic;
using System.IO;


    /// <summary>
    /// Helpers for Project 2.3:
    /// - Read AS2015.txt / AS2021.txt
    /// - Count Enterprise / Content / Transit/Access
    /// - (Optionally) compare CAIDA types to our inferred types from Graph 4
    /// </summary>
    public static class CaidaClassificationStats
    {
        public enum CaidaClass
        {
            Enterprise,
            Content,
            TransitAccess,
            Unknown
        }

        public class Counts
        {
            public int Enterprise;
            public int Content;
            public int TransitAccess;

            public int Total => Enterprise + Content + TransitAccess;
        }

        /// <summary>
        /// Parse an AS type file in the format:
        ///   asn|source|type
        /// ignoring comment lines beginning with '#'.
        /// </summary>
        public static Counts LoadCounts(string filePath)
        {
            var counts = new Counts();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[2.3] File not found: {filePath}");
                return counts;
            }

            using var reader = new StreamReader(filePath);
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                // Skip comments in the header
                if (line[0] == '#')
                    continue;

                var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;

                string type = parts[2].Trim().ToLowerInvariant();

                if (type.Contains("transit"))
                    counts.TransitAccess++;
                else if (type.Contains("content"))
                    counts.Content++;
                else if (type.Contains("enterprise"))
                    counts.Enterprise++;
            }

            return counts;
        }

        /// <summary>
        /// Build a map ASN -> CAIDA class (for comparison with our inferred types).
        /// </summary>
        public static Dictionary<int, CaidaClass> LoadMap(string filePath)
        {
            var map = new Dictionary<int, CaidaClass>();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[2.3] File not found: {filePath}");
                return map;
            }

            using var reader = new StreamReader(filePath);
            string? line;

            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line[0] == '#')
                    continue;

                var parts = line.Split('|', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 3)
                    continue;

                if (!int.TryParse(parts[0], out int asn))
                    continue;

                string type = parts[2].Trim().ToLowerInvariant();

                CaidaClass cls = type.Contains("enterprise") ? CaidaClass.Enterprise
                                   : type.Contains("content") ? CaidaClass.Content
                                   : type.Contains("transit") ? CaidaClass.TransitAccess
                                   : CaidaClass.Unknown;

                map[asn] = cls;
            }

            return map;
        }

        public static void PrintSummary(string label, Counts c)
        {
            Console.WriteLine($"\n--- CAIDA AS classification summary ({label}) ---");
            Console.WriteLine($"Total ASes:       {c.Total}");
            Console.WriteLine($"Enterprise:       {c.Enterprise}");
            Console.WriteLine($"Content:          {c.Content}");
            Console.WriteLine($"Transit/Access:   {c.TransitAccess}");

            if (c.Total > 0)
            {
                Console.WriteLine($"Enterprise %:     {c.Enterprise * 100.0 / c.Total:F6}");
                Console.WriteLine($"Content %:        {c.Content * 100.0 / c.Total:F6}");
                Console.WriteLine($"Transit/Access %: {c.TransitAccess * 100.0 / c.Total:F6}");
            }
        }

        /// <summary>
        /// Compare our inferred Graph-4 classification to CAIDA classes for 2021.
        /// Assumes Program.ASNode.Classification is set to "Enterprise", "Content", or "Transit".
        /// </summary>
        public static void CompareWithInferred(
            Dictionary<int, Program.ASNode> inferredNodes,
            Dictionary<int, CaidaClass> caidaMap,
            out int commonAses,
            out int agree,
            out int disagree)
        {
            commonAses = 0;
            agree      = 0;
            disagree   = 0;

            foreach (var kvp in inferredNodes)
            {
                int asn = kvp.Key;
                var node = kvp.Value;

                if (string.IsNullOrEmpty(node.Classification))
                    continue;

                if (!caidaMap.TryGetValue(asn, out var caidaClass))
                    continue;

                var inferred = MapInferredToCaida(node.Classification);

                if (inferred == CaidaClass.Unknown || caidaClass == CaidaClass.Unknown)
                    continue;

                commonAses++;

                if (inferred == caidaClass)
                    agree++;
                else
                    disagree++;
            }
        }

        private static CaidaClass MapInferredToCaida(string classification)
        {
            switch (classification)
            {
                case "Enterprise": return CaidaClass.Enterprise;
                case "Content":    return CaidaClass.Content;
                case "Transit":    return CaidaClass.TransitAccess;
                default:           return CaidaClass.Unknown;
            }
        }
    }

