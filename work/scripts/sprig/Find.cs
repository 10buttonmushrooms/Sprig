// `sprig find`: answers about dump.cs in a few dozen lines. C# 5 only (PowerShell 5.1 Add-Type).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sprig
{
    public static class FindCommand
    {
        public const int DefaultLineCap = 40;

        // Returns the exit code: 0 found, 1 not found, 2 usage error.
        public static int Run(DumpIndex idx, string[] args, StringBuilder output)
        {
            bool all = false, derives = false;
            string rva = null;
            var words = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                var a = args[i];
                if (a == "--all") all = true;
                else if (a == "--derives") derives = true;
                else if (a == "--rva" && i + 1 < args.Length) rva = args[++i];
                else if (a.StartsWith("--")) { output.AppendLine("unknown option " + a); return 2; }
                else words.Add(a);
            }
            var lines = new List<string>();
            int code;
            if (rva != null) code = Rva(idx, rva, lines);
            else if (words.Count != 1) { output.AppendLine(Usage()); return 2; }
            else if (derives) code = Derives(idx, words[0], lines);
            else code = Lookup(idx, words[0], lines);
            Emit(lines, all, words.Count > 0 ? words[0] : "--rva " + rva, output);
            return code;
        }

        public static string Usage()
        {
            return "usage: sprig find <Type | Namespace.Type | Type.Member> [--all]\n" +
                   "       sprig find --derives <Type>\n" +
                   "       sprig find --rva <0xADDRESS>";
        }

        static void Emit(List<string> lines, bool all, string query, StringBuilder output)
        {
            int cap = all ? int.MaxValue : DefaultLineCap;
            for (int i = 0; i < lines.Count && i < cap; i++) output.AppendLine(lines[i]);
            if (lines.Count > cap)
                output.AppendLine("... " + (lines.Count - cap) + " more lines (sprig find " + query + " --all)");
        }

        // ---------------------------------------------------------------- lookup

        static int Lookup(DumpIndex idx, string query, List<string> lines)
        {
            query = query.Trim();
            // 1. A type: full name, nested name or simple name.
            var types = idx.FindByName(query);
            if (types.Count == 1) { TypeCard(idx, types[0], lines); return 0; }
            if (types.Count > 1)
            {
                lines.Add(query + " matches " + types.Count + " types; use the full name:");
                foreach (var t in types) lines.Add("  " + t.DisplayName + "  (" + t.Kind + ", " + t.Image + ")");
                return 0;
            }
            // 2. Type.Member (also Type..ctor).
            string typePart, memberPart;
            if (SplitMember(query, out typePart, out memberPart))
            {
                var owners = idx.FindByName(typePart);
                if (owners.Count > 0)
                {
                    bool any = false;
                    foreach (var owner in owners)
                        if (MemberCards(idx, owner, memberPart, lines)) any = true;
                    if (any) return 0;
                    lines.Add("not in dump.cs: " + query);
                    var candidates = new List<KeyValuePair<string, string>>();
                    foreach (var owner in owners)
                        for (var t = owner; t != null; t = t.BaseType)
                        {
                            foreach (var m in t.Methods) candidates.Add(new KeyValuePair<string, string>(m.Name, t.FullName + "." + m.Name));
                            foreach (var f in t.Fields) candidates.Add(new KeyValuePair<string, string>(f.Name, t.FullName + "." + f.Name));
                            foreach (var p in t.Properties) candidates.Add(new KeyValuePair<string, string>(p.Name, t.FullName + "." + p.Name));
                        }
                    var close = DumpIndex.Closest(memberPart, candidates, 5);
                    if (close.Count > 0) lines.Add("closest: " + string.Join(", ", close.ToArray()));
                    foreach (var owner in owners)
                    {
                        int unread = DumpIndex.UnreadMention(owner, memberPart);
                        if (unread > 0) lines.Add("note: sprig could not read dump.cs:" + unread + ", which mentions " + memberPart + "; check that line");
                    }
                    return 1;
                }
            }
            // 3. Not found: partial matches or the closest names.
            var typeCandidates = new List<KeyValuePair<string, string>>();
            foreach (var t in idx.Types) typeCandidates.Add(new KeyValuePair<string, string>(t.SimpleName, t.FullName));
            var simple = DumpIndex.LastSegment(DumpIndex.StripGenerics(query));
            var partial = DumpIndex.Rank(simple, typeCandidates, 15);
            bool substring = false;
            foreach (var p in partial) if (DumpIndex.LastSegment(p).ToLowerInvariant().Contains(simple.ToLowerInvariant())) { substring = true; break; }
            if (substring)
            {
                lines.Add("no type named " + query + "; partial matches:");
                foreach (var p in partial) lines.Add("  " + p);
            }
            else
            {
                lines.Add("not in dump.cs: " + query);
                var closest = partial.Count > 0 ? partial.GetRange(0, Math.Min(5, partial.Count)) : DumpIndex.Closest(simple, typeCandidates, 5);
                if (closest.Count > 0) lines.Add("closest: " + string.Join(", ", closest.ToArray()));
            }
            int unreadType = DumpIndex.MentionLine(idx.UnparsedTypeText, simple);
            if (unreadType > 0) lines.Add("note: sprig could not read the type declaration at dump.cs:" + unreadType + ", which mentions " + simple + "; check that line");
            return 1;
        }

        static void UnreadNote(DumpType t, List<string> lines, string indent)
        {
            if (t.Unparsed > 0)
                lines.Add(indent + "note: sprig could not read " + t.Unparsed + " line(s) of " + t.FullName + " in dump.cs (first at dump.cs:" + t.FirstUnparsedLine +
                          "); members on those lines are missing here");
        }

        static bool SplitMember(string query, out string typePart, out string memberPart)
        {
            typePart = memberPart = null;
            if (query.EndsWith("..ctor") || query.EndsWith("..cctor"))
            {
                int p = query.LastIndexOf("..");
                typePart = query.Substring(0, p);
                memberPart = query.Substring(p + 1);
                return true;
            }
            var parts = DumpIndex.SplitNested(query);
            if (parts.Count < 2) return false;
            memberPart = parts[parts.Count - 1];
            typePart = string.Join(".", parts.GetRange(0, parts.Count - 1).ToArray());
            if (memberPart == "ctor" || memberPart == "cctor") memberPart = "." + memberPart;
            return true;
        }

        // ---------------------------------------------------------------- cards

        public static string BnmClassExpr(DumpType t)
        {
            if (t.DeclaringType != null)
                return BnmClassExpr(t.DeclaringType) + ".GetInnerClass(\"" + t.SimpleName + "\")";
            var name = t.Name;
            if (t.GenericArity > 0) name += "`" + t.GenericArity;
            return "BNM::Class(\"" + t.Namespace + "\", \"" + name + "\")";
        }

        static string Hex(long v)
        {
            return "0x" + v.ToString("X", CultureInfo.InvariantCulture);
        }

        static void TypeCard(DumpIndex idx, DumpType t, List<string> lines)
        {
            var mods = t.Modifiers.Length > 0 ? t.Modifiers + " " : "";
            lines.Add(t.DisplayName + "  (" + mods + t.Kind + ", " + t.Image + ", TypeDefIndex " + t.TypeDefIndex + ", dump.cs:" + t.Line + ")");
            if (t.BaseType != null || t.Kind == "class")
            {
                var chain = new List<string>();
                for (var c = t.BaseType; c != null; c = c.BaseType) chain.Add(c.GenericParams.Length > 0 ? c.Name + "<" + c.GenericParams + ">" : c.Name);
                if (chain.Count > 0) lines.Add("  base: " + string.Join(" -> ", chain.ToArray()));
            }
            var ifaces = new List<string>();
            foreach (var b in t.Bases)
            {
                var r = idx.ResolveTypeName(b, t);
                if (r == null || r.Kind == "interface") ifaces.Add(b);
            }
            if (ifaces.Count > 0) lines.Add("  implements: " + string.Join(", ", ifaces.ToArray()));
            if (t.DeclaringType != null) lines.Add("  nested in: " + t.DeclaringType.DisplayName);
            if (t.Kind == "class")
            {
                bool approx;
                int size = idx.InstanceSize(t, out approx);
                lines.Add("  instance size: " + Hex(size) + (approx ? " (approximate)" : "") + "; fields start at 0x10 after the object header");
            }
            else if (t.Kind == "struct")
            {
                int size = idx.StructSize(t, 0);
                lines.Add("  struct size: " + (size < 0 ? "unknown (generic)" : size + " bytes") + "; field offsets are from the start of the struct");
            }
            lines.Add("  BNM: " + BnmClassExpr(t));
            if (t.OffsetsUnknown)
                lines.Add("  note: open generic definition; dump.cs prints 0x0 for its field offsets, so they are shown as n/a");
            UnreadNote(t, lines, "  ");

            if (t.Kind == "enum")
            {
                string underlying = "int";
                foreach (var f in t.Fields) if (f.Name == "value__") underlying = f.Type;
                lines.Add("  enum (" + underlying + "):");
                foreach (var kv in idx.EnumMembers(t)) lines.Add("    " + kv.Key + " = " + kv.Value);
                return;
            }

            var fieldLines = new List<string>();
            var chainTypes = new List<DumpType>();
            for (var c = t; c != null; c = c.BaseType) chainTypes.Insert(0, c);
            foreach (var c in chainTypes)
            {
                foreach (var f in c.Fields)
                {
                    if (f.IsStatic || f.IsConst) continue;
                    var off = c.OffsetsUnknown || f.Offset < 0 ? "n/a   " : Hex(f.Offset).PadRight(6);
                    var from = c == t ? "" : "   (from " + c.Name + ")";
                    fieldLines.Add("    " + off + "  " + f.Type + " " + f.Name + from);
                }
            }
            foreach (var f in t.Fields)
            {
                if (f.IsConst) fieldLines.Add("    const   " + f.Type + " " + f.Name + " = " + f.ConstValue);
                else if (f.IsStatic) fieldLines.Add("    static  " + f.Type + " " + f.Name);
            }
            if (fieldLines.Count > 0) { lines.Add("  fields:"); lines.AddRange(fieldLines); }

            if (t.Properties.Count > 0)
            {
                lines.Add("  properties:");
                foreach (var p in t.Properties)
                    lines.Add("    " + p.Type + " " + p.Name + " {" + (p.HasGet ? " get;" : "") + (p.HasSet ? " set;" : "") + " }");
            }
            if (t.Methods.Count > 0)
            {
                lines.Add("  methods:");
                foreach (var m in t.Methods) lines.Add("    " + MethodLine(m));
            }
        }

        static string MethodLine(DumpMethod m)
        {
            var sb = new StringBuilder();
            if (m.Slot >= 0) sb.Append("[slot ").Append(m.Slot).Append("] ");
            if (m.IsStatic) sb.Append("static ");
            if (m.IsAbstract) sb.Append("abstract ");
            else if (m.IsOverride) sb.Append("override ");
            else if (m.IsVirtual) sb.Append("virtual ");
            sb.Append(m.Signature());
            sb.Append(m.Rva > 0 ? "   RVA " + Hex(m.Rva) : "   (no body)");
            return sb.ToString();
        }

        static bool MemberCards(DumpIndex idx, DumpType owner, string member, List<string> lines)
        {
            bool found = false;
            for (var t = owner; t != null; t = t.BaseType)
            {
                var overloads = new List<DumpMethod>();
                foreach (var m in t.Methods) if (m.Name == member) overloads.Add(m);
                foreach (var m in overloads) { MethodCard(idx, owner, m, overloads, lines); found = true; }
                foreach (var f in t.Fields) if (f.Name == member) { FieldCard(idx, owner, f, lines); found = true; }
                foreach (var p in t.Properties) if (p.Name == member) { PropertyCard(idx, owner, p, lines); found = true; }
                if (found) return true;
            }
            foreach (var iface in owner.Interfaces)
                foreach (var m in iface.Methods)
                    if (m.Name == member) { MethodCard(idx, iface, m, new List<DumpMethod> { m }, lines); found = true; }
            return found;
        }

        static void MethodCard(DumpIndex idx, DumpType queried, DumpMethod m, List<DumpMethod> overloads, List<string> lines)
        {
            if (lines.Count > 0) lines.Add("");
            var kind = new List<string>();
            kind.Add(m.IsStatic ? "static" : "instance");
            if (m.IsAbstract) kind.Add("abstract");
            else if (m.IsVirtual) kind.Add("virtual");
            if (m.Slot >= 0) kind.Add("slot " + m.Slot);
            kind.Add(m.Rva > 0 ? "RVA " + Hex(m.Rva) : "no body");
            kind.Add("dump.cs:" + m.Line);
            lines.Add(m.Owner.FullName + "." + m.Name + "  (" + string.Join(", ", kind.ToArray()) + ")");
            if (m.Owner != queried) lines.Add("  declared in " + m.Owner.FullName + " (base of " + queried.FullName + ")");
            lines.Add("  C#:   " + (m.Modifiers.Length > 0 ? m.Modifiers + " " : "") + m.Signature());
            var sameCount = new List<DumpMethod>();
            foreach (var o in overloads) if (o.Params.Count == m.Params.Count) sameCount.Add(o);
            string lookup;
            if (sameCount.Count > 1)
            {
                var names = new List<string>();
                foreach (var p in m.Params) names.Add("\"" + p.Name + "\"");
                lookup = ".GetMethod(\"" + m.Name + "\", {" + string.Join(", ", names.ToArray()) + "})";
            }
            else lookup = ".GetMethod(\"" + m.Name + "\", " + m.Params.Count + ")";
            lines.Add("  BNM:  " + BnmClassExpr(m.Owner) + lookup);
            var notes = new List<string>();
            lines.Add("  hook: " + idx.HookSignature(m, notes));
            if (m.Rva < 0 && !m.IsAbstract && m.Owner.Kind != "interface")
                notes.Add("no body in this build (abstract or stripped): hook an implementation instead (sprig find --derives " + m.Owner.Name + ")");
            if (m.Owner.Kind == "interface")
                notes.Add("interface method: hook each implementation (sprig find --derives " + m.Owner.Name + ")");
            if (m.IsVirtual && !m.IsStatic)
                notes.Add("virtual: BasicHook patches this body only; overrides in subclasses are separate methods");
            if (sameCount.Count > 1)
                notes.Add(sameCount.Count + " overloads take " + m.Params.Count + (m.Params.Count == 1 ? " parameter" : " parameters") + "; GetMethod(name, count) returns the first, so look it up by parameter names as shown");
            if (overloads.Count > 1)
            {
                var others = new List<string>();
                foreach (var o in overloads) if (o != m) others.Add(o.Name + "(" + o.Params.Count + ")");
                lines.Add("  other overloads: " + string.Join(", ", others.ToArray()));
            }
            foreach (var n in notes) lines.Add("  note: " + n);
        }

        static void FieldCard(DumpIndex idx, DumpType queried, DumpField f, List<string> lines)
        {
            if (lines.Count > 0) lines.Add("");
            var where = f.IsConst ? "const = " + f.ConstValue : (f.IsStatic ? "static" : (f.Owner.OffsetsUnknown ? "offset n/a (open generic)" : "offset " + Hex(f.Offset)));
            lines.Add(f.Owner.FullName + "." + f.Name + "  (field, " + where + ", dump.cs:" + f.Line + ")");
            if (f.Owner != queried) lines.Add("  declared in " + f.Owner.FullName + " (base of " + queried.FullName + ")");
            lines.Add("  C#:   " + (f.Modifiers.Length > 0 ? f.Modifiers + " " : "") + f.Type + " " + f.Name);
            var notes = new List<string>();
            var cpp = idx.CppType(f.Type, "", f.Owner, notes);
            lines.Add("  C++:  " + cpp);
            if (!f.IsConst) lines.Add("  BNM:  " + BnmClassExpr(f.Owner) + ".GetField(\"" + f.Name + "\")   (offset at run time: .GetOffset())");
            var ft = idx.ResolveTypeName(f.Type, f.Owner);
            if (ft != null && ft.Kind == "enum")
            {
                var members = new List<string>();
                foreach (var kv in idx.EnumMembers(ft)) members.Add(kv.Key + " = " + kv.Value);
                lines.Add("  enum " + ft.Name + ": " + string.Join(", ", members.ToArray()));
            }
            if (!f.IsConst && !f.IsStatic && (cpp.StartsWith("void *") || cpp.Contains("Mono::")))
                notes.Add("reference field: store a new reference through il2cpp_gc_wbarrier_set_field (see docs/agents/hooks.md#write-barrier)");
            foreach (var n in notes) lines.Add("  note: " + n);
        }

        static void PropertyCard(DumpIndex idx, DumpType queried, DumpProperty p, List<string> lines)
        {
            if (lines.Count > 0) lines.Add("");
            lines.Add(p.Owner.FullName + "." + p.Name + "  (property " + p.Type + " {" + (p.HasGet ? " get;" : "") + (p.HasSet ? " set;" : "") + " })");
            lines.Add("  BNM:  " + BnmClassExpr(p.Owner) + ".GetProperty(\"" + p.Name + "\")");
            foreach (var m in p.Owner.Methods)
            {
                if (m.Name == "get_" + p.Name || m.Name == "set_" + p.Name)
                {
                    lines.Add("  " + m.Name + ": " + MethodLine(m));
                    lines.Add("    hook: " + idx.HookSignature(m, null));
                }
            }
            var backing = "<" + p.Name + ">k__BackingField";
            foreach (var f in p.Owner.Fields)
                if (f.Name == backing)
                    lines.Add("  backing field: " + backing + " at " + (p.Owner.OffsetsUnknown ? "n/a" : Hex(f.Offset)));
        }

        // ---------------------------------------------------------------- --derives and --rva

        static int Derives(DumpIndex idx, string query, List<string> lines)
        {
            var types = idx.FindByName(query);
            if (types.Count == 0)
            {
                Lookup(idx, query, lines);
                return 1;
            }
            foreach (var baseType in types)
            {
                var derived = idx.DerivedFrom(baseType);
                lines.Add(derived.Count + " types derive from or implement " + baseType.DisplayName + ":");
                derived.Sort((a, b) => string.CompareOrdinal(a.FullName, b.FullName));
                foreach (var d in derived)
                {
                    var own = new List<string>();
                    foreach (var f in d.Fields) if (!f.IsStatic && !f.IsConst) own.Add(f.Name + " (" + f.Type + ")");
                    lines.Add("  " + d.FullName + (own.Count > 0 ? "  { " + string.Join(", ", own.ToArray()) + " }" : ""));
                }
            }
            return 0;
        }

        static int Rva(DumpIndex idx, string text, List<string> lines)
        {
            long rva;
            var t = text.Trim();
            if (t.StartsWith("0x") || t.StartsWith("0X")) t = t.Substring(2);
            if (!long.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out rva))
            {
                lines.Add("not a hex address: " + text);
                return 2;
            }
            long start;
            var m = idx.MethodAtRva(rva, out start);
            var g = idx.GenericAtRva(rva);
            if (g != null && (m == null || g.Rva > start))
            {
                lines.Add(Hex(rva) + " is in " + g.Name + " (+" + Hex(rva - g.Rva) + "), a generic instantiation starting at RVA " + Hex(g.Rva));
                if (g.Definition != null) lines.Add("  definition: " + g.Definition.Owner.FullName + "." + g.Definition.Signature());
                return 0;
            }
            if (m == null)
            {
                lines.Add(Hex(rva) + " is before the first method in dump.cs");
                return 1;
            }
            long next = idx.NextMethodRva(start);
            lines.Add(Hex(rva) + " is in " + m.Owner.FullName + "." + m.Name + " (+" + Hex(rva - start) + "), which starts at RVA " + Hex(start) + (next > 0 ? " (next method at " + Hex(next) + ")" : ""));
            lines.Add("  C#: " + m.Signature() + "   dump.cs:" + m.Line);
            if (next > 0 && rva - start > 0x4000)
                lines.Add("  note: far from the method start; the address may be in code dump.cs does not list (runtime, stubs)");
            return 0;
        }
    }
}
