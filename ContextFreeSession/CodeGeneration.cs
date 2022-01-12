using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace ContextFreeSession.Design
{
    public partial class GlobalType
    {
        public string Generate(NewLineMode newLine = NewLineMode.Environment)
        {
            var result = "";
            var locals = Roles.Select(Project);
            foreach (var role in Roles)
            {
                result += $"public struct {role} : IRole {{ }}".WithNewLine().WithNewLine();
            }
            foreach (var label in Labels)
            {
                result += $"public struct {label} : ILabel {{ }}".WithNewLine().WithNewLine();
            }
            foreach (var local in locals)
            {
                result += local.Generate().WithNewLine();
            }
            result = result.TrimNewLines().WithNewLine();
            switch (newLine)
            {
                case NewLineMode.Environment:
                    result = Regex.Replace(result, @"\r\n?|\n", Environment.NewLine);
                    break;
                case NewLineMode.LF:
                    result = Regex.Replace(result, @"\r\n?|\n", "\n");
                    break;
                case NewLineMode.CRLF:
                    result = Regex.Replace(result, @"\r\n?|\n", "\r\n");
                    break;
            }
            return result;
        }
    }

    public partial class LocalType
    {
        public string Generate()
        {
            var result = "";
            foreach (var (nonterminal, body) in Rules)
            {
                var str = $"public class {nonterminal} : {((LocalTypeElement)body).ToTypeString()}, IStart {{ public string Role => \"{Role}\"; }}";
                result += str.WithNewLine().WithNewLine();
            }
            return result.TrimNewLines().WithNewLine();
        }
    }

    public enum NewLineMode
    {
        Environment = 0,
        LF = 1,
        CRLF = 2,
    }
}
