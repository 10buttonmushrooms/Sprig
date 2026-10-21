# Creating IL2CPP classes with BNM

> **Rules**
> - Describe the class with `BNM_CustomClass` inside the C++ struct, then one `BNM_CustomField` per
>   field and one `BNM_CustomMethod` per method the engine calls.
> - The struct starts with its base class's layout: derive from `BNM::IL2CPP::Il2CppObject` (the
>   0x10 object header), plus padding up to the base's instance size if the base has fields.
> - Implement every method of every interface you list, with the same name and parameter count.
> - Name types with `BNM::CompileTimeClassBuilder("Namespace", "Name").Build()`; check each with `sprig find`.

This is how a module adds a new card-DSL type ([card-dsl.md](card-dsl.md)). Everything here is from
the pinned BNM's headers (`deps/BNM-Android/include/BNM/ClassesManagement.hpp`, `Class.hpp`) unless
marked otherwise.

## The macros

```cpp
BNM_CustomClass(CppStruct, targetType, baseType, ownerType, interfaces...);
BNM_CustomField(member, fieldType, "FieldName");
BNM_CustomMethod(memberFunction, isStatic, returnType, "MethodName", parameterTypes...);
```

- `targetType`: the new class, `BNM::CompileTimeClassBuilder("MyNs", "MyType", "MyImage").Build()`.
  BNM creates an assembly named after `MyImage` if none exists.
- `baseType`: the class to derive from; `System.Object` for roles that are only interfaces.
- `ownerType`: `{}` for a top-level class (or the outer class for a nested one).
- `interfaces...`: one `CompileTimeClassBuilder(...).Build()` per interface.
- Types for fields, returns and parameters: `BNM::Defaults::Get<int>()`, `Get<bool>()`, `Get<void>()`,
  `Get<BNM::IL2CPP::Il2CppObject *>()` (any object), or a `CompileTimeClassBuilder` for game types.

`BNM_CustomClass` registers the class while the library loads; BNM creates it when IL2CPP starts.
To add a class later, `BNM::ClassesManagement::ProcessClassRuntime` must run on IL2CPP's main thread.

## Base classes

The base and every interface must exist in `dump.cs`: `sprig find <Name>` before using it, and
`sprig lint` reports any that do not. An interface goes in the interface list, never as the base.

## Layout

IL2CPP objects start with a 0x10 header (class pointer and monitor). Then come the base class's
fields, then yours. `sprig find <Base>` prints the base's instance size and field offsets.

- Base `System.Object` (instance size 0x10): derive from `BNM::IL2CPP::Il2CppObject`; your first field
  sits at 0x10.
- A base with fields, for example `NonBlockingEffect` (instance size 0x48): reserve its fields first.

```cpp
struct EffectLayout : BNM::IL2CPP::Il2CppObject {
    uint8_t inherited[0x48 - 0x10];   // NonBlockingEffect's fields: sprig find NonBlockingEffect
};
struct MyEffect : EffectLayout {
    int Amount;                         // first own field, at 0x48
    // BNM_CustomClass(MyEffect, ..., BNM::CompileTimeClassBuilder("PvZCards.Engine", "NonBlockingEffect").Build(), ...)
};
```

`sprig lint` compiles a check that the struct is at least the base's size and that every
`BNM_CustomField` comes after the inherited fields.

## Overriding

For each method the engine calls on your type (every interface method, every abstract method of
the base), add a member function and a `BNM_CustomMethod` with the same name and the same number of
parameters. The member function takes the C# parameters only: no `self` (that is `this`) and no
`MethodInfo`. Use `void *` for game objects.

| Interface method (dump.cs) | Member function |
| --- | --- |
| `bool Matches(Entity entity, EntityQueryContext context)` | `bool Matches(void *entity, void *context)` |
| `void HandleReturnToPool(InstanceManager instanceManager)` | `void HandleReturnToPool(void *instanceManager)` |
| `void DeepCopyFrom(object source, InstanceManager instances)` | `void DeepCopyFrom(void *source, void *instances)` |
| `Effect ToEffect(InstanceManager instances, Entity effectEntity)` | `void *ToEffect(void *instances, void *effectEntity)` |

`sprig lint` reports a missing interface method, a wrong parameter count and a `static` override.

## Example: a new query

It compiles and passes `sprig lint`; it has not been tested on a device yet. Cards would use it as
`{"$type": "SprigMods.Queries.EvenIdQuery, SprigMods", "$data": {"Divisor": 2}}`.

```cpp
#include <bloom/bloom.h>

#include <BNM/Class.hpp>
#include <BNM/ClassesManagement.hpp>
#include <BNM/Defaults.hpp>
#include <BNM/Method.hpp>

#include "log.h"

namespace {

BNM::Method<int> getId;  // PvZCards.Engine.Entity.get_Id, looked up in Init

// Matches entities whose Id is a multiple of Divisor (2 when unset). EntityQuery is an interface,
// so the class derives from System.Object: fields start at 0x10, after the header.
struct EvenIdQuery : BNM::IL2CPP::Il2CppObject {
    int Divisor;

    bool Matches(void *entity, void *context) {
        if (!entity || !getId.IsValid()) return false;
        const int divisor = Divisor > 0 ? Divisor : 2;
        return getId[entity]() % divisor == 0;
    }
    void HandleReturnToPool(void *instanceManager) { Divisor = 0; }
    void DeepCopyFrom(void *source, void *instanceManager) {
        if (source) Divisor = static_cast<EvenIdQuery *>(source)->Divisor;
    }

    BNM_CustomClass(EvenIdQuery,
                    BNM::CompileTimeClassBuilder("SprigMods.Queries", "EvenIdQuery", "SprigMods").Build(),
                    BNM::CompileTimeClassBuilder("System", "Object").Build(),
                    {},
                    BNM::CompileTimeClassBuilder("PvZCards.Engine", "EntityQuery").Build(),
                    BNM::CompileTimeClassBuilder("PvZCards.Engine", "Poolable").Build(),
                    BNM::CompileTimeClassBuilder("PvZCards.Engine", "DeepCopyable").Build());
    BNM_CustomField(Divisor, BNM::Defaults::Get<int>(), "Divisor");
    BNM_CustomMethod(Matches, false, BNM::Defaults::Get<bool>(), "Matches",
                     BNM::CompileTimeClassBuilder("PvZCards.Engine", "Entity").Build(),
                     BNM::CompileTimeClassBuilder("PvZCards.Engine", "EntityQueryContext").Build());
    BNM_CustomMethod(HandleReturnToPool, false, BNM::Defaults::Get<void>(), "HandleReturnToPool",
                     BNM::CompileTimeClassBuilder("PvZCards.Engine", "InstanceManager").Build());
    BNM_CustomMethod(DeepCopyFrom, false, BNM::Defaults::Get<void>(), "DeepCopyFrom",
                     BNM::Defaults::Get<BNM::IL2CPP::Il2CppObject *>(),
                     BNM::CompileTimeClassBuilder("PvZCards.Engine", "InstanceManager").Build());
};

}  // namespace

static void Init() {
    if (!BLOOM_CONFIG_BOOL("General", "Enabled", true)) return;
    getId = BNM::Class("PvZCards.Engine", "Entity").GetMethod("get_Id", 0);
    if (!getId.IsValid()) LOG_ERR("EvenIdQuery: Entity.get_Id was not found");
}

BLOOM_REGISTER_PNP(Init);
```

To put it on a card, see [card-dsl.md](card-dsl.md#using-a-new-type-in-a-card). Then confirm on a
device that the engine calls `Matches` (log from it) before building more on it.
