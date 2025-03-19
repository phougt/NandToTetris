using JackAnalyzer.Modules;
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
        static void Main(string[] args)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            string filePath = "C:\\Users\\phoug\\Desktop\\test.txt";

            JackTokenizer tokenizer = new JackTokenizer(filePath);
            CompilationEngine engine = new CompilationEngine(tokenizer);
            engine.CompileClass();

            Console.WriteLine(watch.Elapsed);
        }
    }
}