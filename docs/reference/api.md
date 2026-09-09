# PvZH API quick reference

This reference targets Sprig's bundled APK and was distilled from PvZH-Decomp commit `dda825d568c6a5c866b5eaec0ea26bdbdf94f1de`.

## Localization

`PvZCards.Utilities.LocalizationMapper` is the safest first area to modify.

- `TranslateString(string)` — historical RVA `0x1EC27C8`; central game-text lookup.
- `LoadLocalizedStringsFromLanguageCode(string)` — `0x1EC2944`; language load/merge point.
- `Reload()` — `0x1EC2904`; reapplies localization data.
- `LocalizedStrings` — historical field offset `0x20`; active string dictionary.

## Effects and battles

`PvZCards.Engine.EffectRouter` dispatches runtime effects.

- `ProcessEffect(Effect)` — `0x227E240`; central effect execution hook.
- `RegisterForEffect(Type, Action<Effect>)` — `0x227E514`; prefer registration when observation is enough.
- `RegisterForAllEffects(Action<Effect>)` — `0x227E62C`; observe every effect without replacing dispatch.

Other battle starting points: `PvZCards.Game.InitializeGameStateCommand`, `PvZCards.Game.GameStateUpdatedSignal`, `PvZCards.PVP.PvpInitializeGameStateCommand`, `PvZCards.Common.PlayNextBattleSignal`, and `PvZCards.Core.Systems.TriggerSystem<T>`.

## Cards

`PvZCards.Core.Utilities.CardDataLoaderAndMapper.LoadAndMapAllCards(string)` — `0x21D0E30`; receives the full card JSON before mapping.

`PvZCards.Core.Utilities.CardDataMapper`:

- `SetCardData(int, CardData)` — `0x21C8694`; intercept one card entering the runtime store.
- `GetCardData(int)` — `0x21C86FC`; intercept a card lookup.
- `GetAllCards(List<CardData>)` — `0x21C87E8`.
- `CardDataMap` — historical field offset `0x10`.

`PvZCards.Core.Utilities.CardDatabase`:

- `MapCardData(Dictionary<string, object>)` — `0x21C80C4`; maps one card's entity data.
- `CreateEntity(int, EntityModel, InstanceManager)` — `0x21C81E4`; creates an in-match entity.
- `GetBackingEntityByGuid(int)` — `0x21C834C`.

Use [cards.csv](cards.csv) for known card IDs and display data.

## Serialization and card abilities

PvZH card abilities are data-driven. JSON objects identify components and queries through `$type`.

- `PvZCards.Core.Serialization.SerializedObjectCreator.CreateObject(Dictionary<string, object>)` — `0x222263C`; resolves `$type` to an IL2CPP object.
- `PvZCards.Core.Application.SerializationBinder.BindSerializers(IInjector)` — `0x223EDEC`; boot-time registration point for custom serializers.

Components are under `PvZCards.Engine.Components`; queries are under `PvZCards.Engine.Queries`. Search [types.csv](types.csv), then verify the selected class in your dump.

## Assets

`PvZCards.Common.AssetBundleManager`:

- `LoadAssetAsync<T>(string, Action<T>)` — historical shared-generic RVA `0x2367330`; central asynchronous path.
- `LoadAssetBlocking<T>(string)` — `0x236771C`; synchronous path.

`PvZCards.Common.AssetBundlePathProvider`:

- `AssetPathToBundleName(string)` — `0x1E5AAE0`; redirects an asset path to another bundle.
- `AssetPathToUsableAssetName(string)` — `0x1E5ABF4`; changes the name inside a bundle.

Generic methods can have separate native instantiations. Confirm the closed type used by your target call.

## Heroes and inventory

Use [heroes.md](heroes.md) for hero IDs, factions, and classes. Useful discovery points include `PvZCards.Game.JsonHeroDataService`, `PvZCards.Game.HeroDataConverter`, `PvZCards.HeroSelect.InventoryBasedUnlockedHeroesSelector`, `PvZCards.Inventory.PlayerInventoryHolder`, `PvZCards.Inventory.PlayerInventoryHolderImpl`, `PvZCards.Inventory.PlayerInventoryModifiedSignal`, and `PvZCards.Identity.SyncPlayerInventorySignal`.

Inventory values may be synchronized or server-owned. Start by observing reads and signals rather than changing persisted values.

## BNM essentials

```cpp
auto type = BNM::Class("PvZCards.Engine", "EffectRouter");
auto method = type.GetMethod("ProcessEffect", 1);
BNM::BasicHook(method, replacement, original);

auto field = type.GetField("EffectProcessorMap");
// field.Get<T>(instance) / field.Set<T>(instance, value)
```

Register hooks only inside `OnIl2CppLoaded`. For virtual calls or stripped/inlined methods, inspect the pinned BNM headers before choosing a lower-level Dobby hook.
