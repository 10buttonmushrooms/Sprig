#requires -Version 5.1
# Host tests for `sprig find` and `sprig lint` (docs/agents/lint.md).
# - find and the text-tier lint rules run against a small fixture dump (always).
# - The compiler-tier checks run when ANDROID_NDK_ROOT and deps\ exist (CI's build job).
# - Sprig's own modules and the guide examples are checked against work\dump\dump.cs when it exists.
$ErrorActionPreference = 'Stop'
$ProjectRoot = (Resolve-Path "$PSScriptRoot\..\..").Path
. "$PSScriptRoot\sprig\common.ps1"
Import-SprigTools

$failures = New-Object System.Collections.Generic.List[string]
function Assert-True {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { $script:failures.Add($Message) }
}

$sandbox = Join-Path ([System.IO.Path]::GetTempPath()) "sprig-tools-$([guid]::NewGuid())"
function Write-Fixture {
    param([string]$Relative, [string]$Text)
    $path = Join-Path $sandbox $Relative
    New-Item -ItemType Directory -Force (Split-Path $path) | Out-Null
    [System.IO.File]::WriteAllText($path, $Text, [System.Text.UTF8Encoding]::new($false))
}

$dumpText = @'
// Image 0: Assembly-CSharp.dll - 0
// Image 1: mscorlib.dll - 10
// Image 2: UnityEngine.CoreModule.dll - 20
// Image 3: EngineLib.dll - 30

// Namespace: System
[Serializable]
public class Object // TypeDefIndex: 10
{
\t// Methods

\t// RVA: 0x500 Offset: 0x500 VA: 0x500
\tpublic void .ctor() { }
}

// Namespace:
private enum string.Kind // TypeDefIndex: 11
{
\t// Fields
\tpublic int value__; // 0x0
\tpublic const string.Kind Plain = 0;
}

// Namespace:
private struct string.Probe // TypeDefIndex: 12
{}

// Namespace: System
[DefaultMember("Chars")]
public sealed class String : IComparable, IEnumerable<char> // TypeDefIndex: 13
{
\t// Fields
\tprivate int _stringLength; // 0x10
}

// Namespace:
private struct Dictionary.Entry<TKey, TValue> // TypeDefIndex: 14
{
\t// Fields
\tpublic int hashCode; // 0x0
\tpublic TKey key; // 0x0
}

// Namespace: System.Collections.Generic
public class Dictionary<TKey, TValue> : IDictionary<TKey, TValue> // TypeDefIndex: 15
{
\t// Fields
\tprivate int[] _buckets; // 0x0
\tprivate int _count; // 0x0

\t// Methods

\t// RVA: -1 Offset: -1
\tpublic bool ContainsKey(TKey key) { }
\t/* GenericInstMethod :
\t|
\t|-RVA: 0x600 Offset: 0x600 VA: 0x600
\t|-Dictionary<object, object>.ContainsKey
\t*/
}

// Namespace: UnityEngine
public struct Vector2 // TypeDefIndex: 20
{
\t// Fields
\tpublic float x; // 0x0
\tpublic float y; // 0x4
}

// Namespace: Engine
public interface Poolable // TypeDefIndex: 30
{
\t// Methods

\t// RVA: -1 Offset: -1 Slot: 0
\tpublic abstract void HandleReturnToPool(InstanceManager instanceManager);
}

// Namespace: Engine
public class InstanceManager // TypeDefIndex: 31
{
\t// Methods

\t// RVA: 0x700 Offset: 0x700 VA: 0x700 Slot: 4
\tpublic virtual Poolable Acquire(Type type, bool shouldInject) { }
}

// Namespace: Engine
public interface EntityQuery : Poolable // TypeDefIndex: 32
{
\t// Methods

\t// RVA: -1 Offset: -1 Slot: 0
\tpublic abstract bool Matches(Entity entity, EntityQueryContext context);
}

// Namespace: Engine
public enum Mode // TypeDefIndex: 33
{
\t// Fields
\tpublic int value__; // 0x0
\tpublic const Mode Off = 0;
\tpublic const Mode On = 1;
}

