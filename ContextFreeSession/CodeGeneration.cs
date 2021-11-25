using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextFreeSession.Design
{
    public partial class GlobalType
    {
        public string Generate()
        {
            var res = "";

            var locals = Roles.Select(Project);

            foreach (var r in Roles)
            {
                res += $"public struct {r} : IRole {{ }}";
            }

            foreach (var label in Labels)
            {
                res += $"public struct {label} : ILabel {{ public string ToLabelString() {{ return \"{label}\"; }} }}".WithNewLine();
            }

            foreach (var local in locals)
            {
                res += local.Generate();
            }

            return res;
        }
    }

    public partial class LocalType
    {
        public string Generate()
        {
            var res = "";
            foreach (var (key, value) in Rules)
            {
                var S = key;
                var t = (LocalTypeElement)value;
                var className = S;
                //var s = $"public class {className} : {t.ToTypeString()} {{ public {className}() : base({t.ToExp()}) {{ }} }}";
                var s = $"public class {className} : {t.ToTypeString()} {{ }}";
                res += s.WithNewLine();
            }
            return res;
        }
    }
}
