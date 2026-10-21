# Hooks

> **Rules**
> - Get every signature from `.\sprig.ps1 find Type.Method`. Copy its `hook:` line.
> - Instance methods take `void *self` first; static methods do not. Every hook ends with `const void *method`.
> - After `BNM::BasicHook`, check that `original` is not null; log with `LOG_ERR` and return if it is.
> - Look up and hook only inside `Init`.
> - Never `throw` from a hook. Never use an address or offset as a number.

A hook replaces a game method's code with yours. Bloom modules install hooks from their `Init`
function (`BLOOM_REGISTER_PNP(Init)`), which runs once IL2CPP is ready.

## Signatures

IL2CPP compiles every C# method to a C function. Your hook must have exactly its parameters:

1. `void *self` for an instance method (the object), nothing for a static one;
2. the C# parameters, in order;
3. `const void *method`, IL2CPP's `MethodInfo*`, always last.

`sprig find` prints the signature for you:

```text
> .\sprig.ps1 find AttackComparisonQuery.Matches
PvZCards.Engine.Queries.AttackComparisonQuery.Matches  (instance, slot 4, RVA 0x228C460, dump.cs:758858)
  C#:   public bool Matches(Entity entity, EntityQueryContext context)
  BNM:  BNM::Class("PvZCards.Engine.Queries", "AttackComparisonQuery").GetMethod("Matches", 2)
  hook: bool (*)(void *self, void * /* Entity */ entity, void * /* EntityQueryContext */ context, const void *method)
```

```cpp
using MatchesFn = bool (*)(void *self, void *entity, void *context, const void *method);
MatchesFn originalMatches = nullptr;

bool Matches(void *self, void *entity, void *context, const void *method) {
    bool result = originalMatches(self, entity, context, method);
    return result;
}

// In Init:
auto query = BNM::Class("PvZCards.Engine.Queries", "AttackComparisonQuery");
BNM::BasicHook(query.GetMethod("Matches", 2), Matches, originalMatches);
if (!originalMatches) {
    LOG_ERR("MyMod: AttackComparisonQuery.Matches hook failed");
    return;
}
```

| C# type | C++ type in a signature |
| --- | --- |
| `bool`, `int`, `long`, `float`, `double` | `bool`, `int32_t`, `int64_t`, `float`, `double` |
| `byte`, `short`, `char`, `uint`, `ulong` | `uint8_t`, `int16_t`, `char16_t`, `uint32_t`, `uint64_t` |
| an enum | its underlying type (usually `int32_t`); `sprig find` lists the values |
| `string` | `BNM::Structures::Mono::String *` |
| `T[]`, `List<T>`, `Dictionary<K, V>` | `BNM::Structures::Mono::Array<T> *`, `List<T> *`, `Dictionary<K, V> *` |
| any other class or interface | `void *` |
| `out T`, `ref T` | a pointer to the mapped type |
| a value type (struct) | a matching C++ struct, passed by value (see below) |

`float` and `double` travel in different registers from integers and pointers: declaring a float
parameter as `int` (or the reverse) passes garbage. `sprig lint` checks all of this.

### Value types

A C# struct (for example `UnityEngine.Vector3`) is passed by value. Define a C++ struct with the
same fields in the same order; `sprig find Vector3` prints the fields and the struct size. BNM
already has structs for common Unity types (`BNM::Structures::Unity::Vector3` and others).

## Calling the original

- Inside the hook, call `original` with every argument you received, including `method`.
- `BNM::BasicHook` returns nothing. If the target was not found, `original` stays null: check it right
  after hooking.
- Outside the hook, `original` may be null (the hook was not installed): check before calling.

## Checked hooks

`sprig lint` checks a hook's signature when it can tell which method is hooked. Write the target as
a lookup, inline or through a variable assigned in the same function:

```cpp
BNM::BasicHook(BNM::Class("PvZCards.Game", "AssetVersionMap").GetMethod("FindAssociation", 1), Hook, original);

auto method = cls.GetMethod("OnExecute", 0);
BNM::BasicHook(method, Hook, original);
```