// Namespace: Engine
public class Holder // TypeDefIndex: 34
{
\t// Fields
\tpublic int Count; // 0x10
\tpublic Mode Mode; // 0x14
\tpublic Holder Next; // 0x18
\t[CompilerGenerated]
\tprivate string <Label>k__BackingField; // 0x20
\tpublic const int Max = 4;

\t// Properties
\tpublic string Label { get; set; }

\t// Methods

\t// RVA: 0x1000 Offset: 0x1000 VA: 0x1000
\tpublic void Add(int amount) { }

\t// RVA: 0x1010 Offset: 0x1010 VA: 0x1010
\tpublic void Add(string name) { }

\t// RVA: 0x1020 Offset: 0x1020 VA: 0x1020
\tpublic void Add(int amount, bool notify = True) { }

\t// RVA: 0x1030 Offset: 0x1030 VA: 0x1030
\tpublic static float Scale(float value) { }

\t// RVA: 0x1040 Offset: 0x1040 VA: 0x1040 Slot: 4
\tpublic virtual bool Check(Holder other, Vector2 at) { }

\t// RVA: 0x1050 Offset: 0x1050 VA: 0x1050
\tpublic string get_Label() { }

\t// RVA: 0x1060 Offset: 0x1060 VA: 0x1060
\tpublic void .ctor() { }
}

// Namespace: Engine.Queries
public class CountQuery : EntityQuery, Poolable // TypeDefIndex: 35
{
\t// Fields
\tpublic int Count; // 0x10

\t// Methods

\t// RVA: 0x1100 Offset: 0x1100 VA: 0x1100 Slot: 4
\tpublic bool Matches(Entity entity, EntityQueryContext context) { }

\t// RVA: 0x1110 Offset: 0x1110 VA: 0x1110 Slot: 5
\tpublic void HandleReturnToPool(InstanceManager instanceManager) { }
}

// Namespace: Engine
public class Pool<T> // TypeDefIndex: 36
{
\t// Fields
\tprivate List<T> items; // 0x0
\tprivate int size; // 0x0
}

// Namespace: Engine
public class Entity // TypeDefIndex: 37
{
\t// Fields
\tpublic readonly List<Component> Components; // 0x10
}

// Namespace: Engine
public class EntityQueryContext // TypeDefIndex: 38
{}

// Namespace: Engine
public class Odd // TypeDefIndex: 39
{
\t// Fields
\tpublic int Visible; // 0x10
\tpublic fixed weird Hidden thing
}

// Namespace: Engine
public record Weird // TypeDefIndex: 40
{
\t// Fields
\tpublic int Value; // 0x10
}

