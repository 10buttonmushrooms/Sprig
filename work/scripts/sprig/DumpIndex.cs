// Index of work/dump/dump.cs (Il2CppDumper output) for `sprig find` and `sprig lint`.
// C# 5 only: Windows PowerShell 5.1 compiles this with Add-Type.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Sprig
{
    public sealed class DumpParam
    {
        public string Modifier = "";
        public string Type = "";
        public string Name = "";
    }

    public sealed class DumpField
    {
        public string Name = "";
        public string Type = "";
        public string Modifiers = "";
        public int Offset = -1;
        public bool IsStatic;
        public bool IsConst;
        public string ConstValue = "";
        public int Line;
        public DumpType Owner;
    }

    public sealed class DumpMethod
    {
        public string Name = "";
        public string GenericParams = "";
        public string ReturnType = "";
        public string Modifiers = "";
        public List<DumpParam> Params = new List<DumpParam>();
        public bool IsStatic;
        public bool IsVirtual;
        public bool IsAbstract;
        public bool IsOverride;
        public int Slot = -1;
        public long Rva = -1;
        public int Line;
        public DumpType Owner;

        public string Signature()
        {
            var sb = new StringBuilder();
            sb.Append(ReturnType).Append(' ').Append(Name);
            if (GenericParams.Length > 0) sb.Append('<').Append(GenericParams).Append('>');
            sb.Append('(');
            for (int i = 0; i < Params.Count; i++)
            {
                if (i > 0) sb.Append(", ");
                if (Params[i].Modifier.Length > 0) sb.Append(Params[i].Modifier).Append(' ');
                sb.Append(Params[i].Type).Append(' ').Append(Params[i].Name);
            }
            sb.Append(')');
            return sb.ToString();
        }
    }

    public sealed class DumpProperty
    {
        public string Name = "";
        public string Type = "";
        public string Modifiers = "";
        public bool HasGet;
        public bool HasSet;
        public DumpType Owner;
    }

    public sealed class DumpType
    {
        public string Namespace = "";
        public string Name = "";            // "Outer.Inner" for nested types, without generic parameters
        public string GenericParams = "";   // "TKey, TValue"
        public string Kind = "";            // class, struct, enum, interface
        public string Modifiers = "";
        public string Image = "";
        public int TypeDefIndex = -1;
        public int Line;
        public int Order;
        public List<string> Bases = new List<string>();
        public DumpType BaseType;
        public List<DumpType> Interfaces = new List<DumpType>();
        public DumpType DeclaringType;
        public List<DumpField> Fields = new List<DumpField>();
        public List<DumpMethod> Methods = new List<DumpMethod>();
        public List<DumpProperty> Properties = new List<DumpProperty>();
        // Member lines in this type's body that the parser did not understand; members may be missing.
        public int Unparsed;
        public int FirstUnparsedLine;
        public string UnparsedText = "";

        public string FullName
        {
            get { return Namespace.Length > 0 ? Namespace + "." + Name : Name; }
        }

        public string DisplayName
        {
            get { return GenericParams.Length > 0 ? FullName + "<" + GenericParams + ">" : FullName; }
        }

        public string SimpleName
        {
            get
            {
                var parts = DumpIndex.SplitNested(Name);
                return parts[parts.Count - 1];
            }
        }

        public bool IsNested
        {
            get { return DumpIndex.SplitNested(Name).Count > 1; }
        }

        public int GenericArity
        {
            get { return GenericParams.Length == 0 ? 0 : DumpIndex.SplitTopLevel(GenericParams, ',').Count; }
        }

        public bool IsValueType
        {
            get { return Kind == "struct" || Kind == "enum"; }
        }

        // Il2CppDumper prints 0x0 for every field offset of an open generic definition.
        public bool OffsetsUnknown
        {
            get
            {
                if (GenericParams.Length == 0) return false;
                int instance = 0, zero = 0;
                foreach (var f in Fields)
                {
                    if (f.IsStatic || f.IsConst) continue;
                    instance++;
                    if (f.Offset == 0) zero++;
                }
                return instance > 0 && zero == instance && (instance > 1 || Kind == "class");
            }
        }

        public bool Implements(string simpleName)
        {
            for (var t = this; t != null; t = t.BaseType)
            {
                foreach (var b in t.Bases)
                    if (DumpIndex.StripGenerics(b) == simpleName || DumpIndex.LastSegment(DumpIndex.StripGenerics(b)) == simpleName) return true;
            }
            return false;
        }
    }

    public sealed class GenericInstance
    {
        public long Rva;
        public string Name = "";
        public DumpMethod Definition;
    }

    public sealed class DumpIndex
    {
        public const int FormatVersion = 8;

        public List<DumpType> Types = new List<DumpType>();
        // Type declarations the parser did not understand; those types are missing from the index.
        public int UnparsedTypes;
        public int FirstUnparsedTypeLine;
        public string UnparsedTypeText = "";

        // Unread lines are kept as "<line number>\t<text>\n". Returns the dump.cs line number of the
        // first one that mentions name as a whole word, or 0: such a name may exist after all.
        public static int MentionLine(string unread, string name)
        {
            name = name.Split('`')[0];
            if (name.Length == 0) return 0;
            foreach (var entry in unread.Split('\n'))
            {
                int tab = entry.IndexOf('\t');
                if (tab < 0) continue;
                var text = entry.Substring(tab + 1);
                for (int p = text.IndexOf(name, StringComparison.Ordinal); p >= 0; p = text.IndexOf(name, p + 1, StringComparison.Ordinal))
                {
                    bool before = p == 0 || !(char.IsLetterOrDigit(text[p - 1]) || text[p - 1] == '_');
                    int end = p + name.Length;
                    bool after = end >= text.Length || !(char.IsLetterOrDigit(text[end]) || text[end] == '_');
                    int line;
                    if (before && after && int.TryParse(entry.Substring(0, tab), out line)) return line;
                }
            }
            return 0;
        }

        // The dump.cs line of an unread line in owner or its bases that mentions member, or 0.
        public static int UnreadMention(DumpType owner, string member)
        {
            for (var t = owner; t != null; t = t.BaseType)
            {
                int line = t.Unparsed > 0 ? MentionLine(t.UnparsedText, member) : 0;
                if (line > 0) return line;
            }
            return 0;
        }
        public List<GenericInstance> GenericInstances = new List<GenericInstance>();
        public List<KeyValuePair<string, int>> Images = new List<KeyValuePair<string, int>>();
        public string SourceHash = "";

        Dictionary<string, List<DumpType>> byFull;
        Dictionary<string, List<DumpType>> byName;
        Dictionary<string, List<DumpType>> bySimple;
        List<DumpMethod> methodsByRva;

        // ---------------------------------------------------------------- text helpers

        public static List<string> SplitTopLevel(string text, char separator)
        {
            var result = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<' || c == '(' || c == '[' || c == '{') depth++;
                else if (c == '>' || c == ')' || c == ']' || c == '}') { if (depth > 0) depth--; }
                else if (c == separator && depth == 0)
                {
                    var part = text.Substring(start, i - start).Trim();
                    if (part.Length > 0) result.Add(part);
                    start = i + 1;
                }
            }
            var last = text.Substring(start).Trim();
            if (last.Length > 0) result.Add(last);
            return result;
        }

        // Splits "Outer.Inner.<>c" on dots outside generic arguments.
        public static List<string> SplitNested(string name)
        {
            var result = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (c == '<') depth++;
                else if (c == '>') { if (depth > 0) depth--; }
                else if (c == '.' && depth == 0)
                {
                    result.Add(name.Substring(start, i - start));
                    start = i + 1;
                }
            }
            result.Add(name.Substring(start));
            return result;
        }

        public static string StripGenerics(string typeName)
        {
            var sb = new StringBuilder();
            int depth = 0;
            for (int i = 0; i < typeName.Length; i++)
            {
                char c = typeName[i];
                bool genericOpen = c == '<' && i > 0 && IsIdentChar(typeName[i - 1]);
                if (genericOpen || (c == '<' && depth > 0)) { depth++; continue; }
                if (c == '>' && depth > 0) { depth--; continue; }
                if (depth == 0) sb.Append(c);
            }
            return sb.ToString().Trim();
        }

        public static string LastSegment(string name)
        {
            var parts = SplitNested(name);
            return parts[parts.Count - 1];
        }

        public static bool IsIdentChar(char c)
        {
            return char.IsLetterOrDigit(c) || c == '_' || c == '`';
        }

        // "Dictionary<TKey, TValue>" -> name "Dictionary", params "TKey, TValue".
        // "<>c" and "<Method>d__5" are names, not generic arguments.
        public static void SplitGenericName(string text, out string name, out string generics)
        {
            name = text;
            generics = "";
            if (!text.EndsWith(">")) return;
            int depth = 0;
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if (text[i] == '>') depth++;
                else if (text[i] == '<')
                {
                    depth--;
                    if (depth == 0)
                    {
                        if (i > 0 && IsIdentChar(text[i - 1]))
                        {
                            name = text.Substring(0, i);
                            generics = text.Substring(i + 1, text.Length - i - 2);
                        }
                        return;
                    }
                }
            }
        }

        static List<string> SplitWords(string text)
        {
            // Spaces outside <...>, (...) and [...].
            var result = new List<string>();
            int depth = 0, start = 0;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == '<' || c == '(' || c == '[') depth++;
                else if (c == '>' || c == ')' || c == ']') { if (depth > 0) depth--; }
                else if (c == ' ' && depth == 0)
                {
                    if (i > start) result.Add(text.Substring(start, i - start));
                    start = i + 1;
                }
            }
            if (start < text.Length) result.Add(text.Substring(start));
            return result;
        }

        public static readonly Dictionary<string, string> Aliases = new Dictionary<string, string> {
            { "string", "String" }, { "object", "Object" }, { "bool", "Boolean" }, { "byte", "Byte" }, { "sbyte", "SByte" },
            { "char", "Char" }, { "short", "Int16" }, { "ushort", "UInt16" }, { "int", "Int32" }, { "uint", "UInt32" },
            { "long", "Int64" }, { "ulong", "UInt64" }, { "float", "Single" }, { "double", "Double" }, { "decimal", "Decimal" }
        };

        static readonly HashSet<string> MemberModifiers = new HashSet<string> {
            "public", "private", "protected", "internal", "static", "readonly", "const", "volatile",
            "virtual", "abstract", "override", "sealed", "extern", "new", "unsafe", "fixed", "event", "async"
        };

        static long ParseHex(string text)
        {
            text = text.Trim();
            if (text == "-1") return -1;
            if (text.StartsWith("0x") || text.StartsWith("0X")) text = text.Substring(2);
            long value;
            return long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value) ? value : -1;
        }

        // ---------------------------------------------------------------- parsing

        public static DumpIndex Parse(string path)
        {
            var index = new DumpIndex();
            index.SourceHash = HashFile(path);
            string pendingNamespace = "";
            DumpType current = null;
            long pendingRva = -1;
            int pendingSlot = -1;
            bool inGenericBlock = false;
            bool inAttribute = false;
            long genericRva = -1;
            DumpMethod lastMethod = null;
            int lineNo = 0;
            var unreadTypes = new StringBuilder();
            var unread = new Dictionary<DumpType, StringBuilder>();

            foreach (var raw in File.ReadLines(path))
            {
                lineNo++;
                string line = raw;
                // An attribute whose string argument contains line breaks, such as [Tooltip("a\nb")],
                // continues until a line that ends with "]".
                if (inAttribute)
                {
                    if (line.TrimEnd().EndsWith("]")) inAttribute = false;
                    continue;
                }
                if (line.TrimStart().StartsWith("[") && !line.TrimEnd().EndsWith("]"))
                {
                    inAttribute = true;
                    continue;
                }
                if (inGenericBlock)
                {
                    var t = line.Trim();
                    if (t == "*/") { inGenericBlock = false; continue; }
                    if (t.StartsWith("|-RVA:"))
                    {
                        var rest = t.Substring(6).Trim();
                        int sp = rest.IndexOf(' ');
                        genericRva = ParseHex(sp > 0 ? rest.Substring(0, sp) : rest);
                    }
                    else if (t.StartsWith("|-") && genericRva >= 0)
                    {
                        var gi = new GenericInstance();
                        gi.Rva = genericRva;
                        gi.Name = t.Substring(2).Trim();
                        gi.Definition = lastMethod;
                        index.GenericInstances.Add(gi);
                    }
                    continue;
                }

                if (line.StartsWith("// Image "))
                {
                    // "// Image 7: CardsCoreLib.dll - 12682"
                    int colon = line.IndexOf(':');
                    int dash = line.LastIndexOf(" - ");
                    if (colon > 0 && dash > colon)
                    {
                        int start;
                        if (int.TryParse(line.Substring(dash + 3).Trim(), out start))
                            index.Images.Add(new KeyValuePair<string, int>(line.Substring(colon + 1, dash - colon - 1).Trim(), start));
                    }
                    continue;
                }
                if (line.StartsWith("// Namespace:"))
                {
                    pendingNamespace = line.Substring(13).Trim();
                    continue;
                }
                if (current == null)
                {
                    int tdi = line.IndexOf("// TypeDefIndex:");
                    if (tdi > 0 && !line.StartsWith("//") && !line.StartsWith("["))
                    {
                        current = ParseTypeLine(line, tdi);
                        if (current != null)
                        {
                            current.Namespace = pendingNamespace;
                            current.Line = lineNo;
                            current.Order = index.Types.Count;
                            index.Types.Add(current);
                        }
                        else
                        {
                            if (index.UnparsedTypes++ == 0) index.FirstUnparsedTypeLine = lineNo;
                            unreadTypes.Append(lineNo).Append('\t').Append(line.Trim()).Append('\n');
                        }
                        pendingNamespace = "";
                    }
                    continue;
                }
                // Empty types are printed with "{}" on one line instead of "{" ... "}".
                if (line == "}" || line == "{}")
                {
                    current = null;
                    pendingRva = -1;
                    pendingSlot = -1;
                    continue;
                }
                if (line.Length == 0 || line == "{") continue;
                var s = line.Trim();
                if (s.StartsWith("/* GenericInstMethod")) { inGenericBlock = true; genericRva = -1; continue; }
                if (s.StartsWith("// RVA:"))
                {
                    // "// RVA: 0x2280DBC Offset: 0x227CDBC VA: 0x2280DBC Slot: 6" or "// RVA: -1 Offset: -1 Slot: 0"
                    var parts = s.Substring(7).Trim().Split(' ');
                    pendingRva = parts.Length > 0 ? ParseHex(parts[0]) : -1;
                    pendingSlot = -1;
                    for (int i = 0; i + 1 < parts.Length; i++)
                        if (parts[i] == "Slot:") int.TryParse(parts[i + 1], out pendingSlot);
                    continue;
                }
                if (s.StartsWith("//") || s.StartsWith("[")) continue;

                // Methods end with "{ }"; abstract and interface methods end with ");" instead. Default
                // parameter values contain " = " inside the parentheses; a field initializer has it before them.
                int paren = s.IndexOf('(');
                bool isMethod = paren > 0 && (s.EndsWith("{ }") || (s.EndsWith(");") && s.Substring(0, paren).IndexOf(" = ") < 0));
                if (isMethod)
                {
                    var m = ParseMethodLine(s.EndsWith(");") ? s.Substring(0, s.Length - 1) + " { }" : s);
                    if (m != null)
                    {
                        m.Rva = pendingRva;
                        m.Slot = pendingSlot;
                        m.Line = lineNo;
                        m.Owner = current;
                        if (pendingRva < 0 && current.Kind != "interface") m.IsAbstract = m.IsAbstract || m.Modifiers.Contains("abstract");
                        current.Methods.Add(m);
                        lastMethod = m;
                    }
                    else Unread(unread, current, lineNo, s);
                    pendingRva = -1;
                    pendingSlot = -1;
                    continue;
                }
                int brace = s.IndexOf(" { ");
                if (brace > 0 && (s.Contains("get;") || s.Contains("set;")))
                {
                    var p = ParsePropertyLine(s, brace);
                    if (p != null) { p.Owner = current; current.Properties.Add(p); }
                    else Unread(unread, current, lineNo, s);
                    continue;
                }
                // Fields with initial data carry " /*Metadata offset 0x8B77B8*/" before the ";".
                int meta = s.IndexOf(" /*Metadata offset ");
                if (meta > 0)
                {
                    int metaEnd = s.IndexOf("*/", meta);
                    if (metaEnd > meta) s = s.Substring(0, meta) + s.Substring(metaEnd + 2);
                }
                DumpField field = s.EndsWith(";") || s.Contains("; //") ? ParseFieldLine(s) : null;
                if (field != null) { field.Line = lineNo; field.Owner = current; current.Fields.Add(field); }
                else Unread(unread, current, lineNo, s);
            }
            index.UnparsedTypeText = unreadTypes.ToString();
            foreach (var kv in unread) kv.Key.UnparsedText = kv.Value.ToString();
            index.Resolve();
            return index;
        }

        static void Unread(Dictionary<DumpType, StringBuilder> unread, DumpType t, int lineNo, string text)
        {
            StringBuilder sb;
            if (t.Unparsed++ == 0)
            {
                t.FirstUnparsedLine = lineNo;
                unread[t] = sb = new StringBuilder();
            }
            else sb = unread[t];
            sb.Append(lineNo).Append('\t').Append(text).Append('\n');
        }

        static DumpType ParseTypeLine(string line, int tdiPos)
        {
            var t = new DumpType();
            int tdi;
            int.TryParse(line.Substring(tdiPos + 16).Trim(), out tdi);
            t.TypeDefIndex = tdi;
            var decl = line.Substring(0, tdiPos).Trim();
            string[] kinds = { "class", "struct", "enum", "interface" };
            int kindPos = -1;
            string kind = null;
            foreach (var k in kinds)
            {
                int p = 0;
                while ((p = decl.IndexOf(k + " ", p)) >= 0)
                {
                    if (p == 0 || decl[p - 1] == ' ')
                    {
                        if (kindPos < 0 || p < kindPos) { kindPos = p; kind = k; }
                        break;
                    }
                    p++;
                }
            }
            if (kind == null) return null;
            t.Kind = kind;
            t.Modifiers = decl.Substring(0, kindPos).Trim();
            var rest = decl.Substring(kindPos + kind.Length).Trim();
            // The name ends at the first space or ':' outside <...>.
            int depth = 0, end = rest.Length;
            for (int i = 0; i < rest.Length; i++)
            {
                char c = rest[i];
                if (c == '<') depth++;
                else if (c == '>') { if (depth > 0) depth--; }
                else if ((c == ' ' || c == ':') && depth == 0) { end = i; break; }
            }
            string name, generics;
            SplitGenericName(rest.Substring(0, end), out name, out generics);
            // Nested types of System.String and friends are printed with the C# alias: "string.TrimType".
            var segments = SplitNested(name);
            string real;
            if (segments.Count > 1 && Aliases.TryGetValue(segments[0], out real))
            {
                segments[0] = real;
                name = string.Join(".", segments.ToArray());
            }
            t.Name = name;
            t.GenericParams = generics;
            var after = rest.Substring(end).Trim();
            if (after.StartsWith(":"))
                t.Bases = SplitTopLevel(after.Substring(1), ',');
            return t;
        }

        static DumpMethod ParseMethodLine(string s)
        {
            s = s.Substring(0, s.Length - 3).TrimEnd();
            if (!s.EndsWith(")")) return null;
            int depth = 0, open = -1;
            for (int i = s.Length - 1; i >= 0; i--)
            {
                if (s[i] == ')') depth++;
                else if (s[i] == '(') { depth--; if (depth == 0) { open = i; break; } }
            }
            if (open <= 0) return null;
            var head = SplitWords(s.Substring(0, open).Trim());
            if (head.Count < 2) return null;
            var m = new DumpMethod();
            string name, generics;
            SplitGenericName(head[head.Count - 1], out name, out generics);
            m.Name = name;
            m.GenericParams = generics;
            m.ReturnType = head[head.Count - 2];
            var mods = new StringBuilder();
            for (int i = 0; i < head.Count - 2; i++) { if (i > 0) mods.Append(' '); mods.Append(head[i]); }
            m.Modifiers = mods.ToString();
            var modSet = new HashSet<string>(head.GetRange(0, head.Count - 2));
            m.IsStatic = modSet.Contains("static");
            m.IsVirtual = modSet.Contains("virtual") || modSet.Contains("abstract") || modSet.Contains("override");
            m.IsAbstract = modSet.Contains("abstract");
            m.IsOverride = modSet.Contains("override");
            var plist = s.Substring(open + 1, s.Length - open - 2);
            foreach (var p in SplitTopLevel(plist, ','))
            {
                var text = p;
                int eq = IndexOfTopLevel(text, " = ");
                if (eq > 0) text = text.Substring(0, eq);
                var words = SplitWords(text.Trim());
                var param = new DumpParam();
                int w = 0;
                while (w < words.Count - 2 && (words[w] == "out" || words[w] == "ref" || words[w] == "in" || words[w] == "params" || words[w] == "this"))
                {
                    param.Modifier = param.Modifier.Length > 0 ? param.Modifier + " " + words[w] : words[w];
                    w++;
                }
                if (words.Count - w >= 2)
                {
                    param.Type = string.Join(" ", words.GetRange(w, words.Count - w - 1).ToArray());
                    param.Name = words[words.Count - 1];
                }
                else if (words.Count - w == 1)
                {
                    param.Type = words[w];
                }
                m.Params.Add(param);
            }
            return m;
        }

        static int IndexOfTopLevel(string text, string token)
        {
            int depth = 0;
            for (int i = 0; i + token.Length <= text.Length; i++)
            {
                char c = text[i];
                if (c == '<' || c == '(' || c == '[') depth++;
                else if (c == '>' || c == ')' || c == ']') { if (depth > 0) depth--; }
                else if (depth == 0 && string.CompareOrdinal(text, i, token, 0, token.Length) == 0) return i;
            }
            return -1;
        }

        static DumpProperty ParsePropertyLine(string s, int brace)
        {
            var words = SplitWords(s.Substring(0, brace).Trim());
            if (words.Count < 2) return null;
            var p = new DumpProperty();
            p.Name = words[words.Count - 1];
            p.Type = words[words.Count - 2];
            p.Modifiers = string.Join(" ", words.GetRange(0, words.Count - 2).ToArray());
            var body = s.Substring(brace);
            p.HasGet = body.Contains("get;");
            p.HasSet = body.Contains("set;");
            return p;
        }

        static DumpField ParseFieldLine(string s)
        {
            var f = new DumpField();
            int comment = s.IndexOf("; //");
            string decl;
            if (comment >= 0)
            {
                decl = s.Substring(0, comment);
                var c = s.Substring(comment + 4).Trim();
                if (c.StartsWith("0x")) f.Offset = (int)ParseHex(c);
            }
            else decl = s.TrimEnd(';');
            int eq = IndexOfTopLevel(decl, " = ");
            if (eq > 0)
            {
                f.ConstValue = decl.Substring(eq + 3).Trim();
                decl = decl.Substring(0, eq);
            }
            var words = SplitWords(decl.Trim());
            if (words.Count < 2) return null;
            f.Name = words[words.Count - 1];
            int bracket = f.Name.IndexOf('[');
            if (bracket > 0) f.Name = f.Name.Substring(0, bracket);
            f.Type = words[words.Count - 2];
            var mods = words.GetRange(0, words.Count - 2);
            if (mods.Contains("event")) return null;
            foreach (var m in mods) if (!MemberModifiers.Contains(m)) return null;
            f.Modifiers = string.Join(" ", mods.ToArray());
            f.IsStatic = mods.Contains("static");
            f.IsConst = mods.Contains("const");
            return f;
        }

        // ---------------------------------------------------------------- resolution

        void Resolve()
        {
            Images.Sort((a, b) => a.Value.CompareTo(b.Value));
            foreach (var t in Types)
            {
                foreach (var img in Images) if (img.Value <= t.TypeDefIndex) t.Image = img.Key;
            }
            BuildMaps();
            // Nested types carry an empty namespace and precede their declaring type in the file.
            for (int i = Types.Count - 1; i >= 0; i--) ResolveDeclaring(Types[i], 0);
            BuildMaps();
            foreach (var t in Types) ResolveBases(t);
            methodsByRva = new List<DumpMethod>();
            foreach (var t in Types) foreach (var m in t.Methods) if (m.Rva > 0) methodsByRva.Add(m);
            methodsByRva.Sort((a, b) => a.Rva.CompareTo(b.Rva));
            GenericInstances.Sort((a, b) => a.Rva.CompareTo(b.Rva));
        }

        void ResolveDeclaring(DumpType t, int guard)
        {
            if (t.DeclaringType != null || guard > 16) return;
            var parts = SplitNested(t.Name);
            if (parts.Count < 2) return;
            var outerName = string.Join(".", parts.GetRange(0, parts.Count - 1).ToArray());
            List<DumpType> candidates;
            if (!byName.TryGetValue(outerName, out candidates)) return;
            DumpType best = null;
            int bestDistance = int.MaxValue;
            foreach (var c in candidates)
            {
                int d = c.Order - t.Order;
                int score = d >= 0 ? d : 1000000 - d;   // prefer the nearest declaring type after the nested one
                if (score < bestDistance) { bestDistance = score; best = c; }
            }
            if (best == null) return;
            ResolveDeclaring(best, guard + 1);
            t.DeclaringType = best;
            if (t.Namespace.Length == 0) t.Namespace = best.Namespace;
        }

        void ResolveBases(DumpType t)
        {
            bool first = true;
            foreach (var b in t.Bases)
            {
                var resolved = ResolveTypeName(b, t);
                bool isFirstClassBase = first && t.Kind == "class" && resolved != null && resolved.Kind == "class";
                if (isFirstClassBase) t.BaseType = resolved;
                else if (resolved != null) t.Interfaces.Add(resolved);
                first = false;
            }
            if (t.BaseType == null && t.Kind == "class" && t.FullName != "System.Object")
                t.BaseType = FindExact("System", "Object");
        }

        public DumpType ResolveTypeName(string reference, DumpType context)
        {
            var name = StripGenerics(reference.Trim());
            if (name.EndsWith("[]")) return null;
            List<DumpType> list;
            if (!byName.TryGetValue(name, out list))
            {
                // Fully qualified reference such as "System.Object".
                if (!byFull.TryGetValue(name, out list)) return null;
            }
            if (list.Count == 1) return list[0];
            DumpType best = null;
            int bestScore = -1;
            foreach (var c in list)
            {
                int score = 0;
                if (context != null && c.Namespace == context.Namespace) score += 4;
                if (context != null && c.Image == context.Image) score += 2;
                if (c.Namespace.StartsWith("PvZCards")) score += 1;
                if (score > bestScore) { bestScore = score; best = c; }
            }
            return best;
        }

        void BuildMaps()
        {
            byFull = new Dictionary<string, List<DumpType>>(StringComparer.Ordinal);
            byName = new Dictionary<string, List<DumpType>>(StringComparer.Ordinal);
            bySimple = new Dictionary<string, List<DumpType>>(StringComparer.OrdinalIgnoreCase);
            foreach (var t in Types)
            {
                Add(byFull, t.FullName, t);
                Add(byName, t.Name, t);
                Add(bySimple, t.SimpleName, t);
            }
        }

        static void Add(Dictionary<string, List<DumpType>> map, string key, DumpType t)
        {
            List<DumpType> list;
            if (!map.TryGetValue(key, out list)) { list = new List<DumpType>(); map[key] = list; }
            list.Add(t);
        }

        // ---------------------------------------------------------------- queries

        public DumpType FindExact(string ns, string name)
        {
            List<DumpType> list;
            var full = ns.Length > 0 ? ns + "." + name : name;
            if (!byFull.TryGetValue(full, out list)) return null;
            return list[0];
        }

        // BNM-style lookup: name may carry an arity suffix ("Dictionary`2").
        public DumpType FindBnm(string ns, string name)
        {
            int tick = name.IndexOf('`');
            int arity = -1;
            if (tick > 0)
            {
                int.TryParse(name.Substring(tick + 1), out arity);
                name = name.Substring(0, tick);
            }
            List<DumpType> list;
            var full = ns.Length > 0 ? ns + "." + name : name;
            if (!byFull.TryGetValue(full, out list)) return null;
            foreach (var t in list)
                if (arity < 0 || t.GenericArity == arity) return t;
            return list[0];
        }

        public List<DumpType> FindByName(string name)
        {
            List<DumpType> list;
            if (byFull.TryGetValue(name, out list)) return new List<DumpType>(list);
            if (byName.TryGetValue(name, out list)) return new List<DumpType>(list);
            if (bySimple.TryGetValue(name, out list)) return new List<DumpType>(list);
            return new List<DumpType>();
        }

        public List<DumpType> DerivedFrom(DumpType baseType)
        {
            var result = new List<DumpType>();
            foreach (var t in Types)
            {
                if (t == baseType) continue;
                if (t.BaseType == baseType || t.Interfaces.Contains(baseType)) result.Add(t);
            }
            return result;
        }

        public DumpMethod MethodAtRva(long rva, out long start)
        {
            start = -1;
            if (methodsByRva == null || methodsByRva.Count == 0) return null;
            int lo = 0, hi = methodsByRva.Count - 1, found = -1;
            while (lo <= hi)
            {
                int mid = (lo + hi) / 2;
                if (methodsByRva[mid].Rva <= rva) { found = mid; lo = mid + 1; }
                else hi = mid - 1;
            }
            if (found < 0) return null;
            start = methodsByRva[found].Rva;
            return methodsByRva[found];
        }

        public GenericInstance GenericAtRva(long rva)
        {
            GenericInstance best = null;
            foreach (var g in GenericInstances)
            {
                if (g.Rva > rva) break;
                best = g;
            }
            return best;
        }

        public long NextMethodRva(long rva)
        {
            foreach (var m in methodsByRva) if (m.Rva > rva) return m.Rva;
            return -1;
        }

        public IEnumerable<string> AllTypeNames()
        {
            foreach (var t in Types) yield return t.FullName;
        }

        // ---------------------------------------------------------------- layout and C++ mapping

        static readonly Dictionary<string, int> PrimitiveSizes = new Dictionary<string, int> {
            { "bool", 1 }, { "byte", 1 }, { "sbyte", 1 }, { "char", 2 }, { "short", 2 }, { "ushort", 2 },
            { "int", 4 }, { "uint", 4 }, { "float", 4 }, { "long", 8 }, { "ulong", 8 }, { "double", 8 },
            { "IntPtr", 8 }, { "UIntPtr", 8 }, { "nint", 8 }, { "nuint", 8 }
        };

        public bool IsEnum(string typeName, DumpType context)
        {
            var t = ResolveTypeName(typeName, context);
            return t != null && t.Kind == "enum";
        }

        // Size of a field of this C# type; -1 when unknown (open generic parameter).
        public int FieldSize(string typeName, DumpType context, int guard)
        {
            if (typeName.EndsWith("*") || typeName.EndsWith("[]")) return 8;
            int size;
            if (PrimitiveSizes.TryGetValue(typeName, out size)) return size;
            if (typeName == "string" || typeName == "object") return 8;
            var t = ResolveTypeName(typeName, context);
            if (t == null)
            {
                if (context != null && IsGenericParameter(typeName, context)) return -1;
                return 8;
            }
            if (t.Kind == "enum")
            {
                foreach (var f in t.Fields)
                    if (f.Name == "value__") return FieldSize(f.Type, t, guard + 1);
                return 4;
            }
            if (t.Kind == "struct") return guard > 8 ? -1 : StructSize(t, guard + 1);
            return 8;
        }

        static bool IsGenericParameter(string typeName, DumpType context)
        {
            for (var t = context; t != null; t = t.DeclaringType)
                foreach (var g in SplitTopLevel(t.GenericParams, ','))
                    if (g == typeName) return true;
            return false;
        }

        public int StructSize(DumpType t, int guard)
        {
            int end = 0, align = 1;
            foreach (var f in t.Fields)
            {
                if (f.IsStatic || f.IsConst || f.Offset < 0) continue;
                int size = FieldSize(f.Type, t, guard);
                if (size < 0) return -1;
                end = Math.Max(end, f.Offset + size);
                align = Math.Max(align, Math.Min(size, 8));
            }
            if (end == 0) return 1;
            return (end + align - 1) / align * align;
        }

        // Instance size of a reference type (fields start after the 0x10 object header).
        public int InstanceSize(DumpType t, out bool approximate)
        {
            approximate = false;
            if (t.Kind == "struct") { int s = StructSize(t, 0); approximate = s < 0; return s; }
            int end = 0x10;
            for (var c = t; c != null; c = c.BaseType)
            {
                if (c.OffsetsUnknown) { approximate = true; continue; }
                foreach (var f in c.Fields)
                {
                    if (f.IsStatic || f.IsConst || f.Offset < 0) continue;
                    int size = FieldSize(f.Type, c, 0);
                    if (size < 0) { approximate = true; size = 8; }
                    end = Math.Max(end, f.Offset + size);
                }
            }
            return (end + 7) / 8 * 8;
        }

        public List<DumpField> InstanceFields(DumpType t)
        {
            var chain = new List<DumpType>();
            for (var c = t; c != null; c = c.BaseType) chain.Insert(0, c);
            var result = new List<DumpField>();
            foreach (var c in chain)
                foreach (var f in c.Fields)
                    if (!f.IsStatic && !f.IsConst) result.Add(f);
            return result;
        }

        public List<KeyValuePair<string, string>> EnumMembers(DumpType t)
        {
            var result = new List<KeyValuePair<string, string>>();
            foreach (var f in t.Fields) if (f.IsConst) result.Add(new KeyValuePair<string, string>(f.Name, f.ConstValue));
            return result;
        }

        static readonly Dictionary<string, string> CppPrimitives = new Dictionary<string, string> {
            { "void", "void" }, { "bool", "bool" }, { "byte", "uint8_t" }, { "sbyte", "int8_t" },
            { "char", "char16_t" }, { "short", "int16_t" }, { "ushort", "uint16_t" }, { "int", "int32_t" },
            { "uint", "uint32_t" }, { "long", "int64_t" }, { "ulong", "uint64_t" }, { "float", "float" },
            { "double", "double" }, { "IntPtr", "intptr_t" }, { "UIntPtr", "uintptr_t" }
        };

        // C++ type for a C# type in a hook signature; notes collects warnings for the reader.
        public string CppType(string csType, string modifier, DumpType context, List<string> notes)
        {
            string inner = CppValueType(csType, context, notes);
            if (modifier.Contains("out") || modifier.Contains("ref") || modifier == "in")
                return inner + " *";
            return inner;
        }

        string CppValueType(string csType, DumpType context, List<string> notes)
        {
            string mapped;
            if (CppPrimitives.TryGetValue(csType, out mapped)) return mapped;
            if (csType == "string") return "BNM::Structures::Mono::String *";
            if (csType == "object") return "void *";
            if (csType.EndsWith("[]"))
            {
                var element = csType.Substring(0, csType.Length - 2);
                return "BNM::Structures::Mono::Array<" + ElementType(element, context, notes) + "> *";
            }
            string name, generics;
            SplitGenericName(csType, out name, out generics);
            var args = SplitTopLevel(generics, ',');
            if (name == "List" && args.Count == 1)
                return "BNM::Structures::Mono::List<" + ElementType(args[0], context, notes) + "> *";
            if (name == "Dictionary" && args.Count == 2)
                return "BNM::Structures::Mono::Dictionary<" + ElementType(args[0], context, notes) + ", " + ElementType(args[1], context, notes) + "> *";
            if (context != null && IsGenericParameter(csType, context))
            {
                AddNote(notes, csType + " is a generic parameter: shared generic code passes it as a pointer for reference types; hook the instantiation your target calls");
                return "void * /* " + csType + " */";
            }
            var t = ResolveTypeName(csType, context);
            if (t != null && t.Kind == "enum")
            {
                string underlying = "int32_t";
                foreach (var f in t.Fields) if (f.Name == "value__") { CppPrimitives.TryGetValue(f.Type, out underlying); break; }
                return underlying + " /* " + t.Name + " */";
            }
            if (t != null && t.Kind == "struct")
            {
                AddNote(notes, csType + " is a value type: define a matching C++ struct (" + StructSize(t, 0) + " bytes) and pass it by value");
                return csType + " /* struct */";
            }
            return "void * /* " + csType + " */";
        }

        string ElementType(string csType, DumpType context, List<string> notes)
        {
            var t = CppValueType(csType, context, notes);
            int comment = t.IndexOf(" /*");
            return comment > 0 ? t.Substring(0, comment) : t;
        }

        static void AddNote(List<string> notes, string note)
        {
            if (notes != null && !notes.Contains(note)) notes.Add(note);
        }

        public string HookSignature(DumpMethod m, List<string> notes)
        {
            var sb = new StringBuilder();
            sb.Append(StripComment(CppType(m.ReturnType, "", m.Owner, notes))).Append(" (*)(");
            bool first = true;
            if (!m.IsStatic) { sb.Append("void *self"); first = false; }
            foreach (var p in m.Params)
            {
                if (!first) sb.Append(", ");
                first = false;
                sb.Append(CppType(p.Type, p.Modifier, m.Owner, notes));
                var ptype = sb.ToString();
                if (!ptype.EndsWith("*")) sb.Append(' ');
                sb.Append(SafeIdent(p.Name));
            }
            if (!first) sb.Append(", ");
            sb.Append("const void *method)");
            if (m.GenericParams.Length > 0)
                AddNote(notes, "generic method: each instantiation has its own code; hook the instantiation the game calls (see --rva and GenericInstMethod RVAs)");
            return sb.ToString();
        }

        static string StripComment(string t)
        {
            int c = t.IndexOf(" /*");
            return c > 0 ? t.Substring(0, c) : t;
        }

        static string SafeIdent(string name)
        {
            if (name.Length == 0) return "arg";
            var sb = new StringBuilder();
            foreach (char c in name) sb.Append(IsIdentChar(c) && c != '`' ? c : '_');
            var s = sb.ToString();
            if (s == "method" || s == "self") s = s + "_";
            return s;
        }

        // ---------------------------------------------------------------- suggestions

        public static int EditDistance(string a, string b)
        {
            a = a.ToLowerInvariant();
            b = b.ToLowerInvariant();
            var prev = new int[b.Length + 1];
            var cur = new int[b.Length + 1];
            for (int j = 0; j <= b.Length; j++) prev[j] = j;
            for (int i = 1; i <= a.Length; i++)
            {
                cur[0] = i;
                for (int j = 1; j <= b.Length; j++)
                {
                    int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                    cur[j] = Math.Min(Math.Min(cur[j - 1] + 1, prev[j] + 1), prev[j - 1] + cost);
                }
                var tmp = prev; prev = cur; cur = tmp;
            }
            return prev[b.Length];
        }

        // Like Rank, but never empty: falls back to the nearest names by edit distance.
        public static List<string> Closest(string query, IEnumerable<KeyValuePair<string, string>> candidates, int limit)
        {
            var list = new List<KeyValuePair<string, string>>(candidates);
            var ranked = Rank(query, list, limit);
            if (ranked.Count > 0) return ranked;
            var scored = new List<KeyValuePair<int, string>>();
            var seen = new HashSet<string>();
            foreach (var c in list)
            {
                if (!seen.Add(c.Value)) continue;
                int d = EditDistance(c.Key, query);
                if (!c.Value.StartsWith("PvZCards")) d += 1;
                scored.Add(new KeyValuePair<int, string>(d, c.Value));
            }
            scored.Sort((x, y) => x.Key != y.Key ? x.Key.CompareTo(y.Key) : string.CompareOrdinal(x.Value, y.Value));
            var result = new List<string>();
            foreach (var s in scored) { if (result.Count >= limit) break; result.Add(s.Value); }
            return result;
        }

        // Rank: exact (case-insensitive) > prefix > substring > edit distance <= max(2, len/4).
        public static List<string> Rank(string query, IEnumerable<KeyValuePair<string, string>> candidates, int limit)
        {
            // candidates: key = simple name used for matching, value = text to print
            var scored = new List<KeyValuePair<int, string>>();
            var seen = new HashSet<string>();
            var q = query.ToLowerInvariant();
            int maxDistance = Math.Max(2, query.Length / 4);
            foreach (var c in candidates)
            {
                var simple = c.Key.ToLowerInvariant();
                int score;
                if (simple == q) score = 0;
                else if (simple.StartsWith(q)) score = 1000 + simple.Length;
                else if (simple.Contains(q)) score = 2000 + simple.Length;
                else
                {
                    if (Math.Abs(simple.Length - q.Length) > maxDistance) continue;
                    int d = EditDistance(simple, q);
                    if (d > maxDistance) continue;
                    score = 3000 + d * 100 + simple.Length;
                }
                if (!c.Value.StartsWith("PvZCards")) score += 50;
                if (seen.Add(c.Value)) scored.Add(new KeyValuePair<int, string>(score, c.Value));
            }
            scored.Sort((x, y) => x.Key != y.Key ? x.Key.CompareTo(y.Key) : string.CompareOrdinal(x.Value, y.Value));
            var result = new List<string>();
            foreach (var s in scored) { if (result.Count >= limit) break; result.Add(s.Value); }
            return result;
        }

        // ---------------------------------------------------------------- cache

        public static string HashFile(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
            {
                var bytes = sha.ComputeHash(stream);
                var sb = new StringBuilder();
                foreach (var b in bytes) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        public static DumpIndex LoadOrBuild(string dumpPath, string cachePath)
        {
            var hash = HashFile(dumpPath);
            try
            {
                if (File.Exists(cachePath))
                {
                    var cached = Load(cachePath, hash);
                    if (cached != null) return cached;
                }
            }
            catch (Exception) { }
            var index = Parse(dumpPath);
            try { index.Save(cachePath); } catch (Exception) { }
            return index;
        }

        public void Save(string path)
        {
            var tmp = path + ".tmp";
            using (var w = new BinaryWriter(File.Create(tmp), Encoding.UTF8))
            {
                w.Write("sprig-index");
                w.Write(FormatVersion);
                w.Write(SourceHash);
                w.Write(Images.Count);
                foreach (var img in Images) { w.Write(img.Key); w.Write(img.Value); }
                w.Write(UnparsedTypes); w.Write(FirstUnparsedTypeLine); w.Write(UnparsedTypeText);
                w.Write(Types.Count);
                foreach (var t in Types)
                {
                    w.Write(t.Namespace); w.Write(t.Name); w.Write(t.GenericParams); w.Write(t.Kind);
                    w.Write(t.Modifiers); w.Write(t.TypeDefIndex); w.Write(t.Line);
                    w.Write(t.Unparsed); w.Write(t.FirstUnparsedLine); w.Write(t.UnparsedText);
                    w.Write(t.Bases.Count); foreach (var b in t.Bases) w.Write(b);
                    w.Write(t.Fields.Count);
                    foreach (var f in t.Fields)
                    {
                        w.Write(f.Name); w.Write(f.Type); w.Write(f.Modifiers); w.Write(f.Offset);
                        w.Write(f.IsStatic); w.Write(f.IsConst); w.Write(f.ConstValue); w.Write(f.Line);
                    }
                    w.Write(t.Methods.Count);
                    foreach (var m in t.Methods)
                    {
                        w.Write(m.Name); w.Write(m.GenericParams); w.Write(m.ReturnType); w.Write(m.Modifiers);
                        w.Write(m.IsStatic); w.Write(m.IsVirtual); w.Write(m.IsAbstract); w.Write(m.IsOverride);
                        w.Write(m.Slot); w.Write(m.Rva); w.Write(m.Line);
                        w.Write(m.Params.Count);
                        foreach (var p in m.Params) { w.Write(p.Modifier); w.Write(p.Type); w.Write(p.Name); }
                    }
                    w.Write(t.Properties.Count);
                    foreach (var p in t.Properties)
                    {
                        w.Write(p.Name); w.Write(p.Type); w.Write(p.Modifiers); w.Write(p.HasGet); w.Write(p.HasSet);
                    }
                }
                w.Write(GenericInstances.Count);
                foreach (var g in GenericInstances)
                {
                    w.Write(g.Rva); w.Write(g.Name);
                    w.Write(g.Definition != null ? g.Definition.Owner.Order : -1);
                    w.Write(g.Definition != null ? g.Definition.Owner.Methods.IndexOf(g.Definition) : -1);
                }
            }
            if (File.Exists(path)) File.Delete(path);
            File.Move(tmp, path);
        }

        static DumpIndex Load(string path, string expectedHash)
        {
            using (var r = new BinaryReader(File.OpenRead(path), Encoding.UTF8))
            {
                if (r.ReadString() != "sprig-index") return null;
                if (r.ReadInt32() != FormatVersion) return null;
                var hash = r.ReadString();
                if (hash != expectedHash) return null;
                var index = new DumpIndex();
                index.SourceHash = hash;
                int images = r.ReadInt32();
                for (int i = 0; i < images; i++) index.Images.Add(new KeyValuePair<string, int>(r.ReadString(), r.ReadInt32()));
                index.UnparsedTypes = r.ReadInt32(); index.FirstUnparsedTypeLine = r.ReadInt32(); index.UnparsedTypeText = r.ReadString();
                int types = r.ReadInt32();
                for (int i = 0; i < types; i++)
                {
                    var t = new DumpType();
                    t.Namespace = r.ReadString(); t.Name = r.ReadString(); t.GenericParams = r.ReadString(); t.Kind = r.ReadString();
                    t.Modifiers = r.ReadString(); t.TypeDefIndex = r.ReadInt32(); t.Line = r.ReadInt32(); t.Order = i;
                    t.Unparsed = r.ReadInt32(); t.FirstUnparsedLine = r.ReadInt32(); t.UnparsedText = r.ReadString();
                    int bases = r.ReadInt32();
                    for (int b = 0; b < bases; b++) t.Bases.Add(r.ReadString());
                    int fields = r.ReadInt32();
                    for (int k = 0; k < fields; k++)
                    {
                        var f = new DumpField();
                        f.Name = r.ReadString(); f.Type = r.ReadString(); f.Modifiers = r.ReadString(); f.Offset = r.ReadInt32();
                        f.IsStatic = r.ReadBoolean(); f.IsConst = r.ReadBoolean(); f.ConstValue = r.ReadString(); f.Line = r.ReadInt32();
                        f.Owner = t;
                        t.Fields.Add(f);
                    }
                    int methods = r.ReadInt32();
                    for (int k = 0; k < methods; k++)
                    {
                        var m = new DumpMethod();
                        m.Name = r.ReadString(); m.GenericParams = r.ReadString(); m.ReturnType = r.ReadString(); m.Modifiers = r.ReadString();
                        m.IsStatic = r.ReadBoolean(); m.IsVirtual = r.ReadBoolean(); m.IsAbstract = r.ReadBoolean(); m.IsOverride = r.ReadBoolean();
                        m.Slot = r.ReadInt32(); m.Rva = r.ReadInt64(); m.Line = r.ReadInt32();
                        int ps = r.ReadInt32();
                        for (int p = 0; p < ps; p++)
                        {
                            var param = new DumpParam();
                            param.Modifier = r.ReadString(); param.Type = r.ReadString(); param.Name = r.ReadString();
                            m.Params.Add(param);
                        }
                        m.Owner = t;
                        t.Methods.Add(m);
                    }
                    int props = r.ReadInt32();
                    for (int k = 0; k < props; k++)
                    {
                        var p = new DumpProperty();
                        p.Name = r.ReadString(); p.Type = r.ReadString(); p.Modifiers = r.ReadString(); p.HasGet = r.ReadBoolean(); p.HasSet = r.ReadBoolean();
                        p.Owner = t;
                        t.Properties.Add(p);
                    }
                    index.Types.Add(t);
                }
                int gis = r.ReadInt32();
                for (int i = 0; i < gis; i++)
                {
                    var g = new GenericInstance();
                    g.Rva = r.ReadInt64(); g.Name = r.ReadString();
                    int owner = r.ReadInt32(), method = r.ReadInt32();
                    if (owner >= 0 && owner < index.Types.Count && method >= 0 && method < index.Types[owner].Methods.Count)
                        g.Definition = index.Types[owner].Methods[method];
                    index.GenericInstances.Add(g);
                }
                // Namespaces of nested types were resolved before saving; rebuild links.
                index.Resolve();
                return index;
            }
        }
    }
}
