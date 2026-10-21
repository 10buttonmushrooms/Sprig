// `sprig lint`: checks Bloom modules against dump.cs. C# 5 only (PowerShell 5.1 Add-Type).
// The text tier runs here; the compiler tier compiles generated static_asserts with the NDK's
// clang (lint.ps1 runs clang and hands the output back to ParseCompilerOutput).
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sprig
{
    // compile_commands.json "command" strings use Windows quoting; clang reads the response file
    // with POSIX quoting (passed as --rsp-quoting=posix), which survives PowerShell 5.1's native
    // argument handling.
    public static class CommandLine
    {
        public static List<string> Split(string command)
        {
            var args = new List<string>();
            var cur = new StringBuilder();
            bool inQuotes = false, any = false;
            int i = 0;
            while (i < command.Length)
            {
                char c = command[i];
                if (c == '\\')
                {
                    int n = 0;
                    while (i < command.Length && command[i] == '\\') { n++; i++; }
                    if (i < command.Length && command[i] == '"')
                    {
                        cur.Append('\\', n / 2);
                        if (n % 2 == 1) { cur.Append('"'); i++; }
                        any = true;
                    }
                    else { cur.Append('\\', n); any = true; }
                    continue;
                }
                if (c == '"') { inQuotes = !inQuotes; any = true; i++; continue; }
                if ((c == ' ' || c == '\t') && !inQuotes)
                {
                    if (any) { args.Add(cur.ToString()); cur.Length = 0; any = false; }
                    i++;
                    continue;
                }
                cur.Append(c);
                any = true;
                i++;
            }
            if (any) args.Add(cur.ToString());
            return args;
        }

        // Compiler arguments for a syntax-only check of `unit`, from the entry that compiles `source`.
        public static List<string> SyntaxOnly(List<string> command, string unit)
        {
            var result = new List<string>();
            for (int i = 1; i < command.Count; i++)
            {
                var a = command[i];
                if (a == "-o" || a == "-MF" || a == "-MT" || a == "-MQ") { i++; continue; }
                if (a == "-c" || a == "-MD" || a == "-MMD") continue;
                if (a.StartsWith("-o") && a.Length > 2) continue;
                if (!a.StartsWith("-") && i == command.Count - 1) continue;   // the source file
                if (!a.StartsWith("-") && (a.EndsWith(".cpp") || a.EndsWith(".cc") || a.EndsWith(".cxx") || a.EndsWith(".C"))) continue;
                result.Add(a);
            }
            result.Add("-fsyntax-only");
            result.Add("-ferror-limit=0");
            result.Add("-fno-color-diagnostics");
            result.Add(unit);
            return result;
        }

        public static string ResponseFile(List<string> args)
        {
            var sb = new StringBuilder();
            foreach (var a in args)
            {
                sb.Append('"');
                foreach (char c in a)
                {
                    if (c == '\\' || c == '"') sb.Append('\\');
                    sb.Append(c);
                }
                sb.Append("\"\n");
            }
            return sb.ToString();
        }
    }

    public enum TokKind { Ident, Number, String, Char, Punct }

    public sealed class Token
    {
        public TokKind Kind;
        public string Text = "";
        public string Value = "";   // unescaped content of string literals
        public int Line;
        public int Col;
    }

    public sealed class Finding
    {
        public string File = "";
        public int Line;
        public int Col;
        public string Severity = "error";
        public string Rule = "";
        public string Message = "";
        public string Expected = "";
        public string See = "";
    }

    public sealed class FunctionInfo
    {
        public string Name = "";
        public List<string> Params = new List<string>();
        public int Open;    // token index of '{'
        public int Close;   // token index of the matching '}'
        public int NameToken;
    }

    // What an expression refers to.
    public sealed class Ref
    {
        public string Kind = "unknown";   // type, method, field, property, object, unknown
        public DumpType Type;
        public string TypeText = "";       // "ns.Name" as written, for messages
        public string Member = "";
        public int Count = -2;             // -2 = not given, -1 = any
        public List<string> ParamNames;
        public int Token;                  // where the expression starts

        public static Ref Unknown() { return new Ref(); }
    }

    public sealed class CompilerCheck
    {
        public int WrapperLine;
        public string File = "";
        public int Line;
        public int Col;
        public string Rule = "";
        public string Severity = "error";
        public string Message = "";
        public string Expected = "";
        public string See = "";
    }

    public sealed class SourceFile
    {
        public string Path = "";
        public string RelPath = "";
        public string ModuleDir = "";
        public string ModuleName = "";
        public string ModuleType = "";
        public string Text = "";
        public List<Token> Tokens = new List<Token>();
        public int[] Match;
        public List<FunctionInfo> Functions = new List<FunctionInfo>();
        public int[] FunctionAt;            // token -> index into Functions, or -1 at namespace/class scope
        public HashSet<string> Globals = new HashSet<string>();
        public Dictionary<int, List<string[]>> Suppressions = new Dictionary<int, List<string[]>>();
        public List<KeyValuePair<int, string>> SuppressionErrors = new List<KeyValuePair<int, string>>();
        public Dictionary<string, long> Constants = new Dictionary<string, long>();
        // Bindings: name -> list of (token index, function index or -1, ref)
        public Dictionary<string, List<Binding>> Bindings = new Dictionary<string, List<Binding>>();
        public List<CompilerCheck> Checks = new List<CompilerCheck>();
        public StringBuilder CheckSource = new StringBuilder();
    }

    public sealed class Binding
    {
        public int Token;
        public int Function;
        public Ref Value;
    }

    public sealed class HookWrapper
    {
        public string Name = "";
        public int ClassParam = -1;
        public int MethodParam = -1;
        public int FnParam = -1;
        public int OrigParam = -1;
        public bool ChecksOriginal;
    }

    public sealed class HookSite
    {
        public SourceFile File;
        public int Token;
        public string Api = "";
        public string FnText = "";
        public string OrigText = "";
        public Ref Target;
        public DumpMethod Method;
        public bool Checked;
    }

    public sealed class Linter
    {
        public DumpIndex Index;     // null when dump.cs is missing
        public string Root = "";
        public List<Finding> Findings = new List<Finding>();
        public List<SourceFile> Files = new List<SourceFile>();
        public int Modules;
        public const string HooksGuide = "docs/agents/hooks.md";
        public const string BnmGuide = "docs/agents/bnm-classes.md";
        public const string DslGuide = "docs/agents/card-dsl.md";
        public const string LintGuide = "docs/agents/lint.md";

        public Linter(DumpIndex index, string root)
        {
            Index = index;
            Root = root;
        }

        // ================================================================ modules

        public void LintModule(string dir)
        {
            Modules++;
            var name = Path.GetFileName(dir.TrimEnd('\\', '/'));
            var rel = Rel(dir);
            var ini = Path.Combine(dir, "bloom.ini");
            bool hasMain = File.Exists(Path.Combine(dir, "main.cpp"));
            bool hasSrc = Directory.Exists(Path.Combine(dir, "src"));
            var sources = new List<string>();
            if (hasMain) sources.Add(Path.Combine(dir, "main.cpp"));
            if (hasSrc)
                foreach (var ext in new[] { "*.cpp", "*.cc", "*.cxx", "*.C" })
                    foreach (var f in Directory.GetFiles(Path.Combine(dir, "src"), ext, SearchOption.AllDirectories))
                        if (!sources.Contains(f)) sources.Add(f);
            if (!File.Exists(ini))
            {
                if (hasMain || hasSrc)
                    Add(rel + "\\", 1, 1, "warning", "module", "Bloom\\" + name + " has C++ sources but no bloom.ini, so the build ignores it",
                        "", "Bloom/README.md");
                return;
            }
            string type;
            int typeCount;
            ReadBloomIni(ini, out type, out typeCount);
            var iniRel = Rel(ini);
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, "^[A-Za-z][A-Za-z0-9_-]*$"))
                Add(iniRel, 1, 1, "error", "module", "module name '" + name + "' is invalid: use letters, digits, '_' and '-', starting with a letter", "", "Bloom/README.md");
            if (typeCount != 1 || (type != "Core" && type != "PnP"))
                Add(iniRel, 1, 1, "error", "module", typeCount == 0 ? "bloom.ini has no [Bloom] Type= line, so the build ignores this module"
                    : (typeCount > 1 ? "bloom.ini sets [Bloom] Type more than once" : "bloom.ini Type must be Core or PnP, not '" + type + "'"),
                    "[Bloom]\nType=PnP", "Bloom/README.md");
            var parsed = new List<SourceFile>();
            bool unreadable = false;
            foreach (var s in sources) { var p = TryLoad(s); if (p != null) parsed.Add(p); else unreadable = true; }
            foreach (var h in new[] { "inc", "include" })
            {
                var hd = Path.Combine(dir, h);
                if (Directory.Exists(hd))
                    foreach (var ext in new[] { "*.h", "*.hpp", "*.hh" })
                        foreach (var f in Directory.GetFiles(hd, ext, SearchOption.AllDirectories)) { var p = TryLoad(f); if (p != null) parsed.Add(p); else unreadable = true; }
            }
            foreach (var p in parsed)
            {
                p.ModuleDir = Path.GetFullPath(dir);
                p.ModuleName = name;
                p.ModuleType = type;
            }
            int registers = 0;
            SourceFile registerFile = null;
            int registerLine = 1;
            foreach (var f in parsed)
                for (int i = 0; i < f.Tokens.Count; i++)
                    if (f.Tokens[i].Text == "BLOOM_REGISTER_PNP" && Next(f, i, "("))
                    {
                        registers++;
                        if (registerFile == null) { registerFile = f; registerLine = f.Tokens[i].Line; }
                    }
            // A file lint could not read may hold the registration, so the count is only checked when all were read.
            if (type == "PnP" && registers != 1 && !(unreadable && registers == 0))
                Add(registers == 0 ? iniRel : registerFile.RelPath, registers == 0 ? 1 : registerLine, 1, "error", "module",
                    registers == 0 ? "PnP module never calls BLOOM_REGISTER_PNP(Init), so nothing runs" : "PnP module calls BLOOM_REGISTER_PNP " + registers + " times; register one Init",
                    "BLOOM_REGISTER_PNP(Init);", "Bloom/README.md");
            if (type == "Core" && registers > 0)
                Add(registerFile.RelPath, registerLine, 1, "error", "module", "Core module calls BLOOM_REGISTER_PNP; only PnP modules auto-initialize (use Type=PnP)",
                    "", "Bloom/README.md");
            foreach (var f in parsed) LintSource(f);
        }

        public void LintFiles(IEnumerable<string> paths)
        {
            Modules++;
            foreach (var p in paths) { var f = TryLoad(p); if (f != null) LintSource(f); }
        }

        SourceFile TryLoad(string path)
        {
            try { return Load(path); }
            catch (Exception e)
            {
                InternalFailure(Rel(path), "sprig lint could not read this file", e);
                return null;
            }
        }

        static void ReadBloomIni(string path, out string type, out int count)
        {
            type = "";
            count = 0;
            string section = "";
            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw;
                if (line.Length > 0 && line[0] == '﻿') line = line.Substring(1);
                int c = line.IndexOfAny(new[] { ';', '#' });
                if (c >= 0) line = line.Substring(0, c);
                line = line.Trim();
                if (line.StartsWith("[") && line.EndsWith("]")) { section = line.Substring(1, line.Length - 2).Trim(); continue; }
                if (section != "Bloom") continue;
                int eq = line.IndexOf('=');
                if (eq > 0 && line.Substring(0, eq).Trim() == "Type") { type = line.Substring(eq + 1).Trim(); count++; }
            }
        }

        string Rel(string path)
        {
            var full = Path.GetFullPath(path);
            var root = Path.GetFullPath(Root).TrimEnd('\\') + "\\";
            return full.StartsWith(root, StringComparison.OrdinalIgnoreCase) ? full.Substring(root.Length) : full;
        }

        // ================================================================ tokenizer

        SourceFile Load(string path)
        {
            var f = new SourceFile();
            f.Path = Path.GetFullPath(path);
            f.RelPath = Rel(path);
            f.Text = File.ReadAllText(path);
            Tokenize(f);
            MatchBrackets(f);
            FindFunctions(f);
            Files.Add(f);
            return f;
        }

        static readonly string[] Puncts3 = { "->*", "...", "<<=", ">>=" };
        static readonly string[] Puncts2 = { "::", "->", "++", "--", "<<", "<=", ">=", "==", "!=", "&&", "||", "+=", "-=", "*=", "/=", "%=", "&=", "|=", "^=", ".*" };

        static void Tokenize(SourceFile f)
        {
            var s = f.Text;
            int i = 0, line = 1, lineStart = 0;
            bool atLineStart = true;
            while (i < s.Length)
            {
                char c = s[i];
                if (c == '\n') { line++; i++; lineStart = i; atLineStart = true; continue; }
                if (c == ' ' || c == '\t' || c == '\r' || c == '\f' || c == '\v') { i++; continue; }
                // A suppression comment after code covers its own line; one on a line of its own covers the next line.
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '/')
                {
                    int end = s.IndexOf('\n', i);
                    if (end < 0) end = s.Length;
                    RecordSuppression(f, s.Substring(i + 2, end - i - 2), line, atLineStart ? line + 1 : line);
                    i = end;
                    continue;
                }
                if (c == '/' && i + 1 < s.Length && s[i + 1] == '*')
                {
                    int end = s.IndexOf("*/", i + 2);
                    if (end < 0) end = s.Length - 2;
                    int startLine = line;
                    for (int k = i; k < end; k++) if (s[k] == '\n') { line++; lineStart = k + 1; }
                    int after = Math.Min(s.Length, end + 2);
                    while (after < s.Length && (s[after] == ' ' || s[after] == '\t' || s[after] == '\r')) after++;
                    bool alone = atLineStart && (after >= s.Length || s[after] == '\n');
                    RecordSuppression(f, s.Substring(i + 2, Math.Max(0, end - i - 2)), startLine, alone ? line + 1 : (atLineStart ? line : startLine));
                    i = end + 2;
                    continue;
                }
                if (c == '#' && atLineStart)
                {
                    // Preprocessor line, with backslash continuations.
                    while (i < s.Length && s[i] != '\n')
                    {
                        if (s[i] == '\\' && i + 1 < s.Length && (s[i + 1] == '\n' || (s[i + 1] == '\r' && i + 2 < s.Length && s[i + 2] == '\n')))
                        {
                            i = s.IndexOf('\n', i) + 1;
                            line++;
                            lineStart = i;
                            continue;
                        }
                        if (s[i] == '/' && i + 1 < s.Length && s[i + 1] == '/') break;
                        i++;
                    }
                    continue;
                }
                atLineStart = false;
                var t = new Token();
                t.Line = line;
                t.Col = i - lineStart + 1;
                if (char.IsLetter(c) || c == '_')
                {
                    int start = i;
                    while (i < s.Length && (char.IsLetterOrDigit(s[i]) || s[i] == '_')) i++;
                    var word = s.Substring(start, i - start);
                    if (i < s.Length && (s[i] == '"' || s[i] == '\'') && (word == "u8" || word == "u" || word == "U" || word == "L" || word == "R" || word == "u8R" || word == "LR" || word == "uR" || word == "UR"))
                    {
                        if (word.EndsWith("R") && s[i] == '"')
                        {
                            int paren = s.IndexOf('(', i);
                            var delim = s.Substring(i + 1, paren - i - 1);
                            int close = s.IndexOf(")" + delim + "\"", paren);
                            if (close < 0) close = s.Length - 1;
                            t.Kind = TokKind.String;
                            t.Value = s.Substring(paren + 1, close - paren - 1);
                            for (int k = i; k < close; k++) if (s[k] == '\n') { line++; lineStart = k + 1; }
                            i = close + delim.Length + 2;
                            t.Text = "\"" + t.Value + "\"";
                            f.Tokens.Add(t);
                            continue;
                        }
                        // Prefixed literal: fall through to the quote below with i at the quote.
                        c = s[i];
                    }
                    else
                    {
                        t.Kind = TokKind.Ident;
                        t.Text = word;
                        f.Tokens.Add(t);
                        continue;
                    }
                }
                if (char.IsDigit(c) || (c == '.' && i + 1 < s.Length && char.IsDigit(s[i + 1])))
                {
                    int start = i;
                    while (i < s.Length)
                    {
                        char d = s[i];
                        if (char.IsLetterOrDigit(d) || d == '.' || d == '_') { i++; continue; }
                        if (d == '\'' && i + 1 < s.Length && char.IsLetterOrDigit(s[i + 1])) { i++; continue; }
                        if ((d == '+' || d == '-') && i > start && "eEpP".IndexOf(s[i - 1]) >= 0 && !(s[start] == '0' && start + 1 < s.Length && (s[start + 1] == 'x' || s[start + 1] == 'X') && "eE".IndexOf(s[i - 1]) >= 0)) { i++; continue; }
                        break;
                    }
                    t.Kind = TokKind.Number;
                    t.Text = s.Substring(start, i - start);
                    f.Tokens.Add(t);
                    continue;
                }
                if (c == '"' || c == '\'')
                {
                    char q = c;
                    int start = i;
                    i++;
                    var value = new StringBuilder();
                    while (i < s.Length && s[i] != q && s[i] != '\n')
                    {
                        if (s[i] == '\\' && i + 1 < s.Length)
                        {
                            char e = s[i + 1];
                            value.Append(e == 'n' ? '\n' : e == 't' ? '\t' : e == '0' ? '\0' : e);
                            i += 2;
                            continue;
                        }
                        value.Append(s[i]);
                        i++;
                    }
                    i++;
                    t.Kind = q == '"' ? TokKind.String : TokKind.Char;
                    t.Text = s.Substring(start, Math.Min(i, s.Length) - start);
                    t.Value = value.ToString();
                    f.Tokens.Add(t);
                    continue;
                }
                t.Kind = TokKind.Punct;
                string p = null;
                foreach (var op in Puncts3) if (string.CompareOrdinal(s, i, op, 0, 3) == 0) { p = op; break; }
                if (p == null) foreach (var op in Puncts2) if (string.CompareOrdinal(s, i, op, 0, 2) == 0) { p = op; break; }
                if (p == null) p = c.ToString();
                t.Text = p;
                i += p.Length;
                f.Tokens.Add(t);
            }
        }

        static void RecordSuppression(SourceFile f, string comment, int line, int target)
        {
            var text = comment.Trim();
            if (!text.StartsWith("sprig-lint:")) return;
            var rest = text.Substring(11).Trim();
            if (!rest.StartsWith("allow")) { f.SuppressionErrors.Add(new KeyValuePair<int, string>(line, "unknown sprig-lint directive; use // sprig-lint: allow <rule> <reason>")); return; }
            var parts = rest.Substring(5).Trim().Split(new[] { ' ', '\t' }, 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || parts[1].Trim().Length == 0)
            {
                f.SuppressionErrors.Add(new KeyValuePair<int, string>(line, "a sprig-lint suppression needs a rule and a reason: // sprig-lint: allow <rule> <reason>"));
                if (parts.Length == 0) return;
            }
            List<string[]> list;
            if (!f.Suppressions.TryGetValue(target, out list)) { list = new List<string[]>(); f.Suppressions[target] = list; }
            list.Add(new[] { parts[0], parts.Length > 1 ? parts[1] : "" });
        }

        static void MatchBrackets(SourceFile f)
        {
            f.Match = new int[f.Tokens.Count];
            for (int i = 0; i < f.Match.Length; i++) f.Match[i] = -1;
            var stack = new Stack<int>();
            for (int i = 0; i < f.Tokens.Count; i++)
            {
                var t = f.Tokens[i];
                if (t.Kind != TokKind.Punct) continue;
                if (t.Text == "(" || t.Text == "[" || t.Text == "{") stack.Push(i);
                else if (t.Text == ")" || t.Text == "]" || t.Text == "}")
                {
                    // Tolerate mismatches: pop to the nearest opener of the right kind.
                    string open = t.Text == ")" ? "(" : t.Text == "]" ? "[" : "{";
                    while (stack.Count > 0 && f.Tokens[stack.Peek()].Text != open) stack.Pop();
                    if (stack.Count > 0)
                    {
                        int o = stack.Pop();
                        f.Match[o] = i;
                        f.Match[i] = o;
                    }
                }
            }
        }

        static readonly HashSet<string> NotFunctionNames = new HashSet<string> {
            "if", "for", "while", "switch", "catch", "return", "sizeof", "decltype", "alignof", "noexcept", "static_assert", "defined"
        };

        static readonly HashSet<string> TrailingQualifiers = new HashSet<string> {
            "const", "noexcept", "override", "final", "mutable", "volatile", "&", "&&", "->", "::", "<", ">", "*", ",", "throw"
        };

        // Function bodies at namespace and class scope. Everything inside a body belongs to it.
        static void FindFunctions(SourceFile f)
        {
            f.FunctionAt = new int[f.Tokens.Count];
            for (int i = 0; i < f.FunctionAt.Length; i++) f.FunctionAt[i] = -1;
            var toks = f.Tokens;
            int i2 = 0;
            ScanScope(f, 0, toks.Count, ref i2);
        }

        static void ScanScope(SourceFile f, int from, int to, ref int unused)
        {
            var toks = f.Tokens;
            int i = from;
            while (i < to)
            {
                var t = toks[i];
                if (t.Kind == TokKind.Punct && t.Text == "{" && f.Match[i] > i)
                {
                    int close = f.Match[i];
                    string kind = ClassifyBrace(f, i);
                    if (kind == "function")
                    {
                        var fn = MakeFunction(f, i, close);
                        int index = f.Functions.Count;
                        f.Functions.Add(fn);
                        for (int k = i; k <= close; k++) f.FunctionAt[k] = index;
                        i = close + 1;
                        continue;
                    }
                    if (kind == "scope")
                    {
                        ScanScope(f, i + 1, close, ref unused);
                        i = close + 1;
                        continue;
                    }
                    // Brace initializer or enum body at namespace scope: skip it.
                    i = close + 1;
                    continue;
                }
                if (t.Kind == TokKind.Ident && i + 1 < to)
                {
                    // Namespace-scope variable declarations: the identifier before '=', ';', '[', '{' or a '(' holding literals.
                    int prev = i - 1;
                    var next = toks[i + 1];
                    bool declLike = next.Text == "=" || next.Text == ";" || next.Text == "[" || next.Text == "{" ||
                                    (next.Text == "," && prev >= 0 && toks[prev].Kind == TokKind.Ident);
                    if (declLike && prev >= 0 && (toks[prev].Kind == TokKind.Ident || toks[prev].Text == "*" || toks[prev].Text == "&" || toks[prev].Text == ">") &&
                        !IsKeyword(t.Text) && toks[prev].Text != "return" && toks[prev].Text != "using" && toks[prev].Text != "namespace" && toks[prev].Text != "typedef")
                        f.Globals.Add(t.Text);
                }
                i++;
            }
        }

        static bool IsKeyword(string s)
        {
            switch (s)
            {
                case "const": case "static": case "constexpr": case "inline": case "extern": case "volatile": case "auto":
                case "unsigned": case "signed": case "int": case "long": case "short": case "char": case "bool": case "float":
                case "double": case "void": case "struct": case "class": case "enum": case "union": case "namespace":
                case "public": case "private": case "protected": case "template": case "typename": case "operator":
                    return true;
            }
            return false;
        }

        static string ClassifyBrace(SourceFile f, int brace)
        {
            var toks = f.Tokens;
            // Look back to the start of this declaration.
            int j = brace - 1;
            if (j < 0) return "init";
            // namespace X {  /  namespace {  /  extern "C" {
            int k = j;
            while (k >= 0 && (toks[k].Kind == TokKind.Ident || toks[k].Text == "::") && toks[k].Text != "namespace") k--;
            if (k >= 0 && toks[k].Text == "namespace") return "scope";
            if (j >= 0 && toks[j].Text == "namespace") return "scope";
            if (j >= 1 && toks[j].Kind == TokKind.String && toks[j - 1].Text == "extern") return "scope";
            // struct/class/union X ... {  (enum bodies are skipped)
            int depth = 0;
            for (int m = j; m >= 0 && m > j - 64; m--)
            {
                var tx = toks[m].Text;
                if (tx == ")" || tx == "]") depth++;
                else if (tx == "(" || tx == "[") depth--;
                if (depth != 0) continue;
                if (tx == ";" || tx == "}" || tx == "{" || tx == "=") break;
                if (tx == "enum") return "init";
                if (tx == "struct" || tx == "class" || tx == "union")
                {
                    // "struct X {" but not "struct X x = {" (handled by '=' break above).
                    return "scope";
                }
            }
            // ) qualifiers {  -> function body
            int q = j;
            while (q >= 0 && toks[q].Text != ")" && (toks[q].Kind == TokKind.Ident || TrailingQualifiers.Contains(toks[q].Text))) q--;
            if (q >= 0 && toks[q].Text == ")" && f.Match[q] >= 0)
            {
                int open = f.Match[q];
                if (open > 0)
                {
                    var before = toks[open - 1];
                    if (before.Text == "]") return "init";   // lambda at namespace scope
                    if (before.Kind == TokKind.Ident && !NotFunctionNames.Contains(before.Text)) return "function";
                    if (before.Text == ">" || before.Text == "~" || before.Text == "operator") return "function";
                    if (before.Text == ")" && open > 1) return "function";   // operator()()
                }
            }
            // Constructor initializer lists:  X::X(...) : a(1), b{2} {
            return "init";
        }

        static FunctionInfo MakeFunction(SourceFile f, int open, int close)
        {
            var toks = f.Tokens;
            var fn = new FunctionInfo();
            fn.Open = open;
            fn.Close = close;
            int q = open - 1;
            while (q >= 0 && toks[q].Text != ")") q--;
            if (q < 0 || f.Match[q] < 0) return fn;
            int po = f.Match[q];
            fn.NameToken = po - 1;
            if (po - 1 >= 0) fn.Name = toks[po - 1].Text;
            // Parameter names: last identifier of each top-level comma-separated part, before any '='.
            int start = po + 1, depth = 0;
            for (int i = po + 1; i <= q; i++)
            {
                var tx = toks[i].Text;
                if (i < q && (tx == "(" || tx == "[" || tx == "{" || tx == "<")) { depth++; continue; }
                if (i < q && (tx == ")" || tx == "]" || tx == "}" || tx == ">")) { depth--; continue; }
                if ((tx == "," && depth == 0) || i == q)
                {
                    string name = "";
                    for (int k = start; k < i; k++)
                    {
                        if (toks[k].Text == "=") break;
                        if (toks[k].Kind == TokKind.Ident && !IsKeyword(toks[k].Text)) name = toks[k].Text;
                    }
                    if (i > start) fn.Params.Add(name);
                    start = i + 1;
                }
            }
            return fn;
        }

        static bool Next(SourceFile f, int i, string text)
        {
            return i + 1 < f.Tokens.Count && f.Tokens[i + 1].Text == text;
        }

        static string T(SourceFile f, int i)
        {
            return i >= 0 && i < f.Tokens.Count ? f.Tokens[i].Text : "";
        }

        static string Span(SourceFile f, int from, int to)
        {
            var sb = new StringBuilder();
            for (int i = from; i <= to && i < f.Tokens.Count; i++)
            {
                var t = f.Tokens[i];
                if (sb.Length > 0 && (t.Kind == TokKind.Ident || t.Kind == TokKind.Number) && i > from &&
                    (f.Tokens[i - 1].Kind == TokKind.Ident || f.Tokens[i - 1].Kind == TokKind.Number)) sb.Append(' ');
                sb.Append(t.Text);
            }
            return sb.ToString();
        }

        // ================================================================ expressions

        // Top-level comma-separated arguments of the call whose '(' is at `open`: list of [start, end] token ranges.
        static List<int[]> Args(SourceFile f, int open)
        {
            var result = new List<int[]>();
            int close = f.Match[open];
            if (close < 0) return result;
            int start = open + 1;
            int angle = 0;
            for (int i = open + 1; i <= close; i++)
            {
                var t = f.Tokens[i];
                if (i < close && (t.Text == "(" || t.Text == "[" || t.Text == "{") && f.Match[i] > i) { i = f.Match[i]; continue; }
                if (t.Text == "<" && i > 0 && f.Tokens[i - 1].Kind == TokKind.Ident && LooksTemplate(f.Tokens[i - 1].Text)) angle++;
                else if (t.Text == ">" && angle > 0) angle--;
                if ((t.Text == "," && angle == 0) || i == close)
                {
                    if (i > start) result.Add(new[] { start, i - 1 });
                    start = i + 1;
                }
            }
            return result;
        }

        static bool LooksTemplate(string ident)
        {
            switch (ident)
            {
                case "static_cast": case "reinterpret_cast": case "const_cast": case "dynamic_cast": case "Method": case "Field":
                case "Property": case "List": case "Array": case "Dictionary": case "NewList": case "vector": case "pair": case "tuple":
                case "numeric_limits": case "is_same": case "decltype": case "Read": case "GetField": case "SetField": case "function":
                    return true;
            }
            return ident.Length > 0 && char.IsUpper(ident[0]);
        }

        static string StringArg(SourceFile f, int[] range)
        {
            if (range == null) return null;
            // "x"  or  BNM_OBFUSCATE("x")  or  OBFUSCATE("x")
            for (int i = range[0]; i <= range[1]; i++)
                if (f.Tokens[i].Kind == TokKind.String) return f.Tokens[i].Value;
            return null;
        }

        bool TryConstant(SourceFile f, int[] range, out long value)
        {
            value = 0;
            if (range == null) return false;
            var text = Span(f, range[0], range[1]).Replace(" ", "");
            if (text.StartsWith("static_cast<") && text.EndsWith(")"))
            {
                int p = text.IndexOf(">(");
                if (p > 0) text = text.Substring(p + 2, text.Length - p - 3);
            }
            return ParseIntLiteral(text, f, out value);
        }

        static bool ParseIntLiteral(string text, SourceFile f, out long value)
        {
            value = 0;
            if (text.Length == 0) return false;
            bool negative = false;
            if (text.StartsWith("-")) { negative = true; text = text.Substring(1); }
            else if (text.StartsWith("+")) text = text.Substring(1);
            if (text.StartsWith("(") && text.EndsWith(")")) text = text.Substring(1, text.Length - 2);
            text = text.Replace("'", "");
            while (text.Length > 0 && "uUlL".IndexOf(text[text.Length - 1]) >= 0) text = text.Substring(0, text.Length - 1);
            long v;
            if (text.StartsWith("0x") || text.StartsWith("0X"))
            {
                if (!long.TryParse(text.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) return false;
            }
            else if (text == "std::numeric_limits<int>::min()" || text == "INT_MIN" || text == "INT32_MIN") v = int.MinValue;
            else if (text == "std::numeric_limits<int>::max()" || text == "INT_MAX" || text == "INT32_MAX") v = int.MaxValue;
            else if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out v))
            {
                if (f != null && f.Constants.TryGetValue(text, out v)) { value = negative ? -v : v; return true; }
                return false;
            }
            value = negative ? -v : v;
            return true;
        }

        // Evaluates the postfix chain starting at token i (BNM::Class(...)..., ident..., Type::Method...).
        Ref Eval(SourceFile f, int i, int limit, out int end)
        {
            end = i;
            var toks = f.Tokens;
            Ref cur = Ref.Unknown();
            cur.Token = i;
            if (i >= toks.Count) return cur;
            // Skip leading casts and parentheses: static_cast<...>(expr), (expr)
            if (T(f, i) == "(" && f.Match[i] > i)
            {
                int inner;
                var r = Eval(f, i + 1, f.Match[i] - 1, out inner);
                end = f.Match[i];
                return ContinueChain(f, r, end + 1, limit, out end);
            }
            if (T(f, i) == "BNM" && T(f, i + 1) == "::" && (T(f, i + 2) == "Class" || T(f, i + 2) == "CompileTimeClassBuilder") && T(f, i + 3) == "(")
            {
                int open = i + 3;
                var args = Args(f, open);
                end = f.Match[open];
                if (args.Count >= 2)
                {
                    var ns = StringArg(f, args[0]);
                    var name = StringArg(f, args[1]);
                    if (ns != null && name != null)
                    {
                        cur.Kind = "type";
                        cur.TypeText = (ns.Length > 0 ? ns + "." : "") + name;
                        cur.Member = name;
                        cur.ParamNames = new List<string> { ns, name };
                        cur.Type = Index != null ? Index.FindBnm(ns, name) : null;
                        cur.Count = Index != null && cur.Type == null ? -3 : -2;   // -3: named but missing
                    }
                }
                return ContinueChain(f, cur, end + 1, limit, out end);
            }
            if (toks[i].Kind == TokKind.Ident)
            {
                // Qualified name a::b::c (not BNM::Class handled above).
                int j = i;
                var name = toks[i].Text;
                while (T(f, j + 1) == "::" && j + 2 < toks.Count && toks[j + 2].Kind == TokKind.Ident) { j += 2; name = toks[j].Text; }
                end = j;
                if (j == i)
                {
                    var bound = Lookup(f, name, i);
                    if (bound != null)
                    {
                        cur = Copy(bound);
                        cur.Token = i;
                    }
                }
                return ContinueChain(f, cur, end + 1, limit, out end);
            }
            return cur;
        }

        static Ref Copy(Ref r)
        {
            var c = new Ref();
            c.Kind = r.Kind; c.Type = r.Type; c.TypeText = r.TypeText; c.Member = r.Member; c.Count = r.Count; c.ParamNames = r.ParamNames; c.Token = r.Token;
            return c;
        }

        Ref ContinueChain(SourceFile f, Ref cur, int i, int limit, out int end)
        {
            end = i - 1;
            var toks = f.Tokens;
            while (i + 2 < toks.Count && i <= limit && (T(f, i) == "." || T(f, i) == "->") && toks[i + 1].Kind == TokKind.Ident)
            {
                var member = toks[i + 1].Text;
                int open = i + 2;
                // Template member: .NewList<T>()  -> skip <...>
                if (T(f, open) == "<")
                {
                    int depth = 0, k = open;
                    for (; k < toks.Count; k++)
                    {
                        if (T(f, k) == "<") depth++;
                        else if (T(f, k) == ">") { depth--; if (depth == 0) break; }
                        else if (T(f, k) == ">>") { depth -= 2; if (depth <= 0) break; }
                    }
                    open = k + 1;
                }
                if (T(f, open) != "(" || f.Match[open] < 0)
                {
                    end = i + 1;
                    cur = Ref.Unknown();
                    i = i + 2;
                    continue;
                }
                var args = Args(f, open);
                int close = f.Match[open];
                var next = new Ref();
                next.Token = cur.Token;
                switch (member)
                {
                    case "GetMethod":
                        next.Kind = cur.Kind == "type" ? "method" : "unknown";
                        next.Type = cur.Type;
                        next.TypeText = cur.TypeText;
                        next.Member = args.Count > 0 ? StringArg(f, args[0]) ?? "" : "";
                        if (args.Count < 2) next.Count = -1;
                        else
                        {
                            long n;
                            if (T(f, args[1][0]) == "{")
                            {
                                next.Count = -4;   // parameter names or types
                                next.ParamNames = new List<string>();
                                for (int k = args[1][0]; k <= args[1][1]; k++)
                                    if (toks[k].Kind == TokKind.String) next.ParamNames.Add(toks[k].Value);
                                if (next.ParamNames.Count == 0) next.Count = -5;   // CompileTimeClass list
                            }
                            else if (TryConstant(f, args[1], out n)) next.Count = (int)n;
                            else next.Count = -5;
                        }
                        break;
                    case "GetField":
                    case "GetProperty":
                    case "GetEvent":
                        next.Kind = cur.Kind == "type" ? (member == "GetField" ? "field" : member == "GetProperty" ? "property" : "unknown") : "unknown";
                        next.Type = cur.Type;
                        next.TypeText = cur.TypeText;
                        next.Member = args.Count > 0 ? StringArg(f, args[0]) ?? "" : "";
                        break;
                    case "GetInnerClass":
                        next.Kind = "type";
                        var inner = args.Count > 0 ? StringArg(f, args[0]) ?? "" : "";
                        next.TypeText = cur.TypeText + "." + inner;
                        next.Member = inner;
                        if (cur.Kind == "type" && cur.Type != null && Index != null)
                        {
                            foreach (var t in Index.FindByName(cur.Type.Name + "." + inner))
                                if (t.DeclaringType == cur.Type) { next.Type = t; break; }
                            if (next.Type == null) next.Count = -3;
                        }
                        else next.Kind = cur.Kind == "type" ? "type" : "unknown";
                        break;
                    case "GetGeneric":
                    case "Build":
                        next = Copy(cur);
                        break;
                    case "GetParent":
                        next.Kind = cur.Type != null && cur.Type.BaseType != null ? "type" : "unknown";
                        next.Type = cur.Type != null ? cur.Type.BaseType : null;
                        next.TypeText = next.Type != null ? next.Type.FullName : "";
                        break;
                    case "CreateNewObjectParameters":
                    case "CreateNewObject":
                    case "CreateNewInstance":
                    case "NewList":
                    case "NewListObject":
                    case "NewArray":
                        next.Kind = "object";
                        next.Type = cur.Type;
                        next.TypeText = cur.TypeText;
                        next.Member = member;
                        break;
                    default:
                        next = Ref.Unknown();
                        break;
                }
                // Record the lookup for the names/overload rules.
                if (member == "GetMethod" || member == "GetField" || member == "GetProperty" || member == "GetInnerClass")
                    RecordLookup(f, i + 1, cur, next, member);
                cur = next;
                end = close;
                i = close + 1;
            }
            return cur;
        }

        Binding LookupBinding(SourceFile f, string name, int at)
        {
            List<Binding> list;
            if (!f.Bindings.TryGetValue(name, out list)) return null;
            int fn = at < f.FunctionAt.Length ? f.FunctionAt[at] : -1;
            Binding best = null;
            // Latest binding before `at` in the same function, else the latest global one.
            foreach (var b in list) if (b.Function == fn && fn >= 0 && b.Token < at && (best == null || b.Token > best.Token)) best = b;
            if (best != null) return best;
            foreach (var b in list) if (f.Globals.Contains(name) && (best == null || b.Token > best.Token)) best = b;
            if (best != null) return best;
            foreach (var b in list) if (b.Function == -1 && (best == null || b.Token > best.Token)) best = b;
            return best;
        }

        Ref Lookup(SourceFile f, string name, int at)
        {
            var b = LookupBinding(f, name, at);
            return b != null ? b.Value : null;
        }

        // ================================================================ per-source analysis

        HashSet<string> recordedLookups;
        List<HookWrapper> wrappers;

        void LintSource(SourceFile f)
        {
            foreach (var e in f.SuppressionErrors) Add(f, e.Key, 1, "error", "suppression", e.Value, "", LintGuide + "#suppressing-a-finding");
            Guard(f, "name resolution", () =>
            {
                CollectConstants(f);
                recordedLookups = new HashSet<string>();
                // Two passes: bindings may be assigned in Init and used in other functions.
                CollectBindings(f);
                CollectBindings(f);
                recordedLookups = new HashSet<string>();
                foreach (var key in new List<string>(f.Bindings.Keys))
                    foreach (var b in f.Bindings[key]) { int e; Eval(f, b.Token, f.Tokens.Count - 1, out e); }
            });
            Guard(f, "names", () => RuleNamesSweep(f));
            var hooks = new List<HookSite>();
            Guard(f, "hook discovery", () => { wrappers = FindWrappers(f); hooks = FindHooks(f); });
            Guard(f, "original", () => RuleOriginal(f, hooks));
            Guard(f, "init-phase", () => RuleInitPhase(f));
            Guard(f, "address-literal", () => RuleAddressLiteral(f));
            Guard(f, "write-barrier", () => RuleWriteBarrier(f));
            Guard(f, "exceptions", () => RuleExceptions(f, hooks));
            Guard(f, "managed-global", () => RuleManagedGlobal(f));
            Guard(f, "enum-range", () => RuleEnumRange(f));
            Guard(f, "pool-bypass", () => RulePoolBypass(f));
            Guard(f, "custom-class", () => RuleCustomClass(f));
            if (!Guard(f, "compiler-check generation", () => BuildCompilerChecks(f, hooks)))
            {
                f.Checks.Clear();
                f.CheckSource.Length = 0;
            }
        }

        // A check that crashes on unusual code is reported as a lint-internal warning and skipped,
        // so one lint bug neither hides the other findings nor stops the build.
        bool Guard(SourceFile f, string check, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception e)
            {
                InternalFailure(f.RelPath, "the " + check + " check crashed on this file", e);
                return false;
            }
        }

        public void InternalFailure(string file, string what, Exception e)
        {
            // PowerShell wraps exceptions from .NET calls.
            while (e.InnerException != null && e.GetType().Name.EndsWith("InvocationException")) e = e.InnerException;
            bool io = e is IOException || e is UnauthorizedAccessException;
            Add(file, 1, 1, "warning", "lint-internal",
                what + " (" + e.GetType().Name + ": " + e.Message + "). " +
                (io ? "Close any program that has the file open and run lint again" : "This is a bug in sprig lint, not in your code") + "; the other checks still ran",
                "", LintGuide + "#when-lint-itself-fails");
        }

        void CollectConstants(SourceFile f)
        {
            var toks = f.Tokens;
            for (int i = 0; i + 3 < toks.Count; i++)
            {
                if (toks[i].Text != "constexpr" && toks[i].Text != "const") continue;
                // const[expr] <type words> NAME = <literal> ;
                int j = i + 1;
                while (j < toks.Count && (toks[j].Kind == TokKind.Ident || toks[j].Text == "::") && T(f, j + 1) != "=") j++;
                if (j >= toks.Count || toks[j].Kind != TokKind.Ident || T(f, j + 1) != "=") continue;
                int semi = j + 2;
                while (semi < toks.Count && T(f, semi) != ";" && T(f, semi) != ",") semi++;
                long v;
                if (ParseIntLiteral(Span(f, j + 2, semi - 1).Replace(" ", ""), f, out v)) f.Constants[toks[j].Text] = v;
            }
        }

        void CollectBindings(SourceFile f)
        {
            var toks = f.Tokens;
            for (int i = 1; i + 1 < toks.Count; i++)
            {
                if (toks[i].Text != "=" || toks[i - 1].Kind != TokKind.Ident) continue;
                var name = toks[i - 1].Text;
                // Skip a[x] = ..., obj.f = ..., *p = ...
                var before = T(f, i - 2);
                if (before == "." || before == "->") continue;
                int end;
                var value = Eval(f, i + 1, toks.Count - 1, out end);
                if (value.Kind == "unknown") continue;
                AddBinding(f, name, i - 1, value);
            }
            // Constructor-style declarations:  BNM::Class cls("ns", "name");  auto m(BNM::Class(...).GetMethod(...));
            for (int i = 1; i + 1 < toks.Count; i++)
            {
                if (toks[i].Kind != TokKind.Ident || T(f, i + 1) != "(" || toks[i - 1].Kind != TokKind.Ident) continue;
                if (toks[i - 1].Text != "Class" || T(f, i - 2) != "::" || T(f, i - 3) != "BNM") continue;
                int open = i + 1;
                var args = Args(f, open);
                if (args.Count < 2) continue;
                var ns = StringArg(f, args[0]);
                var nm = StringArg(f, args[1]);
                if (ns == null || nm == null) continue;
                var r = new Ref();
                r.Kind = "type";
                r.TypeText = (ns.Length > 0 ? ns + "." : "") + nm;
                r.Type = Index != null ? Index.FindBnm(ns, nm) : null;
                r.Token = i;
                AddBinding(f, toks[i].Text, i, r);
            }
        }

        void AddBinding(SourceFile f, string name, int token, Ref value)
        {
            List<Binding> list;
            if (!f.Bindings.TryGetValue(name, out list)) { list = new List<Binding>(); f.Bindings[name] = list; }
            foreach (var existing in list) if (existing.Token == token) { existing.Value = value; return; }
            var b = new Binding();
            b.Token = token;
            b.Function = f.FunctionAt[token];
            b.Value = value;
            list.Add(b);
        }

        // ================================================================ names and overload rules

        void RecordLookup(SourceFile f, int memberToken, Ref owner, Ref result, string api)
        {
            if (recordedLookups == null) return;
            var key = f.Path + ":" + memberToken;
            if (!recordedLookups.Add(key)) return;
            pendingLookups.Add(new object[] { f, memberToken, owner, result, api });
        }

        List<object[]> pendingLookups = new List<object[]>();

        void RuleNamesSweep(SourceFile f)
        {
            // Every BNM::Class("ns", "name") in the file, and every lookup recorded while evaluating.
            // Classes the mod creates (BNM_CustomClass targets) are not in dump.cs by design.
            var toks = f.Tokens;
            var created = new HashSet<string>();
            var targetRanges = new List<int[]>();
            for (int i = 0; i + 1 < toks.Count; i++)
            {
                if (T(f, i) != "BNM_CustomClass" || T(f, i + 1) != "(") continue;
                var cargs = Args(f, i + 1);
                if (cargs.Count < 2) continue;
                targetRanges.Add(cargs[1]);
                for (int k = cargs[1][0]; k <= cargs[1][1]; k++)
                {
                    if (T(f, k) != "CompileTimeClassBuilder" || T(f, k + 1) != "(") continue;
                    var targs = Args(f, k + 1);
                    if (targs.Count >= 2 && StringArg(f, targs[0]) != null && StringArg(f, targs[1]) != null)
                        created.Add(StringArg(f, targs[0]) + "|" + StringArg(f, targs[1]));
                    break;
                }
            }
            foreach (var other in Files)
                for (int i = 0; i + 1 < other.Tokens.Count; i++)
                    if (other != f && T(other, i) == "BNM_CustomClass" && T(other, i + 1) == "(")
                    {
                        var cargs = Args(other, i + 1);
                        if (cargs.Count < 2) continue;
                        for (int k = cargs[1][0]; k <= cargs[1][1]; k++)
                            if (T(other, k) == "CompileTimeClassBuilder" && T(other, k + 1) == "(")
                            {
                                var targs = Args(other, k + 1);
                                if (targs.Count >= 2 && StringArg(other, targs[0]) != null && StringArg(other, targs[1]) != null)
                                    created.Add(StringArg(other, targs[0]) + "|" + StringArg(other, targs[1]));
                                break;
                            }
                    }
            for (int i = 0; i + 3 < toks.Count; i++)
            {
                if (T(f, i) != "BNM" || T(f, i + 1) != "::" || (T(f, i + 2) != "Class" && T(f, i + 2) != "CompileTimeClassBuilder") || T(f, i + 3) != "(") continue;
                int e;
                Eval(f, i, toks.Count - 1, out e);
                var args = Args(f, i + 3);
                if (args.Count < 2 || Index == null) continue;
                var ns = StringArg(f, args[0]);
                var name = StringArg(f, args[1]);
                if (ns == null || name == null) continue;
                if (Index.FindBnm(ns, name) != null || created.Contains(ns + "|" + name)) continue;
                bool inTarget = false;
                foreach (var r in targetRanges) if (i >= r[0] && i <= r[1]) inTarget = true;
                if (inTarget) continue;
                var simple = name.Split('`')[0];
                var cands = new List<KeyValuePair<string, string>>();
                foreach (var t in Index.Types) cands.Add(new KeyValuePair<string, string>(t.Name, t.FullName));
                var close = DumpIndex.Closest(simple, cands, 3);
                var exact = Index.FindByName(simple);
                string hint = exact.Count > 0 ? "it exists as " + Join(exact, 3) : (close.Count > 0 ? "closest: " + string.Join(", ", close.ToArray()) : "");
                AddMissing(f, toks[i + 3], null, simple,
                    "class " + (ns.Length > 0 ? ns + "." : "") + name + " is not in dump.cs" + (hint.Length > 0 ? "; " + hint : ""), "");
            }
            foreach (var item in pendingLookups)
            {
                if ((SourceFile)item[0] != f) continue;
                CheckLookup(f, (int)item[1], (Ref)item[2], (Ref)item[3], (string)item[4]);
            }
            pendingLookups.RemoveAll(x => (SourceFile)x[0] == f);
        }

        // A name lint cannot find may still exist when a dump.cs line sprig could not read mentions it,
        // so it warns there instead of stopping the build. owner is null for a missing class.
        void AddMissing(SourceFile f, Token tok, DumpType owner, string name, string message, string expected)
        {
            int line = owner == null ? DumpIndex.MentionLine(Index.UnparsedTypeText, name) : DumpIndex.UnreadMention(owner, name);
            string unread = line > 0 ? "; but sprig could not read dump.cs:" + line + ", which mentions " + name + ", so check that line" : "";
            Add(f, tok.Line, tok.Col, line > 0 ? "warning" : "error", "names", message + unread, expected, "AGENTS.md#look-up-every-name");
        }

        static string Join(List<DumpType> types, int max)
        {
            var names = new List<string>();
            foreach (var t in types) { if (names.Count >= max) break; names.Add(t.FullName); }
            return string.Join(", ", names.ToArray());
        }

        void CheckLookup(SourceFile f, int memberToken, Ref owner, Ref result, string api)
        {
            if (Index == null || owner.Kind != "type" || owner.Type == null) return;
            var tok = f.Tokens[memberToken];
            var type = owner.Type;
            var name = result.Member;
            if (string.IsNullOrEmpty(name)) return;   // the name is a variable, not a literal
            if (api == "GetInnerClass")
            {
                if (result.Type == null)
                {
                    var cands = new List<KeyValuePair<string, string>>();
                    foreach (var t in Index.Types) if (t.DeclaringType == type) cands.Add(new KeyValuePair<string, string>(t.SimpleName, t.SimpleName));
                    AddMissing(f, tok, null, name, type.FullName + " has no inner class " + name + Suggest(name, cands), "");
                }
                return;
            }
            if (api == "GetMethod")
            {
                var overloads = new List<DumpMethod>();
                for (var t = type; t != null; t = t.BaseType) foreach (var m in t.Methods) if (m.Name == name) overloads.Add(m);
                if (overloads.Count == 0)
                {
                    var cands = new List<KeyValuePair<string, string>>();
                    for (var t = type; t != null; t = t.BaseType) foreach (var m in t.Methods) cands.Add(new KeyValuePair<string, string>(m.Name, m.Name));
                    AddMissing(f, tok, type, name, type.FullName + " has no method " + name + Suggest(name, cands), "");
                    return;
                }
                if (result.Count >= 0)
                {
                    // BNM walks from the class up to its bases and returns the first match, so only
                    // the first type with a matching method can be ambiguous.
                    var same = new List<DumpMethod>();
                    for (var t = type; t != null && same.Count == 0; t = t.BaseType)
                        foreach (var m in t.Methods) if (m.Name == name && m.Params.Count == result.Count) same.Add(m);
                    if (same.Count == 0)
                    {
                        var counts = new List<string>();
                        foreach (var m in overloads) if (!counts.Contains(m.Params.Count.ToString())) counts.Add(m.Params.Count.ToString());
                        AddMissing(f, tok, type, name, type.FullName + "." + name + " has no overload with " + result.Count + (result.Count == 1 ? " parameter" : " parameters") +
                            " (it has " + string.Join(", ", counts.ToArray()) + ")", "");
                    }
                    else if (same.Count > 1)
                    {
                        Add(f, tok.Line, tok.Col, "warning", "overload", same.Count + " overloads of " + type.FullName + "." + name + " take " + result.Count + (result.Count == 1 ? " parameter" : " parameters") + "; GetMethod returns the first one",
                            "GetMethod(\"" + name + "\", {" + ParamNameList(same[0]) + "})  (pick the overload with sprig find " + type.Name + "." + name + ")", HooksGuide + "#overloads");
                    }
                }
                else if (result.Count == -1)
                {
                    var first = new List<DumpMethod>();
                    for (var t = type; t != null && first.Count == 0; t = t.BaseType)
                        foreach (var m in t.Methods) if (m.Name == name) first.Add(m);
                    if (first.Count > 1)
                        Add(f, tok.Line, tok.Col, "warning", "overload", "GetMethod(\"" + name + "\") without a parameter count matches " + first.Count + " overloads of " + first[0].Owner.FullName,
                            "GetMethod(\"" + name + "\", " + first[0].Params.Count + ")", HooksGuide + "#overloads");
                }
                else if (result.Count == -4 && result.ParamNames != null)
                {
                    bool match = false;
                    foreach (var m in overloads)
                    {
                        if (m.Params.Count != result.ParamNames.Count) continue;
                        bool ok = true;
                        for (int k = 0; k < m.Params.Count; k++) if (m.Params[k].Name != result.ParamNames[k]) { ok = false; break; }
                        if (ok) { match = true; break; }
                    }
                    if (!match)
                        AddMissing(f, tok, type, name, "no overload of " + type.FullName + "." + name + " has parameters named {" + string.Join(", ", result.ParamNames.ToArray()) + "}",
                            "GetMethod(\"" + name + "\", {" + ParamNameList(overloads[0]) + "})");
                }
                return;
            }
            if (api == "GetField")
            {
                for (var t = type; t != null; t = t.BaseType) foreach (var fl in t.Fields) if (fl.Name == name) return;
                var cands = new List<KeyValuePair<string, string>>();
                for (var t = type; t != null; t = t.BaseType) foreach (var fl in t.Fields) cands.Add(new KeyValuePair<string, string>(fl.Name, fl.Name));
                var hint = "";
                for (var t = type; t != null; t = t.BaseType)
                    foreach (var p in t.Properties)
                        if (p.Name == name) hint = "; " + name + " is a property: its field is <" + name + ">k__BackingField, or use GetProperty";
                AddMissing(f, tok, type, name, type.FullName + " has no field " + name + (hint.Length > 0 ? hint : Suggest(name, cands)), "");
                return;
            }
            if (api == "GetProperty")
            {
                for (var t = type; t != null; t = t.BaseType) foreach (var p in t.Properties) if (p.Name == name) return;
                var cands = new List<KeyValuePair<string, string>>();
                for (var t = type; t != null; t = t.BaseType) foreach (var p in t.Properties) cands.Add(new KeyValuePair<string, string>(p.Name, p.Name));
                AddMissing(f, tok, type, name, type.FullName + " has no property " + name + Suggest(name, cands), "");
            }
        }

        static string ParamNameList(DumpMethod m)
        {
            var names = new List<string>();
            foreach (var p in m.Params) names.Add("\"" + p.Name + "\"");
            return string.Join(", ", names.ToArray());
        }

        static string Suggest(string name, List<KeyValuePair<string, string>> cands)
        {
            var close = DumpIndex.Rank(name, cands, 3);
            return close.Count > 0 ? "; closest: " + string.Join(", ", close.ToArray()) : "";
        }

        // ================================================================ hooks

        static readonly string[] HookApis = { "BasicHook", "InvokeHook", "VirtualHook" };

        List<HookWrapper> FindWrappers(SourceFile f)
        {
            var result = new List<HookWrapper>();
            foreach (var fn in f.Functions)
            {
                for (int i = fn.Open; i < fn.Close; i++)
                {
                    var api = HookApiAt(f, i);
                    if (api == null) continue;
                    int open = i + 1;
                    var args = Args(f, open);
                    int offset = api == "VirtualHook" ? 1 : 0;
                    if (args.Count < 3 + offset) continue;
                    var w = new HookWrapper();
                    w.Name = fn.Name;
                    w.MethodParam = fn.Params.IndexOf(Span(f, args[offset][0], args[offset][1]));
                    w.FnParam = fn.Params.IndexOf(Span(f, args[offset + 1][0], args[offset + 1][1]));
                    w.OrigParam = fn.Params.IndexOf(Span(f, args[offset + 2][0], args[offset + 2][1]));
                    if (offset == 1) w.ClassParam = fn.Params.IndexOf(Span(f, args[0][0], args[0][1]));
                    if (w.MethodParam < 0 || w.FnParam < 0 || w.OrigParam < 0) continue;
                    var origName = fn.Params[w.OrigParam];
                    w.ChecksOriginal = IsChecked(f, origName, open, fn.Close) || CallUsedAsCondition(f, i);
                    bool dup = false;
                    foreach (var x in result) if (x.Name == w.Name) { dup = true; x.ChecksOriginal = x.ChecksOriginal && w.ChecksOriginal; }
                    if (!dup) result.Add(w);
                }
            }
            return result;
        }

        // Returns the API name when token i is BasicHook/InvokeHook/VirtualHook used as a call.
        static string HookApiAt(SourceFile f, int i)
        {
            var t = f.Tokens[i];
            if (t.Kind != TokKind.Ident || T(f, i + 1) != "(") return null;
            foreach (var api in HookApis) if (t.Text == api) return api;
            return null;
        }

        List<HookSite> FindHooks(SourceFile f)
        {
            var sites = new List<HookSite>();
            for (int i = 0; i < f.Tokens.Count; i++)
            {
                int fnIndex = f.FunctionAt[i];
                if (fnIndex < 0) continue;
                var fn = f.Functions[fnIndex];
                var api = HookApiAt(f, i);
                HookWrapper wrapper = null;
                if (api == null)
                {
                    var t = f.Tokens[i];
                    if (t.Kind == TokKind.Ident && T(f, i + 1) == "(" && i != fn.NameToken && T(f, i - 1) != "." && T(f, i - 1) != "->")
                        foreach (var w in wrappers) if (w.Name == t.Text && w.Name != fn.Name) { wrapper = w; break; }
                    if (wrapper == null) continue;
                }
                else
                {
                    // Calls inside a wrapper's own body use its parameters: checked at the wrapper's call sites.
                    bool insideWrapper = false;
                    foreach (var w in wrappers) if (w.Name == fn.Name) insideWrapper = true;
                    if (insideWrapper) continue;
                    // Raw-pointer BasicHook (no BNM::) is a pointer hook: no method to check against.
                    if (api == "BasicHook" && !(T(f, i - 1) == "::" && T(f, i - 2) == "BNM"))
                    {
                        var rargs = Args(f, i + 1);
                        if (rargs.Count >= 1)
                        {
                            var site0 = new HookSite();
                            site0.File = f; site0.Token = i; site0.Api = "pointer";
                            site0.Target = Ref.Unknown();
                            if (rargs.Count >= 3) { site0.FnText = Span(f, rargs[1][0], rargs[1][1]); site0.OrigText = Span(f, rargs[2][0], rargs[2][1]); }
                            site0.Checked = IsChecked(f, site0.OrigText, f.Match[i + 1], fn.Close) || CallUsedAsCondition(f, i);
                            sites.Add(site0);
                        }
                        continue;
                    }
                }
                int open = i + 1;
                var args = Args(f, open);
                var site = new HookSite();
                site.File = f;
                site.Token = i;
                int[] methodArg, fnArg, origArg;
                if (wrapper != null)
                {
                    site.Api = "wrapper " + wrapper.Name;
                    if (args.Count <= Math.Max(wrapper.MethodParam, Math.Max(wrapper.FnParam, wrapper.OrigParam))) continue;
                    methodArg = args[wrapper.MethodParam];
                    fnArg = args[wrapper.FnParam];
                    origArg = args[wrapper.OrigParam];
                    site.Checked = wrapper.ChecksOriginal;
                }
                else
                {
                    site.Api = api;
                    int offset = api == "VirtualHook" ? 1 : 0;
                    if (args.Count < 3 + offset) continue;
                    methodArg = args[offset];
                    fnArg = args[offset + 1];
                    origArg = args[offset + 2];
                }
                site.FnText = Span(f, fnArg[0], fnArg[1]);
                site.OrigText = Span(f, origArg[0], origArg[1]);
                int e;
                site.Target = Eval(f, methodArg[0], methodArg[1], out e);
                if (!site.Checked)
                    site.Checked = IsChecked(f, site.OrigText, f.Match[open], fn.Close) || CallUsedAsCondition(f, i);
                ResolveHookMethod(site);
                sites.Add(site);
            }
            return sites;
        }

        void ResolveHookMethod(HookSite site)
        {
            var f = site.File;
            var tok = f.Tokens[site.Token];
            var r = site.Target;
            if (site.Api == "pointer") return;
            if (r.Kind != "method")
            {
                if (Index != null)
                    Add(f, tok.Line, tok.Col, "warning", "hook-target",
                        "lint cannot trace this hook's target to one method, so its signature is not checked",
                        "pass the method directly: BNM::BasicHook(BNM::Class(\"ns\", \"Type\").GetMethod(\"Name\", n), Hook, original)", HooksGuide + "#checked-hooks");
                return;
            }
            if (Index == null || r.Type == null) return;
            var candidates = new List<DumpMethod>();
            for (var t = r.Type; t != null; t = t.BaseType)
            {
                foreach (var m in t.Methods)
                {
                    if (m.Name != r.Member) continue;
                    if (r.Count >= 0 && m.Params.Count != r.Count) continue;
                    if (r.Count == -4 && r.ParamNames != null)
                    {
                        if (m.Params.Count != r.ParamNames.Count) continue;
                        bool ok = true;
                        for (int k = 0; k < m.Params.Count; k++) if (m.Params[k].Name != r.ParamNames[k]) ok = false;
                        if (!ok) continue;
                    }
                    candidates.Add(m);
                }
                if (candidates.Count > 0) break;
            }
            if (candidates.Count == 1) site.Method = candidates[0];
            else if (candidates.Count > 1)
                Add(f, tok.Line, tok.Col, "warning", "hook-target",
                    "this hook's target matches " + candidates.Count + " overloads of " + r.Type.FullName + "." + r.Member + ", so its signature is not checked",
                    "GetMethod(\"" + r.Member + "\", {" + ParamNameList(candidates[0]) + "})", HooksGuide + "#overloads");
        }

        // True when `name` is tested (null check, comparison, negation) between token `from` and `to`.
        static bool IsChecked(SourceFile f, string name, int from, int to)
        {
            if (name.Length == 0) return false;
            for (int i = Math.Max(0, from); i <= to && i < f.Tokens.Count; i++)
            {
                if (f.Tokens[i].Text != name) continue;
                var prev = T(f, i - 1);
                var next = T(f, i + 1);
                if (prev == "!" || next == "==" || next == "!=" || next == "?" || next == "&&" || next == "||" || prev == "&&" || prev == "||") return true;
                if (prev == "(" && next == ")" && (T(f, i - 2) == "if" || T(f, i - 2) == "while")) return true;
                if (prev == "return" && (next == ";" || next == "!=" || next == "==")) return true;
                if (prev == "(" && T(f, i - 2) == "if") return true;
            }
            return false;
        }

        // InvokeHook/VirtualHook return bool: used in if(...), return, assignment or a logical expression.
        static bool CallUsedAsCondition(SourceFile f, int apiToken)
        {
            int i = apiToken - 1;
            while (i >= 0 && (f.Tokens[i].Text == "::" || f.Tokens[i].Text == "BNM")) i--;
            var prev = T(f, i);
            return prev == "(" && (T(f, i - 1) == "if" || T(f, i - 1) == "while") || prev == "return" || prev == "=" || prev == "!" || prev == "&&" || prev == "||" || prev == "(";
        }

        void RuleOriginal(SourceFile f, List<HookSite> hooks)
        {
            foreach (var site in hooks)
            {
                var tok = f.Tokens[site.Token];
                if (!site.Checked && site.OrigText.Length > 0)
                    Add(f, tok.Line, tok.Col, "error", "original",
                        "the hook's result is never checked: if the target is missing, " + site.OrigText + " stays null and nothing reports it",
                        "if (!" + site.OrigText + ") { LOG_ERR(\"...hook failed\"); return; }", HooksGuide + "#calling-the-original");
            }
            // Calls through an original pointer outside its hook must be guarded.
            var hookOf = new Dictionary<string, HashSet<string>>();
            foreach (var site in hooks)
            {
                if (site.OrigText.Length == 0) continue;
                HashSet<string> set;
                if (!hookOf.TryGetValue(site.OrigText, out set)) { set = new HashSet<string>(); hookOf[site.OrigText] = set; }
                set.Add(StripTemplate(site.FnText));
            }
            for (int i = 0; i + 1 < f.Tokens.Count; i++)
            {
                HashSet<string> fns;
                if (!hookOf.TryGetValue(f.Tokens[i].Text, out fns) || T(f, i + 1) != "(") continue;
                int fnIndex = f.FunctionAt[i];
                if (fnIndex < 0) continue;
                var fn = f.Functions[fnIndex];
                if (fns.Contains(fn.Name)) continue;
                if (IsChecked(f, f.Tokens[i].Text, fn.Open, i)) continue;
                var tok = f.Tokens[i];
                Add(f, tok.Line, tok.Col, "error", "original",
                    tok.Text + " is called outside its hook (" + string.Join(", ", new List<string>(fns).ToArray()) + ") without a null check; it is null when the hook was not installed",
                    "if (" + tok.Text + ") " + tok.Text + "(...);", HooksGuide + "#calling-the-original");
            }
        }

        static string StripTemplate(string fnText)
        {
            int lt = fnText.IndexOf('<');
            var s = lt > 0 ? fnText.Substring(0, lt) : fnText;
            return s.TrimStart('&');
        }

        // ================================================================ other rules

        static readonly HashSet<string> BnmCalls = new HashSet<string> {
            "Class", "CreateMonoString", "CompileTimeClassBuilder", "Method", "Field", "Property", "BasicHook", "InvokeHook", "VirtualHook", "IsA", "TryInvoke"
        };

        void RuleInitPhase(SourceFile f)
        {
            var toks = f.Tokens;
            for (int i = 0; i + 3 < toks.Count; i++)
            {
                if (T(f, i) != "BNM" || T(f, i + 1) != "::") continue;
                var what = T(f, i + 2);
                // CompileTimeClassBuilder only describes a class; BNM resolves it later (BNM_Custom* macros rely on that).
                if (what == "Loading" || what == "CompileTimeClassBuilder" || !BnmCalls.Contains(what)) continue;
                // BNM::Class x("a", "b") and BNM::Class("a","b") are calls; BNM::Class x; is a declaration.
                bool call = T(f, i + 3) == "(" || (toks[i + 3].Kind == TokKind.Ident && T(f, i + 4) == "(" && HasLiteral(f, i + 4));
                if (!call) continue;
                int fn = f.FunctionAt[i];
                if (fn < 0)
                {
                    Add(f, toks[i].Line, toks[i].Col, "error", "init-phase",
                        "BNM::" + what + " runs while the library loads, before IL2CPP is ready; it finds nothing (or crashes)",
                        "look classes and methods up inside your Init function", HooksGuide + "#when-bnm-is-ready");
                    continue;
                }
                if (f.Functions[fn].Name == "JNI_OnLoad")
                    Add(f, toks[i].Line, toks[i].Col, "error", "init-phase",
                        "BNM::" + what + " in JNI_OnLoad runs before IL2CPP is ready; only BNM::Loading calls belong there",
                        "do the lookup in Init (PnP) or OnIl2CppLoaded", HooksGuide + "#when-bnm-is-ready");
            }
        }

        static bool HasLiteral(SourceFile f, int open)
        {
            int close = f.Match[open];
            for (int i = open + 1; i < close; i++) if (f.Tokens[i].Kind == TokKind.String || f.Tokens[i].Kind == TokKind.Number) return true;
            return false;
        }

        void RuleAddressLiteral(SourceFile f)
        {
            var toks = f.Tokens;
            for (int i = 0; i < toks.Count; i++)
            {
                if (toks[i].Kind != TokKind.Number) continue;
                var text = toks[i].Text;
                if (!text.StartsWith("0x") && !text.StartsWith("0X")) continue;
                long v;
                if (!long.TryParse(text.Substring(2).TrimEnd('u', 'U', 'l', 'L'), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out v)) continue;
                // Inside the argument list of a hook call, or cast to a function pointer, or added to a byte pointer.
                bool inHook = false, castFn = false, ptrMath = false;
                for (int k = i - 1; k >= 0 && k > i - 40; k--)
                {
                    if (T(f, k) == ";" || T(f, k) == "{" || T(f, k) == "}") break;
                    if ((T(f, k) == "BasicHook" || T(f, k) == "DobbyHook" || T(f, k) == "InvokeHook") && T(f, k + 1) == "(" && f.Match[k + 1] > i) inHook = true;
                    if (T(f, k) == "reinterpret_cast" || T(f, k) == "(") { if (k + 1 < i && Span(f, k, i).Contains("(*)")) castFn = true; }
                }
                var prevText = T(f, i - 1);
                if (prevText == "+" || prevText == "+=")
                {
                    var stmt = Span(f, Math.Max(0, i - 12), i);
                    if (stmt.Contains("char*") || stmt.Contains("uint8_t*") || stmt.Contains("uintptr_t") || stmt.Contains("char *") || stmt.Contains("BNM_PTR")) ptrMath = true;
                }
                if (v >= 0x100000 && (inHook || castFn || ptrMath))
                    Add(f, toks[i].Line, toks[i].Col, "error", "address-literal",
                        text + " is used as a code address; addresses change between game builds and Sprig resolves methods by name",
                        "BNM::Class(\"ns\", \"Type\").GetMethod(\"Name\", n)", HooksGuide + "#never-use-addresses");
                else if (ptrMath && v > 0 && v < 0x1000)
                    Add(f, toks[i].Line, toks[i].Col, "warning", "address-literal",
                        text + " is a hard-coded field offset; offsets come from dump.cs and change with the game version",
                        "BNM::Class(\"ns\", \"Type\").GetField(\"Name\").GetOffset()", HooksGuide + "#fields");
            }
        }

        bool IsReferenceType(string csType, DumpType owner)
        {
            if (csType == "string" || csType == "object" || csType.EndsWith("[]")) return true;
            if (csType == "bool" || csType == "int" || csType == "uint" || csType == "long" || csType == "ulong" || csType == "float" ||
                csType == "double" || csType == "short" || csType == "ushort" || csType == "byte" || csType == "sbyte" || csType == "char" ||
                csType == "IntPtr" || csType == "UIntPtr") return false;
            if (Index == null) return false;
            var t = Index.ResolveTypeName(csType, owner);
            if (t == null) return false;
            return t.Kind == "class" || t.Kind == "interface";
        }

        void RuleWriteBarrier(SourceFile f)
        {
            var toks = f.Tokens;
            for (int i = 0; i + 1 < toks.Count; i++)
            {
                if (T(f, i) != "=" ) continue;
                int stmtStart = i - 1;
                while (stmtStart > 0 && T(f, stmtStart - 1) != ";" && T(f, stmtStart - 1) != "{" && T(f, stmtStart - 1) != "}") stmtStart--;
                var lhs = Span(f, stmtStart, i - 1);
                int semi = i + 1;
                while (semi < toks.Count && T(f, semi) != ";") semi++;
                var rhs = Span(f, i + 1, semi - 1);
                if (rhs == "nullptr" || rhs == "0" || rhs == "NULL" || rhs.Length == 0) continue;
                var line = Span(f, stmtStart, semi);
                if (line.Contains("wbarrier") || line.Contains("SetReference")) continue;
                bool rawSlot = (lhs.StartsWith("*reinterpret_cast<") || lhs.StartsWith("*(")) && lhs.Contains("**");
                bool fieldSet = false;
                // fieldVar[obj] = value  for a reference-typed BNM field
                if (!rawSlot && toks[stmtStart].Kind == TokKind.Ident && T(f, stmtStart + 1) == "[")
                {
                    var b = Lookup(f, toks[stmtStart].Text, stmtStart);
                    if (b != null && b.Kind == "field" && b.Type != null && Index != null)
                        foreach (var fl in FieldChain(b.Type, b.Member))
                            if (!fl.IsStatic && IsReferenceType(fl.Type, fl.Owner)) fieldSet = true;
                }
                if (rawSlot || fieldSet)
                    Add(f, toks[i].Line, toks[i].Col, "warning", "write-barrier",
                        "a managed reference is stored into an object without IL2CPP's write barrier; the incremental GC can miss it and free the object",
                        "il2cpp_gc_wbarrier_set_field(object, reinterpret_cast<void **>(slot), value)  (see Bloom/DownloadAll)", HooksGuide + "#write-barrier");
            }
        }

        IEnumerable<DumpField> FieldChain(DumpType type, string name)
        {
            for (var t = type; t != null; t = t.BaseType)
                foreach (var fl in t.Fields)
                    if (fl.Name == name) yield return fl;
        }

        void RuleExceptions(SourceFile f, List<HookSite> hooks)
        {
            var hookFns = new HashSet<string>();
            foreach (var h in hooks) hookFns.Add(StripTemplate(h.FnText));
            // A managed exception raised inside a hook unwinds into the game's own handlers, as if the
            // original had thrown. A C++ exception does not: IL2CPP code only catches managed ones.
            foreach (var fn in f.Functions)
            {
                if (!hookFns.Contains(fn.Name)) continue;
                for (int i = fn.Open; i < fn.Close; i++)
                {
                    if (T(f, i) != "throw" || T(f, i + 1) == ";") continue;
                    if (InsideTry(f, fn, i)) continue;
                    var tok = f.Tokens[i];
                    Add(f, tok.Line, tok.Col, "warning", "exceptions",
                        "hook " + fn.Name + " throws a C++ exception into game code, which only catches managed exceptions; the game would abort",
                        "log the problem and return a safe value (or call the original) instead", HooksGuide + "#exceptions");
                }
            }
        }

        static bool InsideTry(SourceFile f, FunctionInfo fn, int at)
        {
            for (int i = fn.Open; i < at; i++)
                if (T(f, i) == "try" && T(f, i + 1) == "{" && f.Match[i + 1] > at) return true;
            return false;
        }

        static readonly string[] CreationApis = { "CreateNewObjectParameters", "CreateNewObject", "CreateNewInstance", "CreateMonoString", "NewList", "NewListObject", "il2cpp_object_new", "il2cpp_string_new", "NewArray" };

        void RuleManagedGlobal(SourceFile f)
        {
            var toks = f.Tokens;
            for (int i = 1; i + 1 < toks.Count; i++)
            {
                if (T(f, i) != "=" || toks[i - 1].Kind != TokKind.Ident) continue;
                var name = toks[i - 1].Text;
                var before = T(f, i - 2);
                if (before == "." || before == "->") continue;
                bool isGlobal = f.Globals.Contains(name) && !IsLocalDeclared(f, name, i - 1);
                bool isStaticLocal = false;
                int fnIdx = f.FunctionAt[i];
                if (fnIdx >= 0)
                {
                    for (int k = i - 2; k >= f.Functions[fnIdx].Open && k > i - 8; k--) if (T(f, k) == "static") isStaticLocal = true;
                }
                if (!isGlobal && !isStaticLocal) continue;
                int semi = i + 1;
                while (semi < toks.Count && T(f, semi) != ";") semi++;
                var rhs = Span(f, i + 1, semi - 1);
                bool creates = false;
                foreach (var api in CreationApis) if (rhs.Contains(api)) creates = true;
                if (rhs.Contains("gchandle")) continue;
                if (creates)
                {
                    if (HasGcHandle(f, name)) continue;
                    Add(f, toks[i].Line, toks[i].Col, "error", "managed-global",
                        name + " keeps a managed object the mod created, but native globals are invisible to the garbage collector: the object can be freed while " + name + " still points at it",
                        "uint32_t handle = il2cpp_gchandle_new(obj, false);  (or create the object when you need it)", HooksGuide + "#keeping-objects");
                    continue;
                }
                int e;
                var value = Eval(f, i + 1, semi - 1, out e);
                bool fieldRead = rhs.StartsWith("Read<") || rhs.Contains("]()") || rhs.EndsWith(".Get()");
                if (fieldRead && value.Kind != "method" && value.Kind != "type" && value.Kind != "field" && value.Kind != "property" && !HasGcHandle(f, name))
                    Add(f, toks[i].Line, toks[i].Col, "warning", "managed-global",
                        name + " keeps a game object in a native global; it stays valid only while the game keeps its own reference",
                        "re-read it when needed, or hold an il2cpp_gchandle_new handle", HooksGuide + "#keeping-objects");
            }
        }

        static bool IsLocalDeclared(SourceFile f, string name, int at)
        {
            int fn = f.FunctionAt[at];
            if (fn < 0) return false;
            var info = f.Functions[fn];
            if (info.Params.Contains(name)) return true;
            for (int i = info.Open; i < at; i++)
            {
                if (f.Tokens[i].Text != name) continue;
                var prev = T(f, i - 1);
                if (f.Tokens[i - 1].Kind == TokKind.Ident && prev != "return" && prev != "static" || prev == "*" && (T(f, i - 2) == "auto" || f.Tokens[i - 2].Kind == TokKind.Ident)) return true;
            }
            return false;
        }

        static bool HasGcHandle(SourceFile f, string name)
        {
            for (int i = 0; i + 2 < f.Tokens.Count; i++)
                if (f.Tokens[i].Text.Contains("gchandle_new") && f.Match.Length > i + 1 && T(f, i + 1) == "(")
                {
                    int close = f.Match[i + 1];
                    for (int k = i + 2; k < close; k++) if (f.Tokens[k].Text == name) return true;
                }
            return false;
        }

        void RuleEnumRange(SourceFile f)
        {
            if (Index == null) return;
            var toks = f.Tokens;
            for (int i = 0; i < toks.Count; i++)
            {
                if (toks[i].Kind != TokKind.Ident) continue;
                var b = Lookup(f, toks[i].Text, i);
                if (b == null || b.Kind != "field" || b.Type == null) continue;
                if (T(f, i - 1) == "=" || T(f, i + 1) == "=") continue;   // the binding itself
                DumpField field = null;
                foreach (var fl in FieldChain(b.Type, b.Member)) { field = fl; break; }
                if (field == null) continue;
                var enumType = Index.ResolveTypeName(field.Type, field.Owner);
                if (enumType == null || enumType.Kind != "enum") continue;
                // fieldVar[obj] = K  or  call(..., fieldVar, ..., K)
                int stmtEnd = i;
                while (stmtEnd < toks.Count && T(f, stmtEnd) != ";") stmtEnd++;
                long value;
                bool found = false;
                int valueToken = -1;
                if (T(f, i + 1) == "[" && f.Match[i + 1] > 0 && T(f, f.Match[i + 1] + 1) == "=")
                {
                    int start = f.Match[i + 1] + 2;
                    found = TryConstant(f, new[] { start, stmtEnd - 1 }, out value);
                    valueToken = start;
                    if (found) CheckEnumValue(f, valueToken, field, enumType, value);
                    continue;
                }
                // Enclosing call's arguments
                int open = i - 1;
                while (open >= 0 && T(f, open) != "(" && T(f, open) != ";" && T(f, open) != "{") open--;
                if (open < 0 || T(f, open) != "(") continue;
                foreach (var arg in Args(f, open))
                {
                    if (arg[0] == i) continue;
                    if (TryConstant(f, arg, out value)) CheckEnumValue(f, arg[0], field, enumType, value);
                }
            }
        }

        void CheckEnumValue(SourceFile f, int token, DumpField field, DumpType enumType, long value)
        {
            var members = Index.EnumMembers(enumType);
            var names = new List<string>();
            foreach (var kv in members)
            {
                long v;
                if (long.TryParse(kv.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) && v == value) return;
                names.Add(kv.Key + " = " + kv.Value);
            }
            if (enumType.Modifiers.Contains("Flags")) return;
            var tok = f.Tokens[token];
            Add(f, tok.Line, tok.Col, "warning", "enum-range",
                value + " is not a value of " + enumType.Name + " (" + field.Owner.Name + "." + field.Name + "); game code that reads this field never expects it",
                string.Join(", ", names.ToArray()), DslGuide + "#the-alias-shortcut");
        }

        void RulePoolBypass(SourceFile f)
        {
            if (Index == null) return;
            var toks = f.Tokens;
            for (int i = 0; i + 1 < toks.Count; i++)
            {
                var t = T(f, i);
                if (t != "CreateNewObjectParameters" && t != "CreateNewObject" && t != "CreateNewInstance") continue;
                if (T(f, i - 1) != "." && T(f, i - 1) != "->") continue;
                // Evaluate the class expression that ends before '.'
                int start = i - 2;
                while (start > 0 && (toks[start].Kind == TokKind.Ident || T(f, start) == "::" || T(f, start) == ")" || T(f, start) == "." || T(f, start) == "("))
                {
                    if (T(f, start) == ")" && f.Match[start] >= 0) { start = f.Match[start] - 1; continue; }
                    if (T(f, start - 1) == "." || T(f, start - 1) == "::" || T(f, start - 1) == "->") { start -= 2; continue; }
                    break;
                }
                int e;
                var r = Eval(f, start, i - 2, out e);
                if (r.Kind != "type" || r.Type == null) continue;
                if (!r.Type.Implements("Poolable")) continue;
                var tok = toks[i];
                Add(f, tok.Line, tok.Col, "warning", "pool-bypass",
                    r.Type.Name + " implements Poolable; creating it directly bypasses the game's object pool",
                    "", DslGuide + "#pooled-objects");
            }
        }

        // ================================================================ custom classes

        void RuleCustomClass(SourceFile f)
        {
            var toks = f.Tokens;
            for (int i = 0; i + 1 < toks.Count; i++)
            {
                if (T(f, i) != "BNM_CustomClass" || T(f, i + 1) != "(") continue;
                var args = Args(f, i + 1);
                if (args.Count < 4) continue;
                var cls = Span(f, args[0][0], args[0][1]);
                DumpType baseType = null;
                string baseText = Span(f, args[2][0], args[2][1]);
                for (int k = args[2][0]; k <= args[2][1]; k++)
                {
                    if (T(f, k) != "CompileTimeClassBuilder" || T(f, k + 1) != "(") continue;
                    var bargs = Args(f, k + 1);
                    if (bargs.Count < 2) break;
                    var ns = StringArg(f, bargs[0]);
                    var nm = StringArg(f, bargs[1]);
                    if (ns == null || nm == null || Index == null) break;
                    baseType = Index.FindBnm(ns, nm);
                    if (baseType == null)
                        Add(f, toks[k].Line, toks[k].Col, "error", "custom-class", "base class " + ns + "." + nm + " is not in dump.cs", "", BnmGuide + "#base-classes");
                    break;
                }
                if (baseType == null) continue;
                // Interfaces listed after the owner argument.
                var interfaces = new List<DumpType>();
                for (int a = 4; a < args.Count; a++)
                    for (int k = args[a][0]; k <= args[a][1]; k++)
                    {
                        if (T(f, k) != "CompileTimeClassBuilder" || T(f, k + 1) != "(") continue;
                        var iargs = Args(f, k + 1);
                        var ins = iargs.Count >= 2 ? StringArg(f, iargs[0]) : null;
                        var inm = iargs.Count >= 2 ? StringArg(f, iargs[1]) : null;
                        if (ins == null || inm == null) break;
                        var it = Index.FindBnm(ins, inm);
                        if (it == null)
                            Add(f, toks[k].Line, toks[k].Col, "error", "custom-class", "interface " + ins + "." + inm + " is not in dump.cs", "", BnmGuide + "#base-classes");
                        else if (it.Kind != "interface")
                            Add(f, toks[k].Line, toks[k].Col, "error", "custom-class", ins + "." + inm + " is a " + it.Kind + ", not an interface; pass it as the base type instead", "", BnmGuide + "#base-classes");
                        else interfaces.Add(it);
                        break;
                    }
                // Methods declared with BNM_CustomMethod inside the same struct.
                int structOpen = i;
                while (structOpen > 0 && !(T(f, structOpen) == "{" && f.Match[structOpen] > i)) structOpen--;
                int structClose = structOpen >= 0 && f.Match[structOpen] > 0 ? f.Match[structOpen] : toks.Count - 1;
                var provided = new Dictionary<string, int>();
                var fields = new List<string>();
                for (int k = structOpen; k < structClose; k++)
                {
                    if (T(f, k) == "BNM_CustomMethod" && T(f, k + 1) == "(")
                    {
                        var margs = Args(f, k + 1);
                        if (margs.Count >= 4)
                        {
                            var mname = StringArg(f, margs[3]) ?? "";
                            bool isStatic = Span(f, margs[1][0], margs[1][1]) == "true";
                            int pcount = margs.Count - 4;
                            provided[mname] = pcount;
                            foreach (var bm in VirtualMethods(baseType, interfaces))
                            {
                                if (bm.Name != mname) continue;
                                if (isStatic)
                                    Add(f, toks[k].Line, toks[k].Col, "error", "custom-class", mname + " overrides a virtual method of " + bm.Owner.Name + " and must not be static", "", BnmGuide + "#overriding");
                                if (pcount != bm.Params.Count)
                                    Add(f, toks[k].Line, toks[k].Col, "error", "custom-class",
                                        mname + " is declared with " + pcount + (pcount == 1 ? " parameter" : " parameters") + " but " + bm.Owner.Name + "." + bm.Name + " takes " + bm.Params.Count,
                                        bm.Signature(), BnmGuide + "#overriding");
                            }
                        }
                    }
                    if (T(f, k) == "BNM_CustomField" && T(f, k + 1) == "(")
                    {
                        var fargs = Args(f, k + 1);
                        if (fargs.Count >= 1) fields.Add(Span(f, fargs[0][0], fargs[0][1]));
                    }
                }
                foreach (var bm in VirtualMethods(baseType, interfaces))
                {
                    if (!(bm.IsAbstract || bm.Owner.Kind == "interface") || provided.ContainsKey(bm.Name)) continue;
                    Add(f, toks[i].Line, toks[i].Col, "error", "custom-class",
                        cls + " does not implement abstract method " + bm.Owner.Name + "." + bm.Name + "; the engine calls it and finds no code",
                        bm.Signature(), BnmGuide + "#overriding");
                }
                bool approx;
                int baseSize = Index.InstanceSize(baseType, out approx);
                var tok = toks[i];
                AddCheck(f, "static_assert(sizeof(" + cls + ") >= " + baseSize + ", \"sprig-lint\");", tok.Line, tok.Col, "custom-class",
                    cls + " is smaller than its base " + baseType.Name + " (instance size 0x" + baseSize.ToString("X") + "); inherited fields would overlap memory the class does not own",
                    "derive " + cls + " from a struct that lays out " + baseType.Name + "'s fields (sprig find " + baseType.Name + ")", BnmGuide + "#layout");
                foreach (var field in fields)
                    AddCheck(f, "static_assert(offsetof(" + cls + ", " + field + ") >= " + baseSize + ", \"sprig-lint\");", tok.Line, tok.Col, "custom-class",
                        "custom field " + field + " overlaps the fields " + cls + " inherits from " + baseType.Name + " (they end at 0x" + baseSize.ToString("X") + ")",
                        "declare custom fields after the inherited layout", BnmGuide + "#layout");
            }
        }

        // Virtual methods of the base chain, then every method of the interfaces (and the interfaces
        // they extend). A base class's implementation of an interface method counts as provided.
        IEnumerable<DumpMethod> VirtualMethods(DumpType type, List<DumpType> extraInterfaces)
        {
            var seen = new HashSet<string>();
            var result = new List<DumpMethod>();
            var implemented = new HashSet<string>();
            for (var t = type; t != null; t = t.BaseType)
                foreach (var m in t.Methods)
                {
                    if (!m.IsAbstract && m.Rva > 0) implemented.Add(m.Name + "/" + m.Params.Count);
                    if ((m.IsVirtual || m.IsAbstract) && seen.Add(m.Name + "/" + m.Params.Count)) result.Add(m);
                }
            var queue = new List<DumpType>();
            for (var t = type; t != null; t = t.BaseType) queue.AddRange(t.Interfaces);
            if (extraInterfaces != null) queue.AddRange(extraInterfaces);
            var visited = new HashSet<DumpType>();
            for (int q = 0; q < queue.Count; q++)
            {
                var iface = queue[q];
                if (!visited.Add(iface)) continue;
                queue.AddRange(iface.Interfaces);
                foreach (var m in iface.Methods)
                {
                    var key = m.Name + "/" + m.Params.Count;
                    if (implemented.Contains(key)) continue;
                    if (seen.Add(key)) result.Add(m);
                }
            }
            return result;
        }

        // ================================================================ compiler tier

        void AddCheck(SourceFile f, string assertion, int line, int col, string rule, string message, string expected, string see)
        {
            var c = new CompilerCheck();
            c.File = f.RelPath; c.Line = line; c.Col = col; c.Rule = rule; c.Message = message; c.Expected = expected; c.See = see;
            c.Severity = "error";
            f.Checks.Add(c);
            f.CheckSource.AppendLine(assertion);
        }

        static string FloatClass(string csType)
        {
            if (csType == "float") return "float";
            if (csType == "double") return "double";
            return "";
        }

        void BuildCompilerChecks(SourceFile f, List<HookSite> hooks)
        {
            if (Index == null) return;
            foreach (var site in hooks)
            {
                if (site.Method == null) continue;
                var m = site.Method;
                var tok = f.Tokens[site.Token];
                int arity = (m.IsStatic ? 0 : 1) + m.Params.Count + 1;
                var expected = Index.HookSignature(m, null);
                var desc = m.Owner.Name + "." + m.Name;
                var fnRef = "&" + site.FnText.TrimStart('&');
                var fnType = "decltype(" + fnRef + ")";
                var parts = new List<string>();
                if (!m.IsStatic) parts.Add("self");
                foreach (var p in m.Params) parts.Add(p.Name);
                parts.Add("const MethodInfo*");
                var needs = "(" + string.Join(", ", parts.ToArray()) + ")";
                AddCheck(f, "static_assert(sprig_lint::Fn<" + fnType + ">::Arity == " + arity + ", \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                    desc + " is " + (m.IsStatic ? "a static" : "an instance") + " method with " + m.Params.Count + " parameter" + (m.Params.Count == 1 ? "" : "s") +
                    "; the hook " + site.FnText + " must take " + arity + " " + needs, expected, HooksGuide + "#signatures");
                // Return type
                var ret = m.ReturnType;
                if (ret == "void")
                    AddCheck(f, "static_assert(std::is_void<sprig_lint::Fn<" + fnType + ">::Ret>::value, \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                        desc + " returns void; the hook must return void", expected, HooksGuide + "#signatures");
                else
                {
                    var fc = FloatClass(ret);
                    if (fc.Length > 0)
                        AddCheck(f, "static_assert(std::is_same<sprig_lint::Fn<" + fnType + ">::Ret, " + fc + ">::value, \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                            desc + " returns " + ret + "; the hook must return " + fc + " (floating-point results use different registers)", expected, HooksGuide + "#signatures");
                    else
                    {
                        AddCheck(f, "static_assert(!std::is_void<sprig_lint::Fn<" + fnType + ">::Ret>::value && !std::is_floating_point<sprig_lint::Fn<" + fnType + ">::Ret>::value, \"sprig-lint\");",
                            tok.Line, tok.Col, "hook-signature", desc + " returns " + ret + "; the hook's return type does not match", expected, HooksGuide + "#signatures");
                        if (NeedsPointerSize(ret, m.Owner))
                            AddCheck(f, "static_assert(sprig_lint::SizeOf<sprig_lint::Fn<" + fnType + ">::Ret>::value == 8, \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                                desc + " returns " + ret + " (8 bytes); a smaller return type truncates it", expected, HooksGuide + "#signatures");
                    }
                }
                // Parameters (checked only when the arity matches, so one mistake reports once).
                int index = m.IsStatic ? 0 : 1;
                if (!m.IsStatic)
                    AddCheck(f, "static_assert(sprig_lint::Fn<" + fnType + ">::Arity != " + arity + " || sprig_lint::SizeOf<sprig_lint::Fn<" + fnType + ">::template Arg<0>>::value == 8, \"sprig-lint\");",
                        tok.Line, tok.Col, "hook-signature", desc + ": the first parameter of an instance-method hook is self (a pointer)", expected, HooksGuide + "#signatures");
                foreach (var p in m.Params)
                {
                    var argType = "sprig_lint::Fn<" + fnType + ">::template Arg<" + index + ">";
                    var guard = "sprig_lint::Fn<" + fnType + ">::Arity != " + arity + " || ";
                    bool byRef = p.Modifier.Contains("out") || p.Modifier.Contains("ref") || p.Modifier == "in";
                    var fc = byRef ? "" : FloatClass(p.Type);
                    if (fc.Length > 0)
                        AddCheck(f, "static_assert(" + guard + "std::is_same<" + argType + ", " + fc + ">::value, \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                            desc + ": parameter " + p.Name + " is " + p.Type + "; declare it as " + fc + " (floating-point arguments use different registers)", expected, HooksGuide + "#signatures");
                    else
                    {
                        AddCheck(f, "static_assert(" + guard + "!std::is_floating_point<" + argType + ">::value, \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                            desc + ": parameter " + p.Name + " is " + p.Type + ", not a floating-point value", expected, HooksGuide + "#signatures");
                        if (byRef || NeedsPointerSize(p.Type, m.Owner))
                            AddCheck(f, "static_assert(" + guard + "sprig_lint::SizeOf<" + argType + ">::value == 8, \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                                desc + ": parameter " + p.Name + " is " + (byRef ? p.Modifier + " " : "") + p.Type + " (8 bytes); a smaller type truncates it", expected, HooksGuide + "#signatures");
                        else
                        {
                            var st = Index.ResolveTypeName(p.Type, m.Owner);
                            if (st != null && st.Kind == "struct")
                            {
                                int size = Index.StructSize(st, 0);
                                if (size > 0)
                                    AddCheck(f, "static_assert(" + guard + "sprig_lint::SizeOf<" + argType + ">::value == " + size + ", \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                                        desc + ": parameter " + p.Name + " is the value type " + p.Type + " (" + size + " bytes); pass a matching struct by value", expected, HooksGuide + "#value-types");
                            }
                        }
                    }
                    index++;
                }
                AddCheck(f, "static_assert(sprig_lint::Fn<" + fnType + ">::Arity != " + arity + " || sprig_lint::SizeOf<sprig_lint::Fn<" + fnType + ">::template Arg<" + (arity - 1) + ">>::value == 8, \"sprig-lint\");",
                    tok.Line, tok.Col, "hook-signature", desc + ": the last parameter of every hook is const MethodInfo* (const void *method)", expected, HooksGuide + "#signatures");
                // The original pointer must have the same shape.
                var orig = site.OrigText;
                if (orig.Length > 0 && IsSimpleIdent(orig))
                    AddCheck(f, "static_assert(sprig_lint::Arity<decltype(" + orig + ")>::value == " + arity + ", \"sprig-lint\");", tok.Line, tok.Col, "hook-signature",
                        "the original pointer " + orig + " must have the same " + arity + " parameters as the hook, including the trailing const MethodInfo*", expected, HooksGuide + "#calling-the-original");
            }
        }

        bool NeedsPointerSize(string csType, DumpType owner)
        {
            if (csType == "long" || csType == "ulong" || csType == "IntPtr" || csType == "UIntPtr" || csType == "double") return true;
            if (csType.EndsWith("*")) return true;
            return IsReferenceType(csType, owner);
        }

        static bool IsSimpleIdent(string s)
        {
            if (s.Length == 0 || !(char.IsLetter(s[0]) || s[0] == '_')) return false;
            foreach (char c in s) if (!(char.IsLetterOrDigit(c) || c == '_')) return false;
            return true;
        }

        public const string CheckPrelude =
            "#include <cstddef>\n#include <tuple>\n#include <type_traits>\n" +
            "#pragma clang diagnostic ignored \"-Winvalid-offsetof\"\n" +
            "namespace sprig_lint {\n" +
            "template <typename T> struct SizeOf { static constexpr std::size_t value = sizeof(T); };\n" +
            "template <> struct SizeOf<void> { static constexpr std::size_t value = 0; };\n" +
            "template <typename T> struct Fn;\n" +
            "template <typename R, typename... A> struct Fn<R (*)(A...)> {\n" +
            "  using Ret = R;\n" +
            "  static constexpr std::size_t Arity = sizeof...(A);\n" +
            "  template <std::size_t I> using Arg = typename std::tuple_element<(I < sizeof...(A) ? I : sizeof...(A)), std::tuple<A..., void>>::type;\n" +
            "};\n" +
            "template <typename R, typename... A> struct Fn<R (*)(A...) noexcept> : Fn<R (*)(A...)> {};\n" +
            "template <typename T> struct Arity { static constexpr std::size_t value = Fn<T>::Arity; };\n" +
            "template <typename T> struct Arity<T &> : Arity<T> {};\n" +
            "}\n";

        // Source of a translation unit that includes the original file and appends the checks.
        // Returns null when the file has no checks, unless `always` is set (plain compile check).
        public string CheckUnit(SourceFile f, bool always, out List<CompilerCheck> checks, out int firstCheckLine)
        {
            checks = f.Checks;
            firstCheckLine = 0;
            if (f.Checks.Count == 0 && !always) return null;
            var sb = new StringBuilder();
            sb.Append("#include \"").Append(f.Path.Replace('\\', '/')).Append("\"\n");
            sb.Append(CheckPrelude);
            int line = 1;
            foreach (char c in sb.ToString()) if (c == '\n') line++;
            firstCheckLine = line;
            var asserts = f.CheckSource.ToString().Split('\n');
            int k = 0;
            foreach (var a in asserts)
            {
                if (a.Trim().Length == 0) continue;
                f.Checks[k].WrapperLine = line;
                sb.Append(a.TrimEnd('\r')).Append('\n');
                line++;
                k++;
            }
            return sb.ToString();
        }

        public IList<SourceFile> FilesWithChecks(bool all)
        {
            var list = new List<SourceFile>();
            foreach (var f in Files)
            {
                bool source = f.Path.EndsWith(".cpp") || f.Path.EndsWith(".cc") || f.Path.EndsWith(".cxx") || f.Path.EndsWith(".C");
                if (f.Checks.Count > 0 || (all && source)) list.Add(f);
            }
            return list;
        }

        // Maps clang diagnostics on the check unit back to findings.
        public void ParseCompilerOutput(SourceFile f, string wrapperPath, string output)
        {
            var wrapperName = Path.GetFileName(wrapperPath);
            var reported = new HashSet<string>();
            foreach (var raw in output.Split('\n'))
            {
                var line = raw.TrimEnd('\r');
                int err = line.IndexOf(": error: ");
                if (err < 0) continue;
                var location = line.Substring(0, err);
                var message = line.Substring(err + 9);
                // path:line:col
                int c2 = location.LastIndexOf(':');
                int c1 = c2 > 0 ? location.LastIndexOf(':', c2 - 1) : -1;
                if (c1 < 0) continue;
                int lineNo;
                if (!int.TryParse(location.Substring(c1 + 1, c2 - c1 - 1), out lineNo)) continue;
                var file = location.Substring(0, c1);
                if (file.EndsWith(wrapperName, StringComparison.OrdinalIgnoreCase))
                {
                    CompilerCheck hit = null;
                    foreach (var c in f.Checks) if (c.WrapperLine == lineNo) hit = c;
                    if (hit == null)
                    {
                        if (reported.Add("prelude:" + message))
                            Add(f.RelPath, 1, 1, "warning", "hook-target", "lint could not compile its checks for this file: " + message.Trim(), "", HooksGuide + "#checked-hooks");
                        continue;
                    }
                    if (message.Contains("static assertion failed") || message.Contains("static_assert failed"))
                    {
                        if (reported.Add(hit.Line + ":" + hit.Rule + ":" + hit.Message))
                            Add(hit.File, hit.Line, hit.Col, hit.Severity, hit.Rule, hit.Message, hit.Expected, hit.See);
                    }
                    else if (reported.Add(hit.Line + ":unchecked"))
                        Add(hit.File, hit.Line, hit.Col, "warning", "hook-target", "lint could not compile its check here: " + message.Trim(),
                            "pass a plain function (not an overloaded or local one) as the hook", HooksGuide + "#checked-hooks");
                }
                else
                {
                    var rel = Rel(file);
                    int colNo = 1;
                    int.TryParse(location.Substring(c2 + 1), out colNo);
                    if (reported.Add(rel + ":" + lineNo + ":" + message))
                        Add(rel, lineNo, colNo, "error", "compile", message.Trim(), "", "");
                }
            }
        }

        // ================================================================ findings

        void Add(SourceFile f, int line, int col, string severity, string rule, string message, string expected, string see)
        {
            if (IsSuppressed(f, line, rule)) return;
            Add(f.RelPath, line, col, severity, rule, message, expected, see);
        }

        static bool IsSuppressed(SourceFile f, int line, string rule)
        {
            List<string[]> list;
            if (f.Suppressions.TryGetValue(line, out list))
                foreach (var s in list) if (s[0] == rule && s[1].Length > 0) return true;
            return false;
        }

        public void Add(string file, int line, int col, string severity, string rule, string message, string expected, string see)
        {
            foreach (var existing in Findings)
                if (existing.File == file && existing.Line == line && existing.Rule == rule && existing.Message == message) return;
            // Suppressions also apply to findings added from compiler output.
            foreach (var src in Files) if (src.RelPath == file && IsSuppressed(src, line, rule)) return;
            var fd = new Finding();
            fd.File = file; fd.Line = line; fd.Col = col; fd.Severity = severity; fd.Rule = rule; fd.Message = message; fd.Expected = expected; fd.See = see;
            Findings.Add(fd);
        }

        public int Errors
        {
            get { int n = 0; foreach (var f in Findings) if (f.Severity == "error") n++; return n; }
        }

        public string Report()
        {
            Findings.Sort((a, b) =>
            {
                int c = string.Compare(a.File, b.File, StringComparison.OrdinalIgnoreCase);
                if (c != 0) return c;
                c = a.Line.CompareTo(b.Line);
                return c != 0 ? c : a.Col.CompareTo(b.Col);
            });
            var sb = new StringBuilder();
            int errors = 0, warnings = 0;
            foreach (var f in Findings)
            {
                if (f.Severity == "error") errors++; else warnings++;
                sb.Append(f.File).Append(':').Append(f.Line).Append(':').Append(f.Col).Append(": ").Append(f.Severity)
                  .Append(" [").Append(f.Rule).Append("] ").Append(f.Message).Append('\n');
                if (f.Expected.Length > 0)
                    foreach (var l in f.Expected.Split('\n')) sb.Append("  expected: ").Append(l).Append('\n');
                if (f.See.Length > 0) sb.Append("  see ").Append(f.See).Append('\n');
            }
            if (errors == 0 && warnings == 0) sb.Append("lint: clean (").Append(Modules).Append(Modules == 1 ? " module)" : " modules)");
            else sb.Append("lint: ").Append(errors).Append(errors == 1 ? " error, " : " errors, ").Append(warnings).Append(warnings == 1 ? " warning in " : " warnings in ")
                   .Append(Modules).Append(Modules == 1 ? " module" : " modules");
            return sb.ToString();
        }
    }
}
