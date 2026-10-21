# Working on Sprig

Sprig builds native C++ mods for Plants vs. Zombies Heroes 1.65.27 (Android, IL2CPP) into the
game's APK. A mod is a Bloom module in `Bloom/<Name>/` ([Bloom/README.md](Bloom/README.md)).
Everything you need is in this repository and in `work/dump/dump.cs`. Read this file first.

## The card DSL

Cards are JSON: lists of components written as `{"$type": "...", "$data": {...}}`. The game turns
each `$type` into an object of that IL2CPP class. A new component, trigger, query, condition,
effect descriptor or effect is a **new IL2CPP class that your module creates with BNM**. That is
the card DSL in this repository; there is no other one to find, so do not search outside it.

| Task | Read |
| --- | --- |
| A new `$type` (component, trigger, query, condition, effect) | [docs/agents/card-dsl.md](docs/agents/card-dsl.md), then [docs/agents/bnm-classes.md](docs/agents/bnm-classes.md) |
| Change what a game method does | [docs/agents/hooks.md](docs/agents/hooks.md) |
| A crash, or a mod that does nothing | [docs/agents/debugging.md](docs/agents/debugging.md) |
| A lint finding | [docs/agents/lint.md](docs/agents/lint.md) |

## The loop

1. Read the guide for your task.
2. Look up every game class, method and field you use (next section). Never guess.
3. Copy `Bloom/_templates/PnP` to `Bloom/<Name>` (letters, digits, `_`, `-`; starts with a letter).
4. Write the code in `Bloom/<Name>/main.cpp` (and `src/`, `inc/` if it grows).
5. Run `.\sprig.ps1 lint <Name>` until it prints `lint: clean`.
6. Build with `.\work\scripts\build_mod.ps1`. It runs lint first. Fix the first error first.
7. With a device attached: `.\work\scripts\all.ps1`, launch the game, read `adb logcat -s Sprig`.
   Without one, say plainly that the mod is built but untested.

## Look up every name

`work/dump/dump.cs` lists every game class (28 MB: do not read or grep it directly). Ask:

```powershell
.\sprig.ps1 find HealEffect                  # fields with offsets, methods, base types, the BNM line
.\sprig.ps1 find EntityQuery.Matches         # exact C# signature and the hook signature to copy
.\sprig.ps1 find --derives EntityQuery       # every vanilla type of that role: learn from them
.\sprig.ps1 find --rva 0x228C470             # which method contains a crash address
```

A wrong name prints `not in dump.cs` and the closest real names. Use those; do not invent one.
From cmd use `.\sprig.cmd`, from Git Bash `./sprig.cmd`.

## Rules

Breaking these crashes the game or makes a mod silently do nothing. `sprig lint` checks them.

- Use BNM only inside `Init` (or code it calls). Never at file scope or in static initializers.
- Check `IsValid()` on every class, method and field you look up; log with `LOG_ERR` and return.
- Write hook signatures exactly as `sprig find Type.Method` prints them: `self` first for instance
  methods, none for static ones, and always the trailing `const void *method`.
- After `BNM::BasicHook`, check that `original` is not null. Outside the hook, check it before calling.
- Never use an address or offset as a number in code: resolve by name, read offsets with `GetOffset()`.
- Store a managed reference into a game object only through the write barrier (see hooks.md).
- Keep a managed object you create in a global only through a GC handle (see hooks.md).
- Never `throw` from a hook; log and return a safe value instead.
- Change only files inside your module's folder unless the user asked for more. Do not edit
  `README.md`, `docs/`, other modules, `deps/`, `work/patches/` or the build scripts.
- Stay inside this repository: everything you need is here.

## Report

When you finish, say what `sprig lint` printed, whether `build_mod.ps1` built, and whether the mod
was tested on a device (with the `adb logcat -s Sprig` lines). If it was not tested, say so.
