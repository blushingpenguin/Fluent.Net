using System;
using System.IO;
using System.Collections.Generic;
using Mono.Options;

namespace Fluent.Net.SyntaxTest
{
    internal class Program
    {
        static int DuplicateCheck(string message, string file, Ast.Span span,
            Dictionary<string, Location> messages)
        {
            Location loc;
            if (!messages.TryGetValue(message, out loc))
            {
                messages.Add(message,
                    new Location() {
                        File = file,
                        Position = span != null ? span.Start : Position.Start
                    });
                return 0;
            }

            Console.Write(file);
            if (span != null)
            {
                Console.Write(
                    $"({span.Start.FormatLineOffset()}, " +
                    $"{span.End.FormatLineOffset()})");
            }
            Console.Write(
                $": duplicate message identifier '{message}' " +
                $"(first instance in file {loc.File}");
            if (span != null)
            {
                Console.Write($"({loc.Position.FormatLineOffset()})");
            }
            Console.WriteLine(").");
            return 1;
        }

        static int RuntimeParse(string file, StreamReader sr,
            Dictionary<string, Location> messages)
        {
            var p = new RuntimeParser();
            var resource = p.GetResource(sr);
            int errors = 0;
            foreach (var error in resource.Errors)
            {
                Console.WriteLine($"{file}: {error.Message}");
                ++errors;
            }
            if (messages != null)
            {
                foreach (var message in resource.Entries)
                {
                    errors += DuplicateCheck(message.Key, file,
                        null, messages);
                }
            }
            return errors;
        }

        class Location
        {
            public string File { get; set; }
            public Position Position { get; set; }
        }

        static int Parse(string file, StreamReader sr,
            Dictionary<string, Location> messages)
        {
            var p = new Parser();
            var resource = p.Parse(sr);
            int errors = 0;
            foreach (var entry in resource.Body)
            {
                if (entry is Ast.Junk junk)
                {
                    foreach (var annotation in junk.Annotations)
                    {
                        Console.WriteLine($"{file}" +
                            $"({annotation.Span.Start.FormatLineOffset()}, " +
                            $"{annotation.Span.End.FormatLineOffset()}): " +
                            $"error {annotation.Code}: {annotation.Message}");
                        ++errors;
                    }
                }
                else if (messages != null &&
                            entry is Ast.MessageTermBase message)
                {
                    errors += DuplicateCheck(message.Id.Name, file,
                        message.Span, messages);
                }
            }
            return errors;
        }

        static int Parse(string file, bool useRuntime)
        {
            if (!File.Exists(file))
            {
                Console.WriteLine($"The file {file} does not exist");
                return 1;
            }

            using (var sr = new StreamReader(file))
            {
                return useRuntime ?
                    RuntimeParse(file, sr, null) :
                    Parse(file, sr, null);
            }
        }

        static int ParseFiles(IEnumerable<string> files, bool useRuntime)
        {
            int errors = 0;
            foreach (string file in files)
            {
                errors += Parse(file, useRuntime);
            }
            return errors;
        }

        static int ParseFolders(IEnumerable<string> folders, bool useRuntime)
        {
            int errors = 0;
            var messageSets = new Dictionary<string, Dictionary<string, Location>>();
            foreach (string folder in folders)
            {
                foreach (string file in Directory.GetFiles(folder, "*.ftl"))
                {
                    string set = Path.GetFileNameWithoutExtension(file)
                        .Replace("_", "-").ToLowerInvariant();

                    Dictionary<string, Location> messages;
                    if (!messageSets.TryGetValue(set, out messages))
                    {
                        messages = new Dictionary<string, Location>();
                        messageSets.Add(set, messages);
                    }

                    using (var sr = new StreamReader(file))
                    {
                        errors += useRuntime ?
                            RuntimeParse(file, sr, messages) :
                            Parse(file, sr, messages);
                    }
                }
            }
            return errors;
        }

        static int TryMain(string[] args)
        {
            bool showHelp = false;
            bool useRuntime = false;
            bool useFolders = false;

            var opts = new OptionSet()
            {
                { "r|runtime", "use the runtime parser",
                    v => useRuntime = v != null },
                { "f|folders", "treat input as folders containing ftl files",
                    v => useFolders = v != null },
                { "h|help", "show this message and exit",
                    v => showHelp = v != null }
            };

            List<string> extra;
            try
            {
                extra = opts.Parse(args);
                if (!showHelp && extra.Count == 0)
                {
                    throw new OptionException("no input files specified", "file");
                }
            }
            catch (OptionException e)
            {
                Console.WriteLine($"Fluent.Net.SyntaxTest: {e.Message}");
                Console.WriteLine("try `Fluent.Net.SyntaxTest --help` for more information");
                return 1;
            }

            if (showHelp)
            {
                Console.WriteLine("Usage: Fluent.Net.SyntaxTest [OPTIONS] files|folders");
                Console.WriteLine("Parse fluent translation files, printing any errors encountered. The exit code");
                Console.WriteLine("of the process is the number of errors encountered.");
                Console.WriteLine("Specifying the folders option causes all files in the folders to be parsed");
                Console.WriteLine("in sets grouped by name, with errors printed for duplicate entries within the");
                Console.WriteLine("sets");
                Console.WriteLine();
                Console.WriteLine("Options:");
                opts.WriteOptionDescriptions(Console.Out);
                return 0;
            }

            if (useFolders)
            {
                return ParseFolders(extra, useRuntime);
            }
            return ParseFiles(extra, useRuntime);
        }

        internal static int Main(string[] args)
        {
            try
            {
                return TryMain(args);
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught unexpected exception:");
                Console.WriteLine(e);
                return 1;
            }
        }
    }
}