A helper that takes `(method, hook, original)` and calls `BNM::BasicHook` with them (like the
`Install` helpers in `Bloom/UnlockAll`) is understood too.

## Overloads

`GetMethod("Name", n)` returns the first method with that name and parameter count. When several
overloads share the count, pick one by parameter names (`sprig find` shows them):

```cpp
users.GetMethod("Tag", {"userTag", "counterDelta"});
```

## When BNM is ready

IL2CPP is ready only when `Init` runs. Code at file scope and static initializers run when the
library loads, long before that: a `BNM::Class(...)` there finds nothing. Declare variables at file
scope (`BNM::Method<int> getId;`) and assign them in `Init`.

## Never use addresses

RVAs and offsets change between game builds. Look methods up by name and read field offsets with
`GetField("Name").GetOffset()`. The RVAs in `docs/reference/api.md` and `sprig find` are for
mapping crash addresses only.

## Fields

```cpp
auto field = cls.GetField("Name");          // check field.IsValid()
BNM::Field<int> health = cls.GetField("Health");
int value = health[object]();               // read
health[object] = 5;                         // write a value-type field
```

A property `X` keeps its value in the field `<X>k__BackingField`. Static fields are read without an
object: `field()`.

## Write barrier

The game runs Unity's incremental garbage collector. Storing a new reference (a pointer to a game
object or string) into a game object must go through IL2CPP's write barrier, or the collector can
miss it and free the object. `Bloom/DownloadAll` does it like this:

```cpp
static void SetReference(void *object, void **field, void *value) {
    using WriteBarrier = void (*)(void *, void **, void *);
    static const auto barrier = [] {
        void *il2cpp = dlopen("libil2cpp.so", RTLD_NOW | RTLD_NOLOAD);
        return il2cpp ? reinterpret_cast<WriteBarrier>(dlsym(il2cpp, "il2cpp_gc_wbarrier_set_field")) : nullptr;
    }();
    if (barrier) barrier(object, field, value);
    else *field = value;
}
```

Value-type fields (`int`, `bool`, `float`, enums) need no barrier.

## Keeping objects

The garbage collector does not see C++ globals. An object your module creates and keeps only in a
global (or a `static`) can be freed while the global still points at it. Either create it when you
need it, or hold a GC handle:

```cpp
using GcHandleNew = uint32_t (*)(void *object, bool pinned);
using GcHandleGet = void *(*)(uint32_t handle);
void *il2cpp = dlopen("libil2cpp.so", RTLD_NOW | RTLD_NOLOAD);
auto gcNew = reinterpret_cast<GcHandleNew>(dlsym(il2cpp, "il2cpp_gchandle_new"));
auto gcGet = reinterpret_cast<GcHandleGet>(dlsym(il2cpp, "il2cpp_gchandle_get_target"));
uint32_t handle = gcNew(object, false);   // keep `handle`, not `object`
void *alive = gcGet(handle);              // when you need the object again
```

Game objects you receive (a hook's `self`, a field you read) stay valid only while the game holds
them. Re-read them when you need them instead of caching them across frames.

## Exceptions

Never `throw` from a hook. Game code only catches managed exceptions, so a C++ exception that
leaves your hook aborts the game. Log the problem and return a safe value or the original's result.
A managed exception raised by game code you call travels to the game's own handlers, as if the
original method had raised it.

## Virtual methods

`BNM::BasicHook` patches one method body. An override in a subclass is a separate body: hook it too
(`sprig find --derives Type` lists the subclasses). `BNM::VirtualHook(cls, method, hook, original)`
replaces the method in one class's vtable instead.

## Hooks that never fire

If a hook installs but never runs, the compiler may have inlined the method into its callers.
Put a `LOG_INFO` in the hook to check, then hook a caller instead.

## Threads

Hooks can run on several game threads. Protect state they share with a `std::mutex`
(see `Bloom/BundleForcer`).
