using System;
using System.Collections.Generic;
using System.Linq;

namespace ContextFreeSession
{
    internal static class TextUtility
    {
        public static readonly string NewLine = "\n";

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
            var accumlator = "";
            foreach (var line in str.Lines())
            {
                accumlator += (new string(Enumerable.Repeat(' ', spaces).ToArray()) + line).WithNewLine();
            }
            return accumlator.Substring(0, accumlator.Length - NewLine.Length);
        }
    }

    internal static class TypeUtility
    {
        public static string ToNameWithGenericParam(this Type t)
        {
            if (t.ContainsGenericParameters)
            {
                if (t.IsConstructedGenericType)
                {
                    var cs = t.GetGenericParameterConstraints();
                    var s = string.Join(" ,", cs.Select(ToNameWithGenericParam));
                    return $"{t.Name}<{s}>";
                }
                throw new Exception();
            }
            return t.Name;
        }

        public static string TypeSig(this Type t)
        {
            if (t.ContainsGenericParameters)
            {
                if (t.IsConstructedGenericType)
                {
                    var cs = t.GetGenericParameterConstraints();
                    var s = string.Join(" ,", cs.Select(TypeSig));
                    return $"{t.FullName}<{s}>";
                }
                throw new Exception();
            }
            return t.FullName;
        }

        public static string ToSimpleString(this Type type)
        {
            if (typeof(int) == type) return "int";
            if (typeof(uint) == type) return "uint";
            if (typeof(long) == type) return "long";
            if (typeof(ulong) == type) return "ulong";
            if (typeof(float) == type) return "float";
            if (typeof(double) == type) return "double";
            if (typeof(string) == type) return "string";
            return ToNameWithGenericParam(type);
        }
    }
}
