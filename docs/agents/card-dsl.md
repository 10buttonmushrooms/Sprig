# The card DSL

> **Rules**
> - A new `$type` is a new IL2CPP class your module creates with BNM ([bnm-classes.md](bnm-classes.md)).
> - It implements the interfaces of its role, always including `Poolable` and `DeepCopyable`.
> - Look up every engine type with `.\sprig.ps1 find`; learn each role from `sprig find --derives <Role>`.
> - Poolable objects come from `InstanceManager.Acquire`, not from creating them directly.
> - Do not fake a new `$type` with a stock object marked by sentinel values (see [the alias shortcut](#the-alias-shortcut)).

Each statement about the engine below says where it comes from: **dump.cs** (visible in
`work/dump/dump.cs`), **disassembly** (read from the game's code on 2026-10-20), **BNM source**
(`deps/BNM-Android`), or **unverified** (not yet checked on a device; treat it as a hypothesis and
log to confirm it).

## Cards are components

A card is an entity with a list of components. In card JSON each component is an object whose
`$type` names its class and whose `$data` holds its fields (`ReferenceTypeSerializer.TYPE_KEY` and
`DATA_KEY`, **dump.cs**).

## How `$type` becomes an object

`ReferenceTypeDeserializer` holds a `SerializedObjectCreator` (**dump.cs**).
`SerializedObjectCreator.CreateObject(Dictionary<string, object>)` does this (**disassembly**):

```text
type = SerializedTypeGetter.GetTypeFromSerializedState(serializedObject)   // reads "$type"
       -> GetTypeFromTypeString(string) -> TypeCache.GetAndAddTypeFromString(string)
       -> IL2CPP's Type.GetType lookup by name; no match throws TypeNotFoundException
if (IsPoolable(type)) return Instances.Acquire(type, true);   // InstanceManager, pooled
else                  return Activator.CreateInstance(type);
```

So a `$type` must be a name IL2CPP's by-name class lookup can resolve. The pinned BNM hooks that
lookup (`Image::ClassFromName`, `Image::GetTypes`, `Assembly::GetAllAssemblies`) so the classes it
creates can be found (**BNM source**, `src/Loading.cpp`). A class created with
`CompileTimeClassBuilder("MyNs", "MyType", "MyImage")` lives in an assembly named after `MyImage`
(**BNM source**, `src/ClassesManagement.cpp`), so cards would refer to it as
`"$type": "MyNs.MyType, MyImage"` (**unverified**). What happens to a card whose `$type` throws is
**unverified**: expect the card (or the whole card load) to fail, and check logcat.

## Roles

Every role below is an interface or class in `dump.cs`. Run `sprig find` on it for the exact
methods, and `sprig find --derives` for every vanilla example.

| Role | Implement | Methods to provide |
| --- | --- | --- |
| Component (any) | `PvZCards.Engine.Component` (interface: `Poolable`, `DeepCopyable`) | `HandleReturnToPool(InstanceManager)`, `DeepCopyFrom(object source, InstanceManager)` |
| Marker component | derive from `PvZCards.Engine.Components.MarkerComponent` (class, no fields) | inherited |
| Trigger | `MarkerComponent` + `PvZCards.Engine.Trigger` (empty interface) | inherited |
| Query | `PvZCards.Engine.EntityQuery` (interface) | `bool Matches(Entity entity, EntityQueryContext context)` + the two above |
| Condition | `PvZCards.Engine.Components.Condition` (marker interface) | evaluated by a `ConditionEvaluator` (below) |
| Effect descriptor | `PvZCards.Engine.Components.EffectDescriptor` (interface) | `Effect ToEffect(InstanceManager instances, Entity effectEntity)` + the two above |
| Removable effect descriptor | `RemovableEffectDescriptor` | also `Effect ToRemovalEffect(InstanceManager, Entity)` |
| Effect | derive from `PvZCards.Engine.NonBlockingEffect` or `BlockingEffect` (abstract `Effect`, instance size 0x48) | `Resolve()`, `IsResolved()`; `DeepCopyFrom`, `HandleReturnToPool` |

All of the above is **dump.cs**. How the engine uses each role:

- **Queries** are held by filters (`EntityFilter.Query`, **dump.cs**) and asked `Matches` for each
  candidate entity (**unverified**).
- **Conditions** are evaluated by `ConditionEvaluator.Evaluate(Condition, Entity, EntityQueryContext)`
  implementations; `ConditionEvaluatorBinder.AddBindings` registers them (**dump.cs**). How a new
  condition type gets its evaluator registered is **unverified**: find out before relying on it.
- **Effect descriptors** turn into effects through `ToEffect` (**dump.cs**). Returning an existing
  engine effect (for example a `HealEffect` with your values) lets the engine's own code process it.
  A brand-new effect class also needs whatever processes effects of its type (**unverified**).
- **Triggers** are marker components on an ability. Which engine event fires a given trigger is
  **unverified**: log before relying on it.

## Finding out how the engine uses something

When a guide says **unverified**, confirm it on a device before building on it. Hook the method you
believe is involved, log, and read `adb logcat -s Sprig`:

```cpp
// Logs every AttackComparisonQuery the engine evaluates. Remove it once you know.
using MatchesFn = bool (*)(void *self, void *entity, void *context, const void *method);
MatchesFn originalMatches = nullptr;

bool LogMatches(void *self, void *entity, void *context, const void *method) {
    LOG_INFO("AttackComparisonQuery.Matches(%p)", entity);
    return originalMatches(self, entity, context, method);
}
```

## Pooled objects

`SerializedObjectCreator.CreateObject` acquires poolable types from its `InstanceManager` instead of
constructing them (**disassembly**). The pool's API (**dump.cs**):

```text
PvZCards.Engine.InstanceManager
  [slot 4] virtual Poolable Acquire(Type type, bool shouldInject)
  [slot 7] virtual void Return(Poolable instance)
```

Methods such as `EffectDescriptor.ToEffect(InstanceManager instances, ...)` receive the instance
manager: acquire the objects you return from it. Creating a poolable object with
`CreateNewObjectParameters` gives an object the pool never saw (`sprig lint` warns: `pool-bypass`).
Your own classes must reset their fields in `HandleReturnToPool` and copy them in `DeepCopyFrom`,
because the engine reuses and copies them.

## The alias shortcut

A tempting way to "add" a `$type` without creating a class: hook `CreateObject`, and for your new
name return a stock object (say an `AttackComparisonQuery`) marked with a sentinel value, then hook
the stock class's `Matches` to special-case the sentinel. It compiles, but:

- it has no fields or state of its own;
- every system that reads the stock fields sees the sentinel, for example an enum set to a value
  the game never produces (`sprig lint` warns: `enum-range`);
- it usually creates poolable objects outside the pool (`pool-bypass`);
- every instance of the stock class pays for the extra hook.

Create a real class instead ([bnm-classes.md](bnm-classes.md)).

## Using a new type in a card

Cards come from the game's card data. `CardDataLoaderAndMapper.LoadAndMapAllCards(string)` receives
the whole card JSON before it is mapped (`docs/reference/api.md`): a module can hook it and edit
the JSON (add a card, or add your component to an existing one) before calling the original.
Keep the JSON valid; a broken card list can stop the game from loading (**unverified**).

## Checklist for a new kind

1. `sprig find <Role>` and `sprig find --derives <Role>`: pick the interface and read two vanilla examples.
2. Write the class with BNM ([bnm-classes.md](bnm-classes.md)), implementing every interface method.
3. `.\sprig.ps1 lint <Name>`: it checks the layout and that each method matches its interface.
4. Hook `LoadAndMapAllCards` (or use a card that already exists) to put the new `$type` on a card.
5. On a device, log from your methods to confirm the engine calls them. Report what you saw.
