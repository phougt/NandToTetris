﻿using JackAnalyzer.Modules;
using JackAnalyzer.Models;
using FluentResults;
using JackAnalyzer.Enums;
using System.ComponentModel.Design;
using JackAnalyzer.Errors;
using System.Diagnostics;

namespace JackAnalyzer
{
    class Program
    {
        public static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();
            
            string firstArg = string.Empty;
            string secondArg = string.Empty;

            bool isMultiFilesTranslating = false;

            if (args.Length == 1)
            {
                isMultiFilesTranslating = args[0].Equals("-dir");
                firstArg = args[0];
            }
            else if (args.Length == 2)
            {
                firstArg = args[0];
                secondArg = args[1];
            }
            else
            {
                Console.Error.WriteLine("Arguments Format is expected to be (SourcePath, OutputPath) or (-dir).");
                Environment.Exit(1);
            }

            if (!isMultiFilesTranslating)
            {
                stopwatch.Start();
                JackTokenizer tokenizer = new JackTokenizer(firstArg);
                CompilationEngine engine = new CompilationEngine(tokenizer, secondArg);
                engine.CompileClass();
                if (engine.TryWriteOutputToFile())
                {
                    stopwatch.Stop();
                    long elapsed = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"[Success] Compilation for '{firstArg}' is successful. Took {elapsed} ms to compile.");
                    Environment.Exit(1);
                }
                else
                {
                    Console.Error.WriteLine($"[Error] Compilation for '{firstArg}' is failed.");
                    Environment.Exit(1);
                }
            }
            else
            {
                try
                {
                    string[] files = Directory.GetFiles(Environment.CurrentDirectory, "*.jack");
                    foreach (string file in files)
                    {
                        stopwatch.Start();
                        JackTokenizer tokenizer = new JackTokenizer(file);
                        CompilationEngine engine = new CompilationEngine(tokenizer, file.Replace(".jack", ".xml"));
                        engine.CompileClass();

                        if (engine.TryWriteOutputToFile())
                        {
                            stopwatch.Stop();
                            Console.WriteLine($"[Success] Compilation for '{file}' is successful.");
                        }
                        else
                        {
                            Console.Error.WriteLine($"[Error] Compilation for '{file}' is failed.");
                            Environment.Exit(1);
                        }
                    }

                    stopwatch.Stop();
                    long elapsed = stopwatch.ElapsedMilliseconds;
                    Console.WriteLine($"[Success] Compilation is successful. Took {elapsed} ms to compile.");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[Error] Compilation is failed.");
                    Console.Error.WriteLine(ex.Message);
                    Environment.Exit(1);
                }
            }
        }
    }
}