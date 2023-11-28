using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeSession
{
    internal static class TextUtility
    {
        public static readonly string NewLine = Environment.NewLine;

        public static bool IsDigit(this char letter)
        {
            return '0' <= letter && letter <= '9';
        }

        public static bool IsLatin(this char letter)
        {
            return ('A' <= letter && letter <= 'Z') || ('a' <= letter && letter <= 'z');
        }

        public static bool IsAlphanumeric(this string str, params char[] additional)
        {
            return !str.Any(c => !(c.IsDigit() || c.IsLatin() || additional.Contains(c)));
        }

        public static IEnumerable<string> Lines(this string str)
        {
            foreach (var line in str.Split(NewLine))
            {
                yield return line;
            }
        }

        public static string WithNewLine(this string str)
        {
            return str + NewLine;
        }

        public static string TrimNewLines(this string str)
        {
            return str.Trim(NewLine.ToCharArray());
        }

        public static string Indented(this string str, int spaces)
        {
            var accumulator = "";
            foreach (var line in str.Lines())
            {
                accumulator += (new string(Enumerable.Repeat(' ', spaces).ToArray()) + line).WithNewLine();
            }
            return accumulator.Substring(0, accumulator.Length - NewLine.Length);
        }
    }
}
