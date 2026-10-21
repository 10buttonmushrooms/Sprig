# sprig lint

`.\sprig.ps1 lint [Module...]` checks Bloom modules against `work/dump/dump.cs` before they run.
Without arguments it checks every `Bloom/<Name>` folder (except names starting with `_`) and
`mod/src`. `work/scripts/build_mod.ps1` runs it first and stops on errors; `-NoLint` skips it.

```text
Bloom\EvenStrength\main.cpp:163:10: error [hook-signature] AttackComparisonQuery.Matches is an instance method with 2 parameters; the hook MatchesHook must take 4 (self, entity, context, const MethodInfo*)
  expected: bool (*)(void *self, void * /* Entity */ entity, void * /* EntityQueryContext */ context, const void *method)
  see docs/agents/hooks.md#signatures
lint: 1 error, 0 warnings in 1 module
```

Exit codes: 0 clean or warnings only, 1 errors, 2 lint could not run.

## How it checks

- **Text checks** read your sources and resolve every class, method and field name against
  `dump.cs`.
- **Compiler checks** compile generated `static_assert`s next to your code with the NDK's clang
  (flags from a CMake configure in `work/build-out/lint`): hook signatures and injected-class layouts.
  They need `ANDROID_NDK_ROOT` and `deps/` (after one `build_mod.ps1`).

Options: `--no-compile` (text checks only), `--compile-all` (also compile files without checks, to
catch compile errors), `--dir <path>` (check a module folder outside `Bloom/`).

## Rules

| Rule | Severity | Catches | Fix |
| --- | --- | --- | --- |
| `module` | error / warning | `bloom.ini` without exactly one `[Bloom] Type=Core` or `Type=PnP`; an invalid name; a PnP module without exactly one `BLOOM_REGISTER_PNP`, or a Core module with one. A folder with code but no `bloom.ini` is a warning (the build ignores it) | [Bloom/README.md](../../Bloom/README.md) |
| `names` | error | a class, inner class, method, field or property that is not in `dump.cs`, or a `GetMethod` parameter count no overload has (a warning when a `dump.cs` line sprig could not read mentions the name; the message says where) | use a name `sprig find` prints |
| `overload` | warning | `GetMethod(name, n)` when several overloads share the count, or `GetMethod(name)` with overloads | [hooks.md](hooks.md#overloads) |
| `hook-signature` | error | a hook (or its `original` pointer) that does not match the method: static or instance, parameter count, `float` vs integer, sizes, the trailing `const MethodInfo*` | [hooks.md](hooks.md#signatures) |
| `hook-target` | warning | a hook whose target lint cannot trace to one method, so its signature is not checked | [hooks.md](hooks.md#checked-hooks) |
| `original` | error | a hook result never checked, or `original` called outside its hook without a null check | [hooks.md](hooks.md#calling-the-original) |
| `init-phase` | error | BNM lookups at file scope, in static initializers or in `JNI_OnLoad` | [hooks.md](hooks.md#when-bnm-is-ready) |
| `address-literal` | error / warning | a literal code address (error) or a literal field offset added to a pointer (warning) | [hooks.md](hooks.md#never-use-addresses) |
| `write-barrier` | warning | a managed reference stored by raw pointer arithmetic or through a reference-type `BNM::Field` | [hooks.md](hooks.md#write-barrier) |
| `exceptions` | warning | a `throw` inside a hook | [hooks.md](hooks.md#exceptions) |
| `managed-global` | error / warning | an object the mod created kept in a global or `static` without a GC handle (error); a game object kept the same way (warning) | [hooks.md](hooks.md#keeping-objects) |
| `enum-range` | warning | a literal written into an enum field that is not one of the enum's values | [card-dsl.md](card-dsl.md#the-alias-shortcut) |
| `pool-bypass` | warning | creating an object of a `Poolable` type directly | [card-dsl.md](card-dsl.md#pooled-objects) |
| `custom-class` | error | an injected class with a base or interface not in `dump.cs`, a struct smaller than its base, custom fields over inherited ones, a missing or mismatched interface method | [bnm-classes.md](bnm-classes.md) |
| `compile` | error | a compiler error in your code, found by the compiler checks | fix the first one first |
| `suppression` | error | a malformed `sprig-lint` comment | below |
| `lint-internal` | warning | a file lint could not read, or a check that crashed on it | [below](#when-lint-itself-fails) |

When `dump.cs` is missing (as in CI), the rules that need it are skipped with a note.

## Suppressing a finding

When a finding is wrong for your code, silence that rule on that line, with a reason:

```cpp
cache = provider;  // sprig-lint: allow managed-global the game keeps this singleton alive
```

The comment can also sit on its own line, directly above the line it covers. A suppression without a reason is itself an error.

## When lint itself fails

- `warning [lint-internal]`: lint could not read that file, or one check crashed on it. The other
  checks still ran and the build continues. It is not a finding about your code: mention it in your
  report and carry on.
- `lint failed: ...` (exit 2): lint could not run at all, and `build_mod.ps1` stops. Build with
  `.\work\scripts\build_mod.ps1 -NoLint` and include the message in your report.