// Namespace: Engine
[Tooltip(" first
 second")]
public class Tooltips // TypeDefIndex: 41
{
\t// Fields
\t[Tooltip(" Russian -> ru
 Japanese -> ja")]
\tpublic int Shown; // 0x10
\tinternal static readonly Tooltips.__StaticArrayInitTypeSize=12 0EBB /*Metadata offset 0x8BD5A8*/; // 0x0
}
'@.Replace('\t', "`t")

try {
    Write-Fixture 'dump.cs' $dumpText
    $dumpPath = Join-Path $sandbox 'dump.cs'
    $idx = [Sprig.DumpIndex]::Parse($dumpPath)

    # ================================================================ find
    function Invoke-Find {
        param([string[]]$Query)
        $sb = New-Object System.Text.StringBuilder
        $code = [Sprig.FindCommand]::Run($idx, $Query, $sb)
        return [pscustomobject]@{ Code = $code; Text = $sb.ToString() }
    }
    function Assert-Find {
        param([string[]]$Query, [int]$Code, [string[]]$Contains)
        $r = Invoke-Find $Query
        Assert-True ($r.Code -eq $Code) "find $($Query -join ' '): exit $($r.Code), expected $Code"
        foreach ($c in $Contains) {
            Assert-True ($r.Text.Contains($c)) "find $($Query -join ' '): output lacks '$c'`n$($r.Text)"
        }
    }
    Assert-Find @('Holder') 0 @('Engine.Holder  (public class', '0x10    int Count', '0x18    Holder Next', 'BNM: BNM::Class("Engine", "Holder")', 'instance size: 0x28', 'const   int Max = 4')
    Assert-Find @('Holder.Add') 0 @('GetMethod("Add", {"amount"})', 'other overloads', '2 overloads take 1 parameter;')
    Assert-Find @('Holder.Scale') 0 @('(static,', 'hook: float (*)(float value, const void *method)')
    Assert-Find @('Holder.Check') 0 @('void *self', 'Vector2 /* struct */ at', 'const void *method)', 'value type')
    Assert-Find @('Holder.Mode') 0 @('offset 0x14', 'enum Mode: Off = 0, On = 1', 'GetField("Mode")')
    Assert-Find @('Holder.Label') 0 @('GetProperty("Label")', 'get_Label', 'backing field: <Label>k__BackingField at 0x20')
    Assert-Find @('System.String') 0 @('System.String  (public sealed class')
    Assert-Find @('String.Kind') 0 @('nested in: System.String', 'Plain = 0')
    Assert-Find @('Dictionary.Entry') 0 @('BNM::Class("System.Collections.Generic", "Dictionary`2").GetInnerClass("Entry")')
    Assert-Find @('Pool') 0 @('n/a', 'open generic definition')
    Assert-Find @('EntityQuery') 0 @('abstract bool Matches(Entity entity, EntityQueryContext context)')
    Assert-Find @('Mode') 0 @('Off = 0', 'On = 1')
    Assert-Find @('--derives', 'EntityQuery') 0 @('Engine.Queries.CountQuery  { Count (int) }')
    Assert-Find @('--rva', '0x1015') 0 @('is in Engine.Holder.Add (+0x5)')
    Assert-Find @('--rva', '0x600') 0 @('Dictionary<object, object>.ContainsKey')
    Assert-Find @('Holdr') 1 @('not in dump.cs: Holdr', 'closest: Engine.Holder')
    Assert-Find @('Holder.Ad') 1 @('not in dump.cs: Holder.Ad', 'Engine.Holder.Add')
    Assert-Find @() 2 @('usage: sprig find')
    $long = Invoke-Find @('Holder'); $all = Invoke-Find @('Holder', '--all')
    Assert-True ($all.Text.Length -ge $long.Text.Length) 'find --all must not print less than the capped card'

    # Lines the parser cannot read are reported, never silently dropped.
    Assert-True ($idx.UnparsedTypes -eq 1) "The fixture has one unreadable type declaration, got $($idx.UnparsedTypes)."
    Assert-Find @('Odd') 0 @('0x10    int Visible', 'note: sprig could not read 1 line(s) of Engine.Odd in dump.cs (first at dump.cs:')
    Assert-Find @('Odd.Hidden') 1 @('not in dump.cs: Odd.Hidden', 'note: sprig could not read dump.cs:183, which mentions Hidden')
    Assert-Find @('Weird') 1 @('note: sprig could not read the type declaration at dump.cs:187, which mentions Weird')
    $holdr = Invoke-Find @('Holdr')
    Assert-True ($holdr.Text -notmatch 'could not read') "A name no unread line mentions must not get the unread note:`n$($holdr.Text)"
    # Multi-line attributes and "/*Metadata offset*/" fields are read.
    Assert-Find @('Tooltips') 0 @('0x10    int Shown', 'static  Tooltips.__StaticArrayInitTypeSize=12 0EBB')
    Assert-True ($idx.FindExact('Engine', 'Tooltips').Unparsed -eq 0) 'Tooltips must parse completely.'

    # The index cache rebuilds when the dump changes.
    $cache = Join-Path $sandbox 'dump.cs.idx'
    $first = [Sprig.DumpIndex]::LoadOrBuild($dumpPath, $cache)
    Assert-True (Test-Path $cache) 'LoadOrBuild must write the index cache.'
    $second = [Sprig.DumpIndex]::LoadOrBuild($dumpPath, $cache)
    Assert-True ($second.Types.Count -eq $first.Types.Count -and $null -ne $second.FindExact('System', 'String')) 'A cached index must equal the parsed one.'
    [System.IO.File]::AppendAllText($dumpPath, "`n// Namespace: Engine`npublic class Extra // TypeDefIndex: 50`n{}`n")
    $third = [Sprig.DumpIndex]::LoadOrBuild($dumpPath, $cache)
    Assert-True ($null -ne $third.FindExact('Engine', 'Extra')) 'The index cache must rebuild when dump.cs changes.'
    $fourth = [Sprig.DumpIndex]::LoadOrBuild($dumpPath, $cache)
    $odd = $fourth.FindExact('Engine', 'Odd')
    Assert-True ($fourth.UnparsedTypes -eq 1 -and [Sprig.DumpIndex]::MentionLine($fourth.UnparsedTypeText, 'Weird') -eq 187 -and
        $odd.Unparsed -eq 1 -and [Sprig.DumpIndex]::UnreadMention($odd, 'Hidden') -eq 183) 'The index cache must keep the unread lines.'
    Write-Host 'Checked sprig find and the index cache.'

    # ================================================================ lint, text tier
    $pnp = "[Bloom]`nType=PnP`n"
    Write-Fixture 'Bloom/BadIni/bloom.ini' "[Bloom]`nType=Plugin`n"
    Write-Fixture 'Bloom/BadIni/main.cpp' "static void Init() {}`n"
    Write-Fixture 'Bloom/NoRegister/bloom.ini' $pnp
    Write-Fixture 'Bloom/NoRegister/main.cpp' "static void Init() {}`n"
    Write-Fixture 'Bloom/Loose/main.cpp' "static void Init() {}`n"
    Write-Fixture 'Bloom/Names/bloom.ini' $pnp
    Write-Fixture 'Bloom/Names/main.cpp' @'
#include <bloom/bloom.h>
static void Init() {
    auto holder = BNM::Class("Engine", "Holdr");
    auto real = BNM::Class("Engine", "Holder");
    auto a = real.GetMethod("Ad", 1);
    auto b = real.GetMethod("Add", 3);
    auto c = real.GetField("Cont");
    auto d = real.GetMethod("Add", 1);
    auto e = real.GetMethod("Add", {"amount"});
    auto f = real.GetProperty("Label");
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Hooks/bloom.ini' $pnp
    Write-Fixture 'Bloom/Hooks/main.cpp' @'
#include <bloom/bloom.h>
using AddFn = void (*)(void *, int, const void *);
AddFn originalAdd = nullptr;
void HookAdd(void *self, int amount, const void *method) { originalAdd(self, amount, method); }
void Elsewhere() { originalAdd(nullptr, 1, nullptr); }
static void Init() {
    BNM::BasicHook(BNM::Class("Engine", "Holder").GetMethod("Add", {"amount"}), HookAdd, originalAdd);
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Phases/bloom.ini' $pnp
    Write-Fixture 'Bloom/Phases/main.cpp' @'
#include <bloom/bloom.h>
static BNM::Class early = BNM::Class("Engine", "Holder");
extern "C" int JNI_OnLoad(void *vm, void *) { BNM::Class("Engine", "Holder"); return 0; }
static void Init() {}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Pointers/bloom.ini' $pnp
    Write-Fixture 'Bloom/Pointers/main.cpp' @'
#include <bloom/bloom.h>
using Fn = void (*)(void *, const void *);
Fn orig = nullptr;
void Hook(void *self, const void *m) { orig(self, m); }
static void Init() {
    void *object = nullptr;
    ::BasicHook(reinterpret_cast<void *>(0x1ED2F84), Hook, orig);
    if (!orig) return;
    int *count = reinterpret_cast<int *>(reinterpret_cast<char *>(object) + 0x18);
    *reinterpret_cast<void **>(reinterpret_cast<char *>(object) + 0x20) = object;
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Throws/bloom.ini' $pnp
    Write-Fixture 'Bloom/Throws/main.cpp' @'
#include <bloom/bloom.h>
#include <stdexcept>
using CheckFn = bool (*)(void *, void *, int, const void *);
CheckFn originalCheck = nullptr;
bool HookCheck(void *self, void *other, int at, const void *method) {
    if (!other) throw std::runtime_error("no other");
    return originalCheck(self, other, at, method);
}
static void Init() {
    BNM::BasicHook(BNM::Class("Engine", "Holder").GetMethod("Check", 2), HookCheck, originalCheck);
    if (!originalCheck) return;
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Globals/bloom.ini' $pnp
    Write-Fixture 'Bloom/Globals/main.cpp' @'
#include <bloom/bloom.h>
void *g_made = nullptr;
void *g_read = nullptr;
template <typename T> T Read(void *o, int off) { return *reinterpret_cast<T *>(static_cast<char *>(o) + off); }
static void Init() {
    auto holder = BNM::Class("Engine", "Holder");
    g_made = holder.CreateNewObjectParameters();
    g_read = Read<void *>(g_made, 8);
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Enums/bloom.ini' $pnp
    Write-Fixture 'Bloom/Enums/main.cpp' @'
#include <bloom/bloom.h>
BNM::Field<int> mode;
constexpr int kBroken = 7;
static void Init() {
    mode = BNM::Class("Engine", "Holder").GetField("Mode");
    void *obj = nullptr;
    mode[obj] = kBroken;
    mode[obj] = 1;
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Pools/bloom.ini' $pnp
    Write-Fixture 'Bloom/Pools/main.cpp' @'
#include <bloom/bloom.h>
static void Init() {
    auto query = BNM::Class("Engine.Queries", "CountQuery").CreateNewObjectParameters();
    (void)query;
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Custom/bloom.ini' $pnp
    Write-Fixture 'Bloom/Custom/main.cpp' @'
#include <bloom/bloom.h>
struct Fake : BNM::IL2CPP::Il2CppObject {
    bool Matches(void *entity) { return true; }
    BNM_CustomClass(Fake, BNM::CompileTimeClassBuilder("Mods", "Fake", "Mods").Build(),
                    BNM::CompileTimeClassBuilder("System", "Object").Build(), {},
                    BNM::CompileTimeClassBuilder("Engine", "EntityQuery").Build());
    BNM_CustomMethod(Matches, false, BNM::Defaults::Get<bool>(), "Matches",
                     BNM::CompileTimeClassBuilder("Engine", "Entity").Build());
};
static void Init() {}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Suppress/bloom.ini' $pnp
    Write-Fixture 'Bloom/Suppress/main.cpp' @'
#include <bloom/bloom.h>
void *g_a = nullptr;
void *g_b = nullptr;
void *g_c = nullptr;
static void Init() {
    auto holder = BNM::Class("Engine", "Holder");
    g_a = holder.CreateNewObjectParameters();  // sprig-lint: allow managed-global the test keeps it alive
    g_b = holder.CreateNewObjectParameters();  // sprig-lint: allow managed-global
    // sprig-lint: allow managed-global a comment on its own line covers the next line
    g_c = holder.CreateNewObjectParameters();
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Target/bloom.ini' $pnp
    Write-Fixture 'Bloom/Target/main.cpp' @'
#include <bloom/bloom.h>
using Fn = void (*)(void *, const void *);
Fn orig = nullptr;
void Hook(void *self, const void *m) { orig(self, m); }
static void Init() {
    BNM::MethodBase target;
    BNM::BasicHook(target, Hook, orig);
    if (!orig) return;
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Clean/bloom.ini' $pnp
    Write-Fixture 'Bloom/Clean/main.cpp' @'
#include <bloom/bloom.h>
using AddFn = void (*)(void *, int, const void *);
AddFn originalAdd = nullptr;
BNM::Field<int> mode;
void HookAdd(void *self, int amount, const void *method) { originalAdd(self, amount, method); }
void Elsewhere() { if (originalAdd) originalAdd(nullptr, 1, nullptr); }
template <typename Hook, typename Original>
bool Install(const BNM::MethodBase &target, Hook hook, Original &original) {
    BNM::BasicHook(target, hook, original);
    return original != nullptr;
}
static void Init() {
    auto holder = BNM::Class("Engine", "Holder");
    mode = holder.GetField("Mode");
    void *obj = nullptr;
    mode[obj] = 0;
    Install(holder.GetMethod("Add", {"amount"}), HookAdd, originalAdd);
    auto label = holder.GetProperty("Label");
    auto count = holder.GetField("Count").GetOffset();
}
BLOOM_REGISTER_PNP(Init);
'@

    Write-Fixture 'Bloom/Unread/bloom.ini' $pnp
    Write-Fixture 'Bloom/Unread/main.cpp' @'
#include <bloom/bloom.h>
static void Init() {
    auto odd = BNM::Class("Engine", "Odd");
    auto hidden = odd.GetField("Hidden");
    auto missing = odd.GetField("Missing");
    auto weird = BNM::Class("Engine", "Weird");
}
BLOOM_REGISTER_PNP(Init);
'@
    Write-Fixture 'Bloom/Locked/bloom.ini' $pnp
    Write-Fixture 'Bloom/Locked/main.cpp' "static void Init() {}`nBLOOM_REGISTER_PNP(Init);`n"

    function Assert-Lint {
        param([string]$Module, [string[]]$Expected)
        $linter = New-Object Sprig.Linter -ArgumentList $idx, $sandbox
        $linter.LintModule((Join-Path $sandbox "Bloom\$Module"))
        $actual = @($linter.Findings | ForEach-Object { '{0} {1} {2}' -f $_.Line, $_.Severity, $_.Rule } | Sort-Object)
        $wanted = @($Expected | Sort-Object)
        Assert-True (($actual -join '; ') -eq ($wanted -join '; ')) "lint $Module`: expected [$($wanted -join '; ')], got [$($actual -join '; ')]`n$($linter.Report())"
    }
    Assert-Lint 'BadIni' @('1 error module')
    Assert-Lint 'NoRegister' @('1 error module')
    Assert-Lint 'Loose' @('1 warning module')
    Assert-Lint 'Names' @('3 error names', '5 error names', '6 error names', '7 error names', '8 warning overload')
    Assert-Lint 'Hooks' @('5 error original', '7 error original')
    Assert-Lint 'Phases' @('2 error init-phase', '3 error init-phase')
    Assert-Lint 'Pointers' @('7 error address-literal', '9 warning address-literal', '10 warning address-literal', '10 warning write-barrier')
    Assert-Lint 'Throws' @('6 warning exceptions')
    Assert-Lint 'Globals' @('7 error managed-global', '8 warning managed-global')
    Assert-Lint 'Enums' @('7 warning enum-range')
    Assert-Lint 'Pools' @('3 warning pool-bypass')
    Assert-Lint 'Custom' @('4 error custom-class', '7 error custom-class')
    Assert-Lint 'Suppress' @('8 error suppression', '8 error managed-global')
    Assert-Lint 'Target' @('7 warning hook-target')
    Assert-Lint 'Clean' @()
    # A missing name that an unread dump.cs line mentions is a warning; other missing names stay errors.
    Assert-Lint 'Unread' @('4 warning names', '5 error names', '6 warning names')
    # A file lint cannot read is a lint-internal warning, not a false "never registers" error.
    $lock = [System.IO.File]::Open((Join-Path $sandbox 'Bloom\Locked\main.cpp'), 'Open', 'Read', 'None')
    try { Assert-Lint 'Locked' @('1 warning lint-internal') } finally { $lock.Dispose() }
    Write-Host 'Checked the lint text-tier fixtures.'

    # ================================================================ lint, compiler tier
    if ($env:ANDROID_NDK_ROOT -and (Test-Path (Join-Path $ProjectRoot 'deps\BNM-Android\include'))) {
        Write-Fixture 'Signatures/bloom.ini' $pnp
        Write-Fixture 'Signatures/main.cpp' @'
#include <bloom/bloom.h>
#include <BNM/Class.hpp>
#include <BNM/MethodBase.hpp>
#include "log.h"
using AddFn = void (*)(void *, int);
AddFn originalAdd = nullptr;
void HookAdd(void *self, int amount) { originalAdd(self, amount); }
using ScaleFn = float (*)(float, const void *);
ScaleFn originalScale = nullptr;
float HookScale(float value, const void *method) { return originalScale(value, method); }
using CheckFn = bool (*)(void *, void *, int, const void *);
CheckFn originalCheck = nullptr;
bool HookCheck(void *self, void *other, int at, const void *method) { return originalCheck(self, other, at, method); }
static void Init() {
    auto holder = BNM::Class("Engine", "Holder");
    BNM::BasicHook(holder.GetMethod("Add", {"amount"}), HookAdd, originalAdd);
    if (!originalAdd) return;
    BNM::BasicHook(holder.GetMethod("Scale", 1), HookScale, originalScale);
    if (!originalScale) return;
    BNM::BasicHook(holder.GetMethod("Check", 2), HookCheck, originalCheck);
    if (!originalCheck) return;
}
BLOOM_REGISTER_PNP(Init);
'@
        $env:SPRIG_DUMP = $dumpPath
        try {
            $out = (& "$PSScriptRoot\sprig\lint.ps1" --dir (Join-Path $sandbox 'Signatures') --compile-all) -join "`n"
            $code = $LASTEXITCODE
        } finally {
            Remove-Item Env:SPRIG_DUMP -ErrorAction SilentlyContinue
        }
        $global:LASTEXITCODE = 0
        Assert-True ($code -eq 1) "compiler tier: lint must fail on the Signatures fixture (exit $code)`n$out"
        Assert-True ($out -match 'main\.cpp:16:\d+: error \[hook-signature\] Holder\.Add is an instance method with 1 parameter') "compiler tier: missing MethodInfo not reported`n$out"
        Assert-True ($out -match 'main\.cpp:16:\d+: error \[hook-signature\] the original pointer originalAdd') "compiler tier: original pointer arity not reported`n$out"
        Assert-True ($out -match 'main\.cpp:20:\d+: error \[hook-signature\] Holder\.Check: parameter at is the value type Vector2 \(8 bytes\)') "compiler tier: struct parameter size not reported`n$out"
        Assert-True ($out -notmatch 'main\.cpp:18:') "compiler tier: the correct Scale hook was reported`n$out"
        Assert-True ($out -notmatch '\[compile\]') "compiler tier: the fixture failed to compile`n$out"
        Write-Host 'Checked the lint compiler tier.'
    } else {
        Write-Host 'Skipping compiler-tier checks: set ANDROID_NDK_ROOT and run build_mod.ps1 once (deps\).'
    }

    # A module name is a folder under Bloom\, never a path.
    foreach ($bad in '..\..\work', 'C:\Windows', 'Bloom/TutorialSkip') {
        $out = (& "$PSScriptRoot\sprig\lint.ps1" $bad) -join "`n"
        $code = $LASTEXITCODE
        $global:LASTEXITCODE = 0
        Assert-True ($code -eq 2 -and $out -match 'invalid module name') "lint must refuse the module name '$bad' (exit $code):`n$out"
    }

    # ================================================================ the real dump
    if (Test-Path (Join-Path $ProjectRoot 'work\dump\dump.cs')) {
        $out = (& "$PSScriptRoot\sprig\lint.ps1") -join "`n"
        $code = $LASTEXITCODE
        $global:LASTEXITCODE = 0
        Assert-True ($code -eq 0 -and $out -notmatch ' error \[') "Sprig's own modules must lint without errors:`n$out"

        # Every complete module example in the guides must lint clean and compile.
        $n = 0
        foreach ($doc in Get-ChildItem (Join-Path $ProjectRoot 'docs\agents') -Filter '*.md') {
            $text = [System.IO.File]::ReadAllText($doc.FullName)
            foreach ($m in [regex]::Matches($text, '```cpp\r?\n(.*?)```', 'Singleline')) {
                $code = $m.Groups[1].Value
                if ($code -notmatch 'BLOOM_REGISTER_PNP') { continue }
                $n++
                $dir = "Examples/Example$n"
                Write-Fixture "$dir/bloom.ini" $pnp
                Write-Fixture "$dir/main.cpp" $code
                $out = (& "$PSScriptRoot\sprig\lint.ps1" --dir (Join-Path $sandbox $dir) --compile-all) -join "`n"
                $exit = $LASTEXITCODE
                $global:LASTEXITCODE = 0
                Assert-True ($exit -eq 0 -and $out -notmatch ' (error|warning) \[') "The example in $($doc.Name) must lint clean and compile:`n$out"
            }
        }
        Assert-True ($n -gt 0) 'No complete module example was found in docs\agents.'
        Write-Host "Checked Sprig's modules and $n guide example(s) against work\dump\dump.cs."
    } else {
        Write-Host 'Skipping the real-dump checks: work\dump\dump.cs is missing.'
    }
} finally {
    if (Test-Path $sandbox) { Remove-Item -LiteralPath $sandbox -Recurse -Force }
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL: $_" }
    throw "$($failures.Count) sprig tool test(s) failed."
}
Write-Host 'All sprig tool tests passed.'
$global:LASTEXITCODE = 0
