using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ContextFreeSession
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
            foreach (var r in Rules)
            {
                var S = r.Key;
                var t = (LocalTypeElement)r.Value;
                var className = S;
                //var s = $"public class {className} : {t.ToTypeString()} {{ public {className}() : base({t.ToExp()}) {{ }} }}";
                var s = $"public class {className} : {t.ToTypeString()} {{ }}";
                res += s.WithNewLine();
            }
            return res;
        }
    }
}
