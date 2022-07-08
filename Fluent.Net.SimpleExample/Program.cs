using System;
using System.Collections.Generic;
using System.IO;

namespace Fluent.Net.SimpleExample
{
    class Program
    {
        static MessageContext GetMessages(string lang)
        {
            string ftlPath = Path.Combine("..", "..", "..", $"{lang}.ftl");
            using var sr = new StreamReader(ftlPath);
            var options = new MessageContextOptions { UseIsolating = false };
            var mc = new MessageContext(lang, options);
            var errors = mc.AddMessages(sr);
            foreach (var error in errors)
            {
                Console.WriteLine(error);
            }
            return mc;
        }

        static void RunTest(string lang)
        {
            var messageContext = GetMessages(lang);
            var translator = new TranslationService(new MessageContext[] { messageContext });

            Console.WriteLine($"{lang}:");
            Console.WriteLine($"tabs-close-button = {translator.GetString("tabs-close-button")}");

            Console.WriteLine("tabs-close-tooltip ($tabCount = 1) = " +
                translator.GetString("tabs-close-tooltip", TranslationService.Args("tabCount", 1)));
            Console.WriteLine($"tabs-close-tooltip ($tabCount = 2) = " +
                translator.GetString("tabs-close-tooltip", TranslationService.Args("tabCount", 2)));

            Console.WriteLine("tabs-close-warning ($tabCount = 1) = " +
                translator.GetString("tabs-close-warning", TranslationService.Args("tabCount", 1)));
            Console.WriteLine("tabs-close-warning ($tabCount = 2) = " +
                translator.GetString("tabs-close-warning", TranslationService.Args("tabCount", 2)));

            Console.WriteLine($"sync-dialog-title = {translator.GetString("sync-dialog-title")}");
            Console.WriteLine($"sync-headline-title = {translator.GetString("sync-headline-title")}");
            Console.WriteLine($"sync-signedout-title = {translator.GetString("sync-signedout-title")}");
            Console.WriteLine();
        }

        static void Main()
        {
            RunTest("en");
            RunTest("it");
            RunTest("pl");
        }
    }
}
