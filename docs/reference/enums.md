# PvZH enums

188 game enums extracted from the reference APK. Regenerate them for other game versions.

## `PvZCards.ATT` (1 enums)

### `ATTStatus`  *(TypeDefIndex 4752)*

| name | value |
|---|--:|
| `NotDetermined` | 0 |
| `Restricted` | 1 |
| `Denied` | 2 |
| `Authorized` | 3 |

## `PvZCards.Application` (2 enums)

### `InputNotifierKeys`  *(TypeDefIndex 6351)*

| name | value |
|---|--:|
| `InGame` | 0 |

### `WifiStatus`  *(TypeDefIndex 6442)*

| name | value |
|---|--:|
| `Off` | 0 |
| `On` | 1 |

## `PvZCards.AutomationServer` (4 enums)

### `AutomationCheatCommandType`  *(TypeDefIndex 4063)*

| name | value |
|---|--:|
| `PerformanceMetrics` | 0 |
| `PlayAutomatedGame` | 1 |
| `WinGame` | 2 |
| `LoseGame` | 3 |
| `ResetProfile` | 4 |
| `SpeedUpGame` | 5 |
| `ResetGameSpeed` | 6 |
| `EnableAutoPlay` | 7 |
| `DisableAutoPlay` | 8 |
| `EnableAlwaysShowReturnToHqButton` | 9 |
| `JumpToNode` | 10 |
| `DeviceInfo` | 11 |
| `StartContinuousScreenshots` | 12 |
| `StopContinuousScreenshots` | 13 |
| `GameDebugInfo` | 14 |
| `AddCardToInventory` | 15 |
| `AddGems` | 16 |
| `AddHeroesToInventory` | 17 |
| `AllowCashPurchases` | 18 |
| `UseCheatsPurchaseFlow` | 19 |
| `SkipTutorials` | 20 |
| `UnlockAllHeroes` | 21 |
| `SetGamesCompletedAsPlants` | 22 |
| `SetGamesCompletedAsZombies` | 23 |
| `UnlockZombieHq` | 24 |
| `GetRandomSeed` | 25 |
| `ResetGame` | 26 |
| `AddGemQuest` | 27 |
| `UnlockAllSilverCards` | 28 |
| `GetBundlesUsed` | 29 |
| `LimitFps` | 30 |
| `SetLogLevel` | 31 |
| `TurnOffIncentivizedAds` | 32 |
| `ResetDailyBattleTimer` | 33 |
| `GrantMaxTicketsForCurrentWeeklyEvent` | 34 |
| `SetCurrentWeeklyEvent` | 35 |
| `UnlockMulligan` | 36 |

### `AutomationPackOpeningCommandType`  *(TypeDefIndex 4095)*

| name | value |
|---|--:|
| `OneOfEachCard` | 0 |
| `FourOfEachCard` | 1 |
| `FifthOfOneCard` | 2 |
| `OneThirdOfCardsOpenedUseless` | 3 |
| `FourOfAllR1Cards` | 4 |
| `FourOfAllR2Cards` | 5 |
| `FourOfAllR3Cards` | 6 |
| `FourOfAllR4Cards` | 7 |
| `RarityRatesR1` | 8 |
| `RarityRatesR2` | 9 |
| `RarityRatesR3` | 10 |
| `RarityRatesR4` | 11 |
| `AllHeroes` | 12 |

### `AutomationRequestType`  *(TypeDefIndex 4132)*

| name | value |
|---|--:|
| `None` | 0 |
| `Tap` | 1 |
| `Scroll` | 2 |
| `DragAndDrop` | 3 |
| `GetMessageHierarchy` | 4 |
| `KeepAlive` | 5 |
| `Ping` | 6 |
| `Quit` | 7 |
| `Screenshot` | 8 |
| `Cheat` | 9 |
| `Error` | 10 |
| `GetLocalizedText` | 11 |
| `GetProgressionConfiguration` | 12 |
| `GetUnlockedHeroes` | 13 |
| `StartGame` | 14 |
| `GetOffers` | 15 |
| `GetAllObjects` | 16 |
| `GetCardList` | 17 |
| `GetUserId` | 18 |
| `GetPackOpeningStats` | 19 |
| `GetLoadTimeList` | 20 |
| `GetCurrentWeeklyEventInfo` | 21 |

### `TimingStep`  *(TypeDefIndex 4152)*

| name | value |
|---|--:|
| `AppStart` | 0 |
| `FirstCommand` | 1 |
| `InitScreen` | 2 |
| `AgeGate` | 3 |
| `Prologue` | 4 |
| `GameStart` | 5 |
| `GameEnd` | 6 |
| `PostGameScreen` | 7 |
| `LandingScreen` | 8 |
| `ProgressionScreen` | 9 |
| `PackOpening` | 10 |
| `DeckEditor` | 11 |
| `QuestScreen` | 12 |

## `PvZCards.Cheats` (1 enums)

### `PerformanceRunType`  *(TypeDefIndex 3955)*

| name | value |
|---|--:|
| `None` | 0 |
| `All` | 1 |
| `Fps` | 2 |
| `Memory` | 3 |

## `PvZCards.Common` (11 enums)

### `Achievement`  *(TypeDefIndex 4309)*

| name | value |
|---|--:|
| `OutOfTheShadows` | 0 |
| `SuperCoiffure` | 1 |
| `BackToTheGrave` | 2 |
| `MasterMulcher` | 3 |
| `VegetationDomination` | 4 |
| `FiendishFeat` | 5 |
| `MeVsYou` | 6 |
| `Winning` | 7 |
| `SoMuchDamage` | 8 |
| `BarrelingAlong` | 9 |

### `BlockAllInteractionStatus`  *(TypeDefIndex 4400)*

| name | value |
|---|--:|
| `BlockInteraction` | 0 |
| `AllowInteraction` | 1 |

### `CopernicusTables`  *(TypeDefIndex 4422)*

| name | value |
|---|--:|
| `Patch` | 0 |
| `HockeyAppErrors` | 1 |
| `Gameplay_PvP` | 2 |
| `Gameplay_PvE` | 3 |
| `PvP_Match_Progress` | 4 |
| `Crafting` | 5 |
| `Funnel` | 6 |
| `GameCurrency` | 7 |
| `ErrorLogging` | 8 |
| `SessionStart` | 9 |
| `Social` | 10 |
| `UIBehavior` | 11 |
| `PvpBackgroundEvent` | 12 |
| `gameplay_pvp_start` | 13 |
| `Ecomm` | 14 |
| `Quests` | 15 |
| `Transition` | 16 |
| `DeckEditor` | 17 |
| `EA2_AD` | 18 |
| `CardRedraw` | 19 |
| `Interstitial` | 20 |

### `ExclusionTarget`  *(TypeDefIndex 4445)*

| name | value |
|---|--:|
| `Windows` | 0 |
| `IOS` | 1 |
| `Android` | 2 |

### `GatsFeatureToggleKeys`  *(TypeDefIndex 4458)*

| name | value |
|---|--:|
| `None` | 0 |
| `DraperInterstitials` | 1 |
| `GetActiveQuests` | 2 |
| `FeaturedStoreOffers` | 3 |
| `AllStoreOffers` | 4 |
| `NewPlayersMustMakeFactionSelectionInPvP` | 5 |
| `NewPlayersInitiallySelectZombieFactionInPvP` | 6 |
| `IncentivizedAds2` | 7 |
| `StoreVideoAds` | 8 |
| `ScheduledEvents` | 9 |
| `ServerAuthoritativePvp` | 10 |
| `PackOpeningGroupByPack` | 11 |
| `FancyDeckSelectorFlippyTappy` | 12 |
| `MonitorMemory` | 13 |
| `SkipDeckEditorFtue` | 14 |
| `HeroPurchase` | 15 |
| `HideFutureEventCards` | 16 |
| `ShowFullPveProgressionCounter` | 17 |
| `TurboButton` | 18 |
| `BattleOfTheDay` | 19 |
| `HeroLevels` | 20 |
| `Inbox` | 21 |
| `InboxBottomBar` | 22 |
| `LogglyLevelVerbose` | 23 |
| `LogglyLevelDebug` | 24 |
| `LogglyLevelWarning` | 25 |
| `LogglyLevelError` | 26 |
| `LogglyLevelAssert` | 27 |
| `LogglyLevelException` | 28 |
| `PvpSeasonComplete` | 29 |
| `OfflineForMaintenance` | 30 |
| `SkipDailyBattleRetryTimerOnAdClose` | 31 |
| `SeasonResetCountdownTimer` | 32 |
| `RandomBattles` | 33 |
| `PackOpeningDeckRecipes` | 34 |
| `ReportIncentivizedAdDiagnosticInfo` | 35 |
| `Credits` | 36 |

### `GatsPlatforms`  *(TypeDefIndex 4483)*

| name | value |
|---|--:|
| `android` | 0 |
| `ios` | 1 |

### `NarrativeEvent`  *(TypeDefIndex 4535)*

| name | value |
|---|--:|
| `Start` | 0 |
| `GracePeriodComplete` | 1 |
| `Tap` | 2 |
| `TapToSkipDelayComplete` | 3 |
| `AnimationFinished` | 4 |
| `AllowDismiss` | 5 |

### `NarrativeState`  *(TypeDefIndex 4534)*

| name | value |
|---|--:|
| `Initialized` | 0 |
| `Started` | 1 |
| `CanTapToSkip` | 2 |
| `WaitingOnSkipDelay` | 3 |
| `TapToComplete` | 4 |
| `Complete` | 5 |

### `NavigationType`  *(TypeDefIndex 4543)*

| name | value |
|---|--:|
| `ZombieHq` | 0 |
| `PlantHq` | 1 |

### `ScreenLoadType`  *(TypeDefIndex 4601)*

| name | value |
|---|--:|
| `OverlayAsync` | 0 |
| `OverlayBlocking` | 1 |
| `ReplaceAsync` | 2 |
| `ReplaceAndMaintainAsync` | 3 |
| `PopAllAndShow` | 4 |

### `ShakeScreenType`  *(TypeDefIndex 4658)*

| name | value |
|---|--:|
| `Soft` | 0 |
| `Medium` | 1 |
| `Hard` | 2 |

## `PvZCards.CopernicusEvents` (13 enums)

### `AdType`  *(TypeDefIndex 3895)*

| name | value |
|---|--:|
| `Ea2` | 0 |
| `Offerwall` | 1 |

### `CopernicusFunnelEventsVersionThree`  *(TypeDefIndex 3783)*

| name | value |
|---|--:|
| `NoEventsCompleted` | 0 |
| `GameStarts` | 1 |
| `AgeGatePop` | 200 |
| `AgeGateClose` | 300 |
| `ReturningPlayer` | 350 |
| `LoadProgress` | 360 |
| `LoginSuccess` | 370 |
| `ChooseProfile` | 380 |
| `FirstComicStart` | 400 |
| `FirstComicClose` | 500 |
| `PlaceFirstPeashooterHitPlay` | 600 |
| `PlaceSecondTurnHitPlay` | 700 |
| `SecondComicStart` | 800 |
| `SecondComicClose` | 900 |
| `PrecisionBlast` | 950 |
| `Growshroom` | 1000 |
| `Fertilizer` | 1050 |
| `PlantsWin` | 1075 |
| `OpenPacks1` | 1100 |
| `TeamEditor1` | 1150 |
| `AddPlantCards1` | 1200 |
| `StartSecondLevel` | 1250 |
| `SuperblockExplanation` | 1300 |
| `Teamup` | 1350 |
| `OpenPacks2` | 1400 |
| `TeamEditor2` | 1450 |
| `AddPlantCards2` | 1500 |
| `StartThirdLevel` | 1550 |
| `ZombieTricks` | 1600 |
| `TurnExplanation` | 1650 |
| `HighGroundExplanation` | 1700 |
| `AquaticExplanation` | 1750 |
| `Turn5Starts` | 1800 |
| `OpenPacks3` | 1850 |
| `TeamEditor3` | 1900 |
| `AddPlantCards3` | 1950 |
| `RemovePeashooter` | 2000 |
| `AddPeashooter` | 2050 |
| `ThirdComicStart` | 2100 |
| `ThirdComicClose` | 2150 |
| `LandingScreenAppears` | 2200 |
| `UnlockPlantHero2` | 3200 |
| `UnlockZombieHq` | 3400 |
| `UnlockMulligan` | 3500 |
| `UnlockZombieHero2` | 3600 |
| `UnlockPlantHero3` | 3700 |
| `UnlockZombieHero3` | 3800 |
| `UsernamePrompt` | 4000 |
| `UsernameInput` | 4100 |
| `StartPvP` | 4200 |

### `CopernicusGameCurrencySource`  *(TypeDefIndex 3785)*

| name | value |
|---|--:|
| `PvEWin` | 0 |
| `PvELoss` | 1 |
| `PvPCasualWin` | 2 |
| `PvPCasualLoss` | 3 |
| `PvPRankedWin` | 4 |
| `PvPRankedLoss` | 5 |
| `WonGame` | 6 |
| `Purchase` | 7 |
| `OpenedPack` | 8 |
| `QuestComplete` | 9 |
| `PvpRankUp` | 10 |
| `PvpTierUp` | 11 |
| `Ea2` | 12 |
| `Event` | 13 |
| `DeckRecipe` | 14 |
| `BotD` | 15 |

### `CopernicusGameCurrencyType`  *(TypeDefIndex 3786)*

| name | value |
|---|--:|
| `Gem` | 0 |
| `Sku` | 1 |
| `Pack` | 2 |
| `Card` | 3 |
| `Hero` | 4 |
| `Silver` | 5 |
| `Sparks` | 6 |
| `EventPoints` | 7 |
| `Currency` | 8 |

### `CopernicusGameplayEndOfGameState`  *(TypeDefIndex 3788)*

| name | value |
|---|--:|
| `Won` | 0 |
| `PlayerConceded` | 1 |
| `Lost` | 2 |
| `ServerError` | 3 |
| `NetworkError` | 4 |
| `OpponentConceded` | 5 |
| `OpponentDisconnected` | 6 |
| `QuitApp` | 7 |
| `PlayerInactivity` | 8 |

### `CopernicusQuestEventType`  *(TypeDefIndex 3913)*

| name | value |
|---|--:|
| `Invalid` | 0 |
| `Issued` | 1 |
| `Completed` | 2 |
| `Refreshed` | 3 |

### `CopernicusSessionStartLaunchType`  *(TypeDefIndex 3887)*

| name | value |
|---|--:|
| `Standard` | 0 |
| `Upgrade` | 1 |
| `Install` | 2 |
| `Foreground` | 3 |
| `PushNote` | 4 |
| `LocalNote` | 5 |

### `CraftingSource`  *(TypeDefIndex 3779)*

| name | value |
|---|--:|
| `Card` | 0 |
| `Quest` | 1 |

### `PvPMatchProgressMetricsEvent`  *(TypeDefIndex 3847)*

| name | value |
|---|--:|
| `MatchMakingStart` | 0 |
| `MatchMakingCancel` | 1 |
| `MatchPollingStart` | 2 |
| `MatchPollingError` | 3 |
| `CreateChallengeStart` | 4 |
| `CreateChallengeError` | 5 |
| `MatchMade` | 6 |
| `MatchMakingError` | 7 |
| `ImHereStart` | 8 |
| `ImHereError` | 9 |
| `InitGameStart` | 10 |
| `InitGameError` | 11 |
| `PreGame` | 12 |
| `InGame` | 13 |
| `SentInvalidPlay` | 14 |

### `Status`  *(TypeDefIndex 3894)*

| name | value |
|---|--:|
| `Viewed` | 0 |
| `Clicked` | 1 |
| `Cancelled` | 2 |
| `RewardRecieved` | 3 |
| `Completed` | 4 |

### `UIBehaviorEntrySource`  *(TypeDefIndex 3827)*

| name | value |
|---|--:|
| `Unknown` | 0 |
| `LandingScreen` | 1 |
| `ZombieHq` | 2 |
| `PlantHq` | 3 |
| `WinScreen` | 4 |
| `LossScreen` | 5 |
| `PostGameNextGameScreen` | 6 |
| `Almanac` | 7 |
| `HeroSelect` | 8 |
| `ReturningPlayer` | 9 |
| `Settings` | 10 |
| `FriendsList` | 11 |
| `GameCenterLogin` | 12 |
| `Collection` | 13 |
| `PvE` | 14 |
| `PvP` | 15 |
| `Store` | 16 |
| `PackOpening` | 17 |
| `BotD` | 18 |

### `UIBehaviorTransitionTrackerScreen`  *(TypeDefIndex 3904)*

| name | value |
|---|--:|
| `LandingScreen` | 0 |
| `Map` | 1 |
| `GameStart` | 2 |
| `RewardScreen` | 3 |
| `DeckBuilder` | 4 |

### `UIBehaviorType`  *(TypeDefIndex 3828)*

| name | value |
|---|--:|
| `InvalidType` | 0 |
| `GameCenter` | 1 |
| `FacebookLoginClick` | 2 |
| `FacebookLogout` | 3 |
| `FacebookLoginSuccess` | 4 |
| `StatsPage` | 5 |
| `TutorialSkip` | 6 |
| `GemStore` | 7 |
| `Almanac` | 8 |
| `Quest` | 9 |
| `Events` | 10 |
| `CardStore` | 11 |
| `ProfileReconClick` | 12 |
| `ProfileReconConfirm` | 13 |
| `Collection` | 14 |
| `TeamEditor` | 15 |
| `Ea2` | 16 |
| `Comic` | 17 |
| `ScheduledEvent` | 18 |
| `DeckRecipe` | 19 |
| `MiniStore` | 20 |
| `Inbox` | 21 |

## `PvZCards.Core` (1 enums)

### `PvpContextKey`  *(TypeDefIndex 12684)*

| name | value |
|---|--:|
| `LocalService` | 0 |
| `RemoteService` | 1 |

## `PvZCards.Core.AI` (1 enums)

### `MainPhaseStrategyKeys`  *(TypeDefIndex 13274)*

| name | value |
|---|--:|
| `Main` | 0 |
| `Surprise` | 1 |

## `PvZCards.Core.Annotations` (2 enums)

### `ImplicitUseKindFlags`  *(TypeDefIndex 13240)*

| name | value |
|---|--:|
| `Default` | 7 |
| `Access` | 1 |
| `Assign` | 2 |
| `InstantiatedWithFixedConstructorSignature` | 4 |
| `InstantiatedNoFixedConstructorSignature` | 8 |

### `ImplicitUseTargetFlags`  *(TypeDefIndex 13241)*

| name | value |
|---|--:|
| `Default` | 1 |
| `Itself` | 1 |
| `Members` | 2 |
| `WithMembers` | 3 |

## `PvZCards.Core.Data` (8 enums)

### `DesignStatus`  *(TypeDefIndex 13203)*

| name | value |
|---|--:|
| `InDesign` | 0 |
| `DraftComplete` | 1 |
| `NeedsRevision` | 2 |
| `Finished` | 3 |

### `GameEventDismissType`  *(TypeDefIndex 13183)*

| name | value |
|---|--:|
| `TapScreen` | 0 |
| `PlayNextCard` | 1 |
| `WaitForSeconds` | 2 |
| `TapFightButton` | 3 |
| `Manual` | 4 |

### `GameEventTriggerType`  *(TypeDefIndex 13182)*

| name | value |
|---|--:|
| `PlayedCard` | 0 |
| `Turn` | 1 |
| `FirstBlock` | 2 |
| `BeforeCombatWithBlock` | 3 |
| `PlayedAllCards` | 4 |
| `AllInstancesOfRecord` | 5 |
| `DrawCard` | 6 |
| `AllInstancesOfPlayCard` | 7 |

### `HeroSelectionMode`  *(TypeDefIndex 13202)*

| name | value |
|---|--:|
| `PreSelected` | 0 |
| `PlayerChoice` | 1 |

### `MessageNarrator`  *(TypeDefIndex 13181)*

| name | value |
|---|--:|
| `None` | 0 |
| `Plants` | 1 |
| `Zombies` | 2 |

### `MessageViewType`  *(TypeDefIndex 13180)*

| name | value |
|---|--:|
| `NarratorBubbleTailTop` | 0 |
| `NarratorBubbleTailBottom` | 1 |
| `TextHint` | 2 |
| `CardCallout` | 3 |

### `NodeRepresentationType`  *(TypeDefIndex 13204)*

| name | value |
|---|--:|
| `None` | 0 |
| `Hero` | 1 |
| `Card` | 2 |
| `TreasureChest` | 3 |
| `SpecialTreasureChest` | 4 |
| `SpecialTreasureChestWithHero` | 5 |

### `PointerPositionType`  *(TypeDefIndex 13175)*

| name | value |
|---|--:|
| `Coordinates` | 0 |
| `Card` | 1 |
| `Fighter` | 2 |

## `PvZCards.Core.Systems.Utilities` (1 enums)

### `MultiplierContext`  *(TypeDefIndex 13020)*

| name | value |
|---|--:|
| `Default` | 0 |
| `Attack` | 1 |
| `Health` | 2 |

## `PvZCards.Core.Utilities` (6 enums)

### `BlockingPhaseType`  *(TypeDefIndex 12764)*

| name | value |
|---|--:|
| `Unknown` | 0 |
| `Main` | 1 |
| `Surprise` | 2 |
| `PrimaryTarget` | 3 |
| `SecondaryTarget` | 4 |
| `Superblock` | 5 |
| `Mulligan` | 6 |

### `GameErrorCode`  *(TypeDefIndex 12722)*

| name | value |
|---|--:|
| `None` | 0 |
| `MatchmakingServerError` | 1 |
| `MatchmakingNetworkError` | 2 |
| `GameInitNetworkError` | 3 |
| `GameInitServerError` | 4 |
| `PvpNetworkError` | 5 |
| `PvpServerError` | 6 |
| `PvpInvalidPlayError` | 7 |
| `PvpOutOfDatePlayError` | 8 |

### `GameOverState`  *(TypeDefIndex 12721)*

| name | value |
|---|--:|
| `None` | 0 |
| `Loss` | 1 |
| `Concession` | 2 |

### `GameState`  *(TypeDefIndex 12720)*

| name | value |
|---|--:|
| `NotStarted` | 0 |
| `Playing` | 1 |
| `GameOver` | 2 |
| `Error` | 3 |

### `MatchResult`  *(TypeDefIndex 12710)*

| name | value |
|---|--:|
| `Loss` | 0 |
| `Win` | 1 |
| `Tie` | 2 |

### `MatchResultReason`  *(TypeDefIndex 12711)*

| name | value |
|---|--:|
| `Play` | 0 |
| `Inactivity` | 1 |

## `PvZCards.DailyLoginCalendar` (2 enums)

### `DailyLoginRewardType`  *(TypeDefIndex 3322)*

| name | value |
|---|--:|
| `None` | 0 |
| `Gems` | 1 |
| `Tickets` | 2 |
| `Sparks` | 3 |
| `Pack` | 4 |
| `Card` | 5 |

### `RewardStatus`  *(TypeDefIndex 3321)*

| name | value |
|---|--:|
| `LOCKED` | 0 |
| `REWARD_ALREADY_PASSED` | 1 |
| `READY_TO_CLAIM` | 2 |
| `CLAIMED` | 3 |
| `MISSED` | 4 |
| `NEXT_IN` | 5 |

## `PvZCards.DeckEditor` (2 enums)

### `CardLocation`  *(TypeDefIndex 3226)*

| name | value |
|---|--:|
| `Invalid` | 0 |
| `Superpower` | 1 |
| `Deck` | 2 |
| `Owned` | 3 |
| `Unowned` | 4 |

### `DeckEditUsageStatus`  *(TypeDefIndex 3227)*

| name | value |
|---|--:|
| `Unknown` | 0 |
| `EmptyDeck` | 1 |
| `FinishLater` | 2 |
| `Finished` | 3 |
| `FinishForMe` | 4 |

## `PvZCards.DeckRecipes` (2 enums)

### `DeckRecipeAcquisitionMethod`  *(TypeDefIndex 3176)*

| name | value |
|---|--:|
| `None` | 0 |
| `Craft` | 1 |
| `Silver` | 2 |
| `Booster` | 3 |
| `Buy` | 4 |

### `DeckRecipeInfoScreenShownFrom`  *(TypeDefIndex 3183)*

| name | value |
|---|--:|
| `Other` | 0 |
| `Ftue` | 1 |
| `PackOpening` | 2 |

## `PvZCards.Draper` (2 enums)

### `InterstitialCoordinatorEvent`  *(TypeDefIndex 3087)*

| name | value |
|---|--:|
| `LoadRequested` | 0 |
| `LoadSucceeded` | 1 |
| `LoadFailed` | 2 |
| `ShowRequested` | 3 |
| `ShowIfReadyRequested` | 4 |
| `DisableRequested` | 5 |

### `InterstitialCoordinatorState`  *(TypeDefIndex 3086)*

| name | value |
|---|--:|
| `Idle` | 0 |
| `LoadingToWait` | 1 |
| `LoadingToShow` | 2 |
| `LoadedAndWaiting` | 3 |

## `PvZCards.Engine` (7 enums)

### `ChannelLoggerLevel`  *(TypeDefIndex 15279)*

| name | value |
|---|--:|
| `Verbose` | 0 |
| `Debug` | 1 |
| `Warning` | 2 |
| `Error` | 3 |
| `Off` | 4 |

### `EffectTagType`  *(TypeDefIndex 15309)*

| name | value |
|---|--:|
| `None` | 0 |
| `Armored` | 1 |
| `Deadly` | 2 |
| `ExtraAttack` | 3 |
| `FrenzyAttack` | 4 |
| `Truestrike` | 5 |
| `DiedNormally` | 6 |
| `Unhurtable` | 7 |
| `ExtraAttackCancelled` | 8 |
| `Mustached` | 9 |
| `AttackOverride` | 10 |
| `MultiplyDamage` | 11 |
| `HealCancelled` | 12 |

### `FactionType`  *(TypeDefIndex 15296)*

| name | value |
|---|--:|
| `Plants` | 0 |
| `Zombies` | 1 |
| `All` | 2 |

### `PvpMode`  *(TypeDefIndex 15304)*

| name | value |
|---|--:|
| `None` | 0 |
| `Casual` | 1 |
| `Ranked` | 2 |
| `Challenge` | 3 |

### `RecordKey`  *(TypeDefIndex 15339)*

| name | value |
|---|--:|
| `SourceId` | 0 |
| `TargetId` | 1 |
| `EffectEntityId` | 2 |
| `AbilityGroupId` | 3 |
| `TurnCount` | 4 |
| `ZoneIds` | 5 |
| `CardId` | 6 |
| `HandId` | 7 |
| `PlayerId` | 8 |
| `DamageAmount` | 9 |
| `HealthDelta` | 10 |
| `AbsoluteHealth` | 11 |
| `AttackDelta` | 12 |
| `BuffDuration` | 13 |
| `LaneId` | 14 |
| `AssetId` | 15 |
| `OrderInLane` | 16 |
| `OldLaneId` | 17 |
| `AmountIncremented` | 18 |
| `AvailableSun` | 19 |
| `IsFaceDown` | 20 |
| `BlocksRemaining` | 21 |
| `GrantableAbilityType` | 22 |
| `BlockMeterValue` | 23 |
| `DeckId` | 24 |
| `SunCostAmount` | 25 |
| `EffectGroupId` | 26 |
| `BlockerIds` | 27 |
| `ChargeAmount` | 28 |
| `TriggerType` | 29 |
| `ShowParticleEffect` | 30 |
| `WillKillHero` | 31 |
| `IsBlock` | 32 |
| `AttemptedAmountIncremented` | 33 |
| `OriginalSourceId` | 34 |
| `OverwrittenEnvironmentId` | 35 |
| `CardIdData` | 36 |
| `TotalSun` | 37 |
| `DiscardPileId` | 38 |
| `SuperpowerPoolId` | 39 |
| `GrantableAbilityValue` | 40 |
| `NewBaseValue` | 41 |
| `NewBuffDelta` | 42 |
| `CardStatType` | 43 |
| `IsExcludedFromGroups` | 44 |
| `AbilityGuid` | 45 |
| `HasUniqueAbility` | 46 |
| `FromTriggeredEffect` | 47 |
| `LocationId` | 48 |
| `SpecialAbilityType` | 49 |
| `CurrentValue` | 50 |
| `IsOnSelfPlay` | 51 |

### `RecordType`  *(TypeDefIndex 15338)*

| name | value |
|---|--:|
| `Default` | 0 |
| `TurnStartComplete` | 1 |
| `PlayOneTimeEffect` | 2 |
| `PlayCardToLanes` | 3 |
| `Damage` | 4 |
| `Block` | 5 |
| `Buff` | 6 |
| `ZombieMainPhaseStart` | 7 |
| `PlantMainPhaseStart` | 8 |
| `ZombieSurprisePhaseStart` | 9 |
| `PhaseResume` | 10 |
| `DrawHandPhaseStart` | 11 |
| `DrawHandPhaseEnd` | 12 |
| `MulliganPhaseStart` | 13 |
| `MulliganPhaseEnd` | 14 |
| `LaneCombatStart` | 15 |
| `TriggeredAbilityStart` | 16 |
| `PlaySuperblockPhaseStart` | 17 |
| `PlaySuperblock` | 18 |
| `KeepSuperblock` | 19 |
| `ChooseTargetPhaseStart` | 20 |
| `ChooseTargetPhaseEnd` | 21 |
| `CreateLane` | 22 |
| `CardMove` | 23 |
| `GainSun` | 24 |
| `Heal` | 25 |
| `Reveal` | 26 |
| `ReturnToHandFromPlay` | 27 |
| `CreateCard` | 28 |
| `TurnStart` | 29 |
| `MulliganCard` | 30 |
| `EndMulligan` | 31 |
| `DeadlyDamage` | 32 |
| `AbilityGranted` | 33 |
| `AbilityRemoved` | 34 |
| `DrawPlayerHand` | 35 |
| `SlowRecord` | 36 |
| `UnslowRecord` | 37 |
| `CombatStart` | 38 |
| `CombatEnd` | 39 |
| `LoseRecord` | 40 |
| `ChargeBlockMeterRecord` | 41 |
| `DrawCard` | 42 |
| `ModifySunCost` | 43 |
| `Destroy` | 44 |
| `DiscardFromPlay` | 45 |
| `NullDamage` | 46 |
| `TurnIntoGravestone` | 47 |
| `TransformIntoCard` | 48 |
| `ShuffleGravestone` | 49 |
| `MinHealthBlock` | 50 |
| `RedirectAbilityStart` | 51 |
| `CardReset` | 52 |
| `DrawCardFromSuperpowerPool` | 53 |
| `PlayerPhaseEnd` | 54 |
| `StartTurnTimer` | 55 |
| `EndTurnTimer` | 56 |
| `StartMulliganTimer` | 57 |
| `EndMulliganTimer` | 58 |
| `StartInterruptTimer` | 59 |
| `EndInterruptTimer` | 60 |
| `ConcedeRecord` | 61 |
| `RevealPhaseEnd` | 62 |
| `PlayEnvironment` | 63 |
| `DestroyEnvironment` | 64 |
| `ExtraAttack` | 65 |
| `FrenzyAttack` | 66 |
| `ExtraAttackCancelled` | 67 |
| `AttackBuffExpiration` | 68 |
| `HealthBuffExpiration` | 69 |
| `StartGame` | 70 |
| `SunCostDeducted` | 71 |
| `SunCountIncremented` | 72 |
| `LaneCombatEnd` | 73 |
| `CardReturnedToPlayersDeck` | 74 |
| `DiscardFromDeck` | 75 |
| `DiscardSuperpowerCard` | 76 |
| `SetStat` | 77 |
| `TriggeredAbilityEnd` | 78 |
| `RedirectAbiltyEnd` | 79 |
| `TriggeredAbilityGranted` | 80 |
| `TransformIntoCardInHand` | 81 |
| `TriggeredAbilityRemoved` | 82 |
| `CreateEnvironmentInLane` | 83 |
| `CreateBoardAbility` | 84 |
| `CreateCardsInDeck` | 85 |
| `PlayFighterOnFighter` | 86 |
| `RemoveDisplacedFighter` | 87 |
| `HealCancelled` | 88 |

### `ServiceFailure`  *(TypeDefIndex 15305)*

| name | value |
|---|--:|
| `DidNotFail` | 0 |
| `InvalidTarget` | 1 |
| `NotEnoughResources` | 2 |
| `IsNotFighter` | 3 |
| `HasNoTarget` | 4 |
| `IsNotALane` | 5 |
| `LaneIsFull` | 6 |
| `MustHaveTeamup` | 7 |
| `MustOnlyMulliganInitialCards` | 8 |
| `NullFailure` | 9 |
| `WrongPlayer` | 10 |
| `ServerOutOfSync` | 11 |
| `SocketError` | 12 |
| `GameAlreadyOver` | 13 |
| `CantEndTurn` | 14 |
| `NotMulliganPhase` | 15 |
| `NoBlockingEffect` | 16 |
| `IsNotAquatic` | 17 |
| `MissingSunCost` | 18 |
| `MissingPvpMessages` | 19 |
| `NotSuperblockPhase` | 20 |
| `NotPlayCardPhase` | 21 |
| `NotASuperblockCard` | 22 |
| `BurstHasNoEffects` | 23 |
| `CardNotInHand` | 24 |
| `UnplayableCard` | 25 |
| `CardNotPlayableDuringPhase` | 26 |
| `ResponseParsingError` | 27 |
| `WrongBlockingEffect` | 28 |
| `InvalidPlayType` | 29 |
| `DuplicatePlay` | 30 |
| `CantEndTurnForOpponentYet` | 31 |
| `ServerCallFailedForSomeReasonHandledElsewhere` | 32 |
| `ConcedingForOpponentBeforeMulligansAreDone` | 33 |

## `PvZCards.Engine.Components` (17 enums)

### `AbilityValueType`  *(TypeDefIndex 15751)*

| name | value |
|---|--:|
| `None` | 0 |
| `Damage` | 1 |

### `AdditionalTargetType`  *(TypeDefIndex 15933)*

| name | value |
|---|--:|
| `None` | 0 |
| `Query` | 1 |

### `CardColor`  *(TypeDefIndex 15831)*

| name | value |
|---|--:|
| `Kabloom` | 1 |
| `MegaGro` | 2 |
| `Guardian` | 4 |
| `Smarty` | 8 |
| `Solar` | 16 |
| `Brainy` | 32 |
| `Hearty` | 64 |
| `Sneaky` | 128 |
| `Hungry` | 256 |
| `Madcap` | 512 |

### `CardStat`  *(TypeDefIndex 15714)*

| name | value |
|---|--:|
| `None` | 0 |
| `Attack` | 1 |
| `Health` | 2 |
| `SunCost` | 3 |

### `GrantableAbilityType`  *(TypeDefIndex 15801)*

| name | value |
|---|--:|
| `None` | 0 |
| `Unhurtable` | 1 |
| `Deadly` | 2 |
| `Frenzy` | 3 |
| `Truestrike` | 4 |
| `Strikethrough` | 5 |
| `Afterlife` | 6 |
| `MinHealth` | 7 |
| `NoExtraAttacks` | 8 |
| `GravestoneSpy` | 9 |
| `Teamup` | 10 |
| `Aquatic` | 11 |
| `CanPlayFighterInSurprisePhase` | 12 |
| `Mustache` | 13 |
| `AttackOverride` | 14 |
| `MultiplyDamage` | 15 |
| `Graveyard` | 16 |
| `Untrickable` | 17 |
| `Unhealable` | 18 |

### `LaneOrder`  *(TypeDefIndex 15865)*

| name | value |
|---|--:|
| `Any` | -1 |
| `Front` | 0 |
| `Back` | 1 |

### `LaneType`  *(TypeDefIndex 15864)*

| name | value |
|---|--:|
| `Highground` | 0 |
| `Grass` | 1 |
| `Water` | 2 |
| `None` | 3 |

### `NamedAbilityType`  *(TypeDefIndex 15783)*

| name | value |
|---|--:|
| `None` | 0 |
| `Ambush` | 9 |
| `Repeater` | 11 |
| `Overshoot` | 12 |
| `Unique` | 13 |

### `QueryEntityConditionEvaluationType`  *(TypeDefIndex 15786)*

| name | value |
|---|--:|
| `All` | 0 |
| `Any` | 1 |

### `RarityValue`  *(TypeDefIndex 15829)*

| name | value |
|---|--:|
| `R1` | 0 |
| `R2` | 1 |
| `R3` | 2 |
| `R4` | 3 |
| `R0` | 4 |
| `Event` | 5 |

### `SpecialAbilityType`  *(TypeDefIndex 15782)*

| name | value |
|---|--:|
| `None` | 0 |
| `Unhurtable` | 1 |
| `Deadly` | 2 |
| `Frenzy` | 3 |
| `Truestrike` | 4 |
| `Strikethrough` | 5 |
| `Afterlife` | 6 |
| `MinHealth` | 7 |
| `Armor` | 8 |
| `Ambush` | 9 |
| `AttackOverride` | 10 |
| `Repeater` | 11 |
| `Overshoot` | 12 |
| `Unique` | 13 |
| `Untrickable` | 14 |

### `TargetScopeSortMethod`  *(TypeDefIndex 15932)*

| name | value |
|---|--:|
| `None` | 0 |
| `Lowest` | 1 |
| `Highest` | 2 |

### `TargetScopeSortValue`  *(TypeDefIndex 15931)*

| name | value |
|---|--:|
| `None` | 0 |
| `Attack` | 1 |
| `Health` | 2 |

### `TargetScopeType`  *(TypeDefIndex 15930)*

| name | value |
|---|--:|
| `All` | 0 |
| `Sorted` | 1 |

### `TargetSelectionType`  *(TypeDefIndex 15928)*

| name | value |
|---|--:|
| `All` | 0 |
| `Manual` | 1 |
| `Random` | 2 |

### `TargetStep`  *(TypeDefIndex 15929)*

| name | value |
|---|--:|
| `Primary` | 0 |
| `Secondary` | 1 |

### `TrickImmunityType`  *(TypeDefIndex 15771)*

| name | value |
|---|--:|
| `All` | 0 |
| `Plants` | 1 |
| `Zombies` | 2 |

## `PvZCards.Engine.Components.Data` (1 enums)

### `ActivationTime`  *(TypeDefIndex 15953)*

| name | value |
|---|--:|
| `Immediate` | 0 |
| `NextTurn` | 1 |

## `PvZCards.Engine.Data` (7 enums)

### `DateConflictResolutionLogic`  *(TypeDefIndex 15710)*

| name | value |
|---|--:|
| `HighestValueInUtcTimestampFormat` | 0 |
| `LowestValueInUtcTimestampFormat` | 1 |
| `SetOnce` | 2 |

### `DynamicTagType`  *(TypeDefIndex 15708)*

| name | value |
|---|--:|
| `HeroGamesPlayed` | 0 |
| `FeatureUnlockProgress` | 1 |
| `FeatureUnlockCompletion` | 2 |
| `PveProgressWins` | 3 |
| `PveProgressLosses` | 4 |
| `LinearQuestCompletions` | 5 |
| `TotalHeroUsages` | 6 |

### `InventoryType`  *(TypeDefIndex 15685)*

| name | value |
|---|--:|
| `UNKNOWN` | 0 |
| `CARD` | 1 |
| `HERO` | 2 |
| `CURRENCY` | 3 |

### `NamedDates`  *(TypeDefIndex 15709)*

| name | value |
|---|--:|
| `LastQuestCompleted` | 0 |
| `LastPvpGamePlayed` | 1 |
| `FirstQuestCompleted` | 2 |
| `Birth` | 3 |
| `LastSessionStarted` | 4 |

### `PlayerDataLevel`  *(TypeDefIndex 15681)*

| name | value |
|---|--:|
| `None` | 0 |
| `WorldWide` | 1 |
| `Tu4` | 2 |

### `QuestType`  *(TypeDefIndex 15699)*

| name | value |
|---|--:|
| `Unknown` | 0 |
| `Random` | 1 |
| `Linear` | 2 |
| `FeatureUnlock` | 3 |
| `Hero` | 4 |

### `UserTag`  *(TypeDefIndex 15707)*

| name | value |
|---|--:|
| `PvEGamesCompletedAsPlants` | 0 |
| `PvEGamesCompletedAsZombies` | 1 |
| `PrologueNarrativeShown` | 2 |
| `PvEPlayerElo` | 3 |
| `StoreLandingScreenTutorialShown` | 4 |
| `StoreTutorialShown` | 5 |
| `ShowOldSplashScreen` | 6 |
| `LinearQuest1` | 7 |
| `LinearQuest2` | 8 |
| `LinearQuest3` | 9 |
| `LinearQuest4` | 10 |
| `LinearQuest5` | 11 |
| `LinearQuest6` | 12 |
| `LinearQuest7` | 13 |
| `LinearQuest8` | 14 |
| `LinearQuest9` | 15 |
| `LinearQuest10` | 16 |
| `LinearQuest11` | 17 |
| `LinearQuest12` | 18 |
| `LinearQuest13` | 19 |
| `LinearQuest14` | 20 |
| `LinearQuest15` | 21 |
| `LinearQuest16` | 22 |
| `LinearQuest17` | 23 |
| `LinearQuest18` | 24 |
| `LinearQuest19` | 25 |
| `LinearQuest20` | 26 |
| `LinearQuest21` | 27 |
| `LinearQuest22` | 28 |
| `LinearQuest23` | 29 |
| `LinearQuestMax` | 30 |
| `HasSeenPlantHqUnlock` | 31 |
| `HasSeenZombieHqUnlock` | 32 |
| `HasChosenSecondPlantHero` | 33 |
| `HasChosenThirdPlantHero` | 34 |
| `HasChosenFirstZombieHero` | 35 |
| `HasChosenSecondZombieHero` | 36 |
| `HasSeenTeamBuilderUnlock` | 37 |
| `HasSeenQuestsUnlock` | 38 |
| `HasSeenHeroSelectTutorial` | 39 |
| `HasSeenTeamBuilderUnlockedTutorial` | 40 |
| `HasSeenDeckSelectTutorial` | 41 |
| `HasSeenTeamBuilderTutorial` | 42 |
| `HasSeenZombieHqTutorial` | 43 |
| `HasSeenQuestsTutorial` | 44 |
| `TotalPlantPvEWins` | 45 |
| `TotalZombiePvEWins` | 46 |
| `DeckBuilderShowAll` | 47 |
| `TotalQuestCompletions` | 48 |
| `HasSeenTempStoreTutorial` | 49 |
| `LifetimePvpGames` | 50 |
| `HasBeenGivenTeamBuilderCards` | 51 |
| `HasSeenAutoBuildSuggestion` | 52 |
| `ReceivedCardPackKabloom` | 53 |
| `ReceivedCardPackMegaGro` | 54 |
| `ReceivedCardPackGuardian` | 55 |
| `ReceivedCardPackSmarty` | 56 |
| `ReceivedCardPackSolar` | 57 |
| `ReceivedCardPackBrainy` | 58 |
| `ReceivedCardPackHearty` | 59 |
| `ReceivedCardPackSneaky` | 60 |
| `ReceivedCardPackHungry` | 61 |
| `ReceivedCardPackMadCap` | 62 |
| `HasCompletedDeckbuilderFtue1` | 63 |
| `HasCompletedDeckbuilderFtue2` | 64 |
| `HasCompletedDeckbuilderFtue3` | 65 |
| `HasSeenAcquiredAllSilver` | 66 |
| `LinearQuestMigrations` | 67 |
| `TotalPveGamesWithoutElo` | 68 |
| `HasSeenPvpIntro` | 69 |
| `DefunctProgressiveRegistrationRecorded` | 70 |
| `HasSeenQuestsTutorial2` | 71 |
| `HasSeenQuestsTutorial3` | 72 |
| `HasSeenAllFreePlantHeroes` | 73 |
| `HasSeenCardChaseTutorial` | 74 |
| `ProgressiveRegistrationRecorded` | 75 |
| `HasSeenFtueDeckRecipeTutorial1` | 76 |
| `HasSeenFtueDeckRecipeTutorial2` | 77 |
| `DeckRecipeWarningSeenCount` | 78 |
| `HasSeenNewDeckSelectorTutorial` | 79 |
| `HasSeenDailyBattleTutorial` | 80 |
| `HasSeenNotificationTutorial` | 81 |

## `PvZCards.Engine.Dtos` (1 enums)

### `PlayType`  *(TypeDefIndex 15659)*

| name | value |
|---|--:|
| `None` | 0 |
| `EndMainPhase` | 1 |
| `EndSurprisePhase` | 2 |
| `PlayCard` | 3 |
| `Mulligan` | 4 |
| `EndMulligan` | 5 |
| `SelectTarget` | 6 |
| `SelectRandomTarget` | 7 |
| `Concede` | 8 |
| `StartGame` | 9 |
| `ForceEndTurn` | 10 |
| `KeepSuperBlock` | 11 |
| `PlaySuperBlock` | 12 |
| `HorribleFailure` | 13 |
| `ConcedeForOpponent` | 14 |

## `PvZCards.Engine.Effects` (5 enums)

### `DeckPosition`  *(TypeDefIndex 15624)*

| name | value |
|---|--:|
| `Top` | 0 |
| `Bottom` | 1 |
| `Random` | 2 |

### `EffectDuration`  *(TypeDefIndex 15580)*

| name | value |
|---|--:|
| `Permanent` | 0 |
| `EndOfTurn` | 1 |
| `NextFighter` | 2 |

### `EffectValue`  *(TypeDefIndex 15581)*

| name | value |
|---|--:|
| `None` | 0 |
| `DamageAmount` | 1 |
| `HealAmount` | 2 |
| `AttackBuffAmount` | 3 |
| `HealthBuffAmount` | 4 |
| `TotalBuffAmount` | 5 |
| `DrawnCardId` | 6 |
| `HandId` | 7 |
| `DeckId` | 8 |
| `AbilityValue` | 9 |
| `SetStatValue` | 10 |
| `OriginalTargetGuid` | 11 |

### `StatModOperation`  *(TypeDefIndex 15546)*

| name | value |
|---|--:|
| `None` | 0 |
| `Set` | 1 |
| `Add` | 2 |

### `TargetingPhase`  *(TypeDefIndex 15557)*

| name | value |
|---|--:|
| `Primary` | 0 |
| `Secondary` | 1 |

## `PvZCards.Engine.Queries` (1 enums)

### `StatComparisonOperator`  *(TypeDefIndex 15518)*

| name | value |
|---|--:|
| `Equal` | 0 |
| `LessOrEqual` | 1 |
| `GreaterOrEqual` | 2 |

## `PvZCards.Engine.Utilities` (3 enums)

### `SyncDataType`  *(TypeDefIndex 15366)*

| name | value |
|---|--:|
| `PlayerInventory` | 0 |
| `User` | 1 |
| `Quests` | 2 |
| `PvP` | 3 |
| `PvpSeasonHistory` | 4 |
| `Decks` | 5 |
| `PurchaseHistory` | 6 |

### `SyncLevelRequired`  *(TypeDefIndex 15367)*

| name | value |
|---|--:|
| `None` | 0 |
| `IfDelta` | 1 |
| `AlwaysTry` | 2 |
| `MustSucceed` | 3 |

### `SyncStatus`  *(TypeDefIndex 15361)*

| name | value |
|---|--:|
| `NotTried` | 0 |
| `InProgress` | 1 |
| `Failed` | 2 |
| `Succeeded` | 3 |

## `PvZCards.Game` (32 enums)

### `ActionExecutionMethod`  *(TypeDefIndex 6140)*

| name | value |
|---|--:|
| `TightLoop` | 0 |
| `Coroutine` | 1 |
| `Thread` | 2 |

### `AnimationEventType`  *(TypeDefIndex 5104)*

| name | value |
|---|--:|
| `Damage` | 0 |
| `Buff` | 1 |
| `MoveCard` | 2 |
| `GainSun` | 3 |
| `Heal` | 4 |
| `CreateCard` | 5 |
| `GrantAbility` | 6 |
| `RemoveAbility` | 7 |
| `Slow` | 8 |
| `ModifySunCost` | 9 |
| `TurnIntoGravestone` | 10 |
| `TransformCard` | 11 |
| `ShuffleGravestone` | 12 |
| `ChargeBlockMeter` | 13 |
| `Destroy` | 14 |
| `DrawCard` | 15 |
| `Bounce` | 16 |
| `None` | 17 |
| `ResetCard` | 18 |
| `TransformCardInHand` | 19 |

### `BlockMeterFillDirection`  *(TypeDefIndex 5129)*

| name | value |
|---|--:|
| `None` | 0 |
| `Regular` | 1 |
| `Reverse` | -1 |

### `CardFrameIconType`  *(TypeDefIndex 5174)*

| name | value |
|---|--:|
| `BasicAttack` | 0 |
| `BasicHealth` | 1 |
| `Armor` | 2 |
| `Deadly` | 3 |
| `Frenzy` | 4 |
| `Multi` | 5 |
| `Strikethrough` | 6 |
| `Truestrike` | 7 |
| `Unhurtable` | 8 |
| `Ambush` | 9 |
| `AttackOverride` | 10 |
| `Repeater` | 11 |
| `Overshoot` | 12 |
| `HealthMulti` | 13 |
| `Untrickable` | 14 |

### `CardFrameType`  *(TypeDefIndex 5192)*

| name | value |
|---|--:|
| `Fighter` | 0 |
| `Trick` | 1 |
| `Environment` | 2 |
| `TrickPower` | 3 |
| `FighterPower` | 4 |
| `EnvironmentPower` | 5 |

### `CardVisualEffectType`  *(TypeDefIndex 5242)*

| name | value |
|---|--:|
| `Slow` | 0 |
| `Springboard` | 1 |
| `Evolution` | 2 |

### `DrawAnimationType`  *(TypeDefIndex 5324)*

| name | value |
|---|--:|
| `DirectlyToHand` | 0 |
| `DrawForMulligan` | 1 |
| `Herald` | 2 |

### `EffectVisualType`  *(TypeDefIndex 5362)*

| name | value |
|---|--:|
| `Floater` | 0 |
| `Prefab` | 1 |

### `EndTurnEvents`  *(TypeDefIndex 5381)*

| name | value |
|---|--:|
| `CardsPlayable` | 0 |
| `NoCardsPlayable` | 1 |
| `TurnEnded` | 2 |
| `TurnInterruptedByOtherPhase` | 3 |
| `ResumeOptionalEarlyEnd` | 4 |
| `ResumeDone` | 5 |
| `ForceEndTurn` | 6 |
| `DisableInput` | 7 |

### `EndTurnSpriteStates`  *(TypeDefIndex 5382)*

| name | value |
|---|--:|
| `MainPhase` | 0 |
| `TricksPhase` | 1 |
| `KeepOldValue` | 2 |

### `EndTurnStates`  *(TypeDefIndex 5380)*

| name | value |
|---|--:|
| `Disabled` | 0 |
| `OptionalEarlyEnd` | 1 |
| `Done` | 2 |
| `Interrupted` | 3 |

### `ExecutableFactoryKeys`  *(TypeDefIndex 5974)*

| name | value |
|---|--:|
| `Main` | 0 |
| `Delay` | 1 |

### `GameEventRecordProcessorKeys`  *(TypeDefIndex 5448)*

| name | value |
|---|--:|
| `PreAnimation` | 0 |
| `PostAnimation` | 1 |

### `GameInputState`  *(TypeDefIndex 5465)*

| name | value |
|---|--:|
| `Default` | 0 |
| `NotAccepted` | 1 |
| `Animating` | 2 |
| `SelectCardToPlayInactive` | 3 |
| `SelectCardToPlayActive` | 4 |
| `SelectPlayCardTarget` | 5 |
| `SelectTriggeredEffectPrimaryTarget` | 6 |
| `SelectTriggeredEffectSecondaryTarget` | 7 |
| `SelectPlaySuperblock` | 8 |
| `SelectPlaySuperblockTarget` | 9 |

### `IdleType`  *(TypeDefIndex 5605)*

| name | value |
|---|--:|
| `IdleRegular` | 0 |
| `IdleTense` | 1 |
| `IdleThinking` | 2 |
| `IdleLose` | 3 |

### `IntroOrder`  *(TypeDefIndex 5630)*

| name | value |
|---|--:|
| `OpponentPlayerFirst` | 0 |
| `LocalPlayerFirst` | 1 |

### `LaneUIPosition`  *(TypeDefIndex 5713)*

| name | value |
|---|--:|
| `Invalid` | -1 |
| `FrontSpot` | 0 |
| `BackSpot` | 1 |

### `MusicSynchAction`  *(TypeDefIndex 5821)*

| name | value |
|---|--:|
| `Ignore` | 0 |
| `Quarter` | 1 |
| `Eight` | 2 |
| `Note` | 3 |
| `HalfNote` | 4 |

### `NonEntityViewId`  *(TypeDefIndex 5838)*

| name | value |
|---|--:|
| `LocalMulliganScreen` | 0 |
| `Unused` | 1 |
| `AllySunCounter` | 2 |
| `OpponentSunCounter` | 3 |
| `AllyLife` | 4 |
| `OpponentLife` | 5 |
| `AllyHand` | 6 |
| `OpponentHand` | 7 |
| `AllySuperblockHand` | 8 |
| `OpponentSuperblockHand` | 9 |

### `OneTimeEffectAnchorTypeX`  *(TypeDefIndex 5847)*

| name | value |
|---|--:|
| `PlayedPosition` | 0 |
| `MiddleScreenPosition` | 1 |

### `OneTimeEffectAnchorTypeY`  *(TypeDefIndex 5848)*

| name | value |
|---|--:|
| `PlayedPosition` | 0 |
| `MiddleScreenPosition` | 1 |
| `PlantSide` | 2 |
| `ZombieSide` | 3 |

### `PlayerBehaviorKeys`  *(TypeDefIndex 5889)*

| name | value |
|---|--:|
| `Plants` | 0 |
| `Zombies` | 1 |

### `ProjectileOriginType`  *(TypeDefIndex 5934)*

| name | value |
|---|--:|
| `Static` | 0 |
| `Hero` | 1 |

### `ProjectileType`  *(TypeDefIndex 5935)*

| name | value |
|---|--:|
| `Normal` | 0 |
| `Random` | 1 |

### `PvpAIStrategy`  *(TypeDefIndex 5429)*

| name | value |
|---|--:|
| `Disabled` | 0 |
| `Normal` | 1 |
| `OnlyEndsTurns` | 2 |

### `PvpEvent`  *(TypeDefIndex 5961)*

| name | value |
|---|--:|
| `StartMulligans` | 0 |
| `StartGame` | 1 |
| `OpponentMulligan` | 2 |
| `PlayerMulligan` | 3 |
| `SentPlayerMulligan` | 4 |
| `MulliganTimerComplete` | 5 |
| `WaitForOpponentDelayComplete` | 6 |
| `TurnEnded` | 7 |
| `OpponentTurnTimerComplete` | 8 |
| `GameEnded` | 9 |
| `ErrorOccurred` | 10 |
| `NewTimerAdded` | 11 |
| `TimerWillEnd` | 12 |
| `EndedGameForOpponent` | 13 |

### `PvpState`  *(TypeDefIndex 5960)*

| name | value |
|---|--:|
| `Playing` | 0 |
| `Mulliganing` | 1 |
| `OpponentMulliganing` | 2 |
| `PlayerMulliganing` | 3 |
| `WaitingOnMulligansFromServer` | 4 |
| `WaitingOnMulligansFromServerAndOpponent` | 5 |
| `WaitingOnMulligansFromServerAndTimerIsUp` | 6 |
| `WaitingForOpponentToReconnect` | 7 |
| `WaitingOnMulligansFromServerAndOpponentAndTurnTimerComplete` | 8 |
| `OpponentDisconnected` | 9 |
| `EndingGameForOpponent` | 10 |
| `GameOver` | 11 |
| `Error` | 12 |

### `RecipeDeckRarity`  *(TypeDefIndex 5293)*

| name | value |
|---|--:|
| `R1` | 0 |
| `R2` | 1 |
| `R3` | 2 |
| `RSpace` | 3 |
| `RDinosaur` | 4 |
| `RTriassic` | 5 |
| `RSet5` | 6 |
| `RSet6` | 7 |

### `SubTurn`  *(TypeDefIndex 6161)*

| name | value |
|---|--:|
| `None` | 0 |
| `LocalTargetPhase` | 1 |
| `OpponentTargetPhase` | 2 |
| `LocalSuperblockPhase` | 3 |
| `OpponentSuperblockPhase` | 4 |

### `TargetSorOrder`  *(TypeDefIndex 6174)*

| name | value |
|---|--:|
| `Hero` | 0 |
| `Card` | 1 |
| `Environment` | 2 |
| `Lane` | 3 |
| `None` | 4 |

### `ToolTipPositionType`  *(TypeDefIndex 5222)*

| name | value |
|---|--:|
| `Basic` | 0 |
| `Card` | 1 |
| `OpponentGravestone` | 2 |
| `OpponentFighter` | 3 |

### `Turn`  *(TypeDefIndex 6160)*

| name | value |
|---|--:|
| `StartingPhase` | 0 |
| `ZombieMainPhase` | 1 |
| `PlantMainPhase` | 2 |
| `ZombieSurprisePhase` | 3 |
| `CombatPhase` | 4 |
| `GameDonePhase` | 5 |
| `MulliganPhase` | 6 |

## `PvZCards.GameOver` (8 enums)

### `GameOverFlowEvent`  *(TypeDefIndex 2948)*

| name | value |
|---|--:|
| `GameOverScreenFinishedAnimating` | 0 |
| `RewardScreenFinished` | 1 |
| `InterstitialDone` | 2 |
| `OneClickPurchaseFlowStarted` | 3 |
| `OneClickPurchaseFlowFinished` | 4 |
| `FinishGameOverFlowCalled` | 5 |
| `EmptyScreenLoaded` | 6 |
| `StartSwappingSoundBanks` | 7 |
| `DoneSwappingSoundBanks` | 8 |
| `FeatureUnlockToastShowed` | 9 |
| `HeroRewardScreenShowed` | 10 |
| `NarrativeComplete` | 11 |
| `ShowScreen` | 12 |
| `ScreenClosed` | 13 |
| `QuestRollupDone` | 14 |
| `GameWonLost` | 15 |
| `StarScreenFinished` | 16 |
| `RewardPackOpeningDone` | 17 |
| `ServerCallsCompleted` | 18 |
| `PvpRewardFailure` | 19 |
| `PvpRewardFailureScreenFinished` | 20 |

### `GameOverFlowFinalAction`  *(TypeDefIndex 2949)*

| name | value |
|---|--:|
| `None` | 0 |
| `NextBattle` | 1 |
| `ReturnToLandingScreen` | 2 |
| `PlayAgain` | 3 |
| `StartNewPvpGame` | 4 |
| `ShowDeckBuilderFtue` | 5 |

### `GameOverFlowState`  *(TypeDefIndex 2947)*

| name | value |
|---|--:|
| `GamePlayed` | 0 |
| `GameOverScreenWonLost` | 1 |
| `Rewards` | 2 |
| `StarRewards` | 3 |
| `RankUpRewards` | 4 |
| `TierUpRewards` | 5 |
| `PostGameNarrative` | 6 |
| `FinalScreen` | 7 |
| `LoadingEmptyScreen` | 8 |
| `SwappingSoundBanks` | 9 |
| `ShowingHeroRewardScreen` | 10 |
| `RewardPackOpening` | 11 |
| `WaitingForServerCallsToComplete` | 12 |
| `DraperInterstitial` | 13 |
| `OneClickPurchaseFlow` | 14 |
| `ScreenShown` | 15 |
| `AllDone` | 16 |
| `ShowingPvpRewardFailureScreen` | 17 |

### `RewardPresentationStep`  *(TypeDefIndex 2966)*

| name | value |
|---|--:|
| `GeneralGameRewards` | 0 |
| `DailyBattleRewards` | 1 |
| `DailyBattleCumulativeRewards` | 2 |

### `RewardsFlowEvent`  *(TypeDefIndex 3005)*

| name | value |
|---|--:|
| `Start` | 0 |
| `RewardsCollected` | 1 |

### `RewardsFlowState`  *(TypeDefIndex 3004)*

| name | value |
|---|--:|
| `Unstarted` | 0 |
| `GemAndEventPointRewards` | 1 |
| `PackRewards` | 2 |

### `ShowGameOverRewardsPresentationType`  *(TypeDefIndex 3009)*

| name | value |
|---|--:|
| `ReplaceScreen` | 0 |
| `OverlayScreen` | 1 |

### `SingleCardRewardScreenType`  *(TypeDefIndex 3024)*

| name | value |
|---|--:|
| `Loss` | 0 |
| `Win` | 1 |

## `PvZCards.GluRevSDK` (1 enums)

### `AD_Placement`  *(TypeDefIndex 6499)*

| name | value |
|---|--:|
| `Tickets` | 0 |
| `Redraws` | 1 |
| `QuestRefresh` | 2 |
| `WeeklyEvents` | 3 |
| `QuestTimer` | 4 |
| `daily_login_streak_restore` | 5 |

## `PvZCards.HeroReward` (2 enums)

### `HeroRewardChoice`  *(TypeDefIndex 2821)*

| name | value |
|---|--:|
| `PlantHero2` | 0 |
| `PlantHero3` | 1 |
| `ZombieHero2` | 2 |
| `ZombieHero3` | 3 |

### `PostNarrationDestination`  *(TypeDefIndex 2820)*

| name | value |
|---|--:|
| `HeroSelect` | 0 |
| `PlantHeadquartersIntro` | 1 |

## `PvZCards.Inventory` (1 enums)

### `Currency`  *(TypeDefIndex 6212)*

| name | value |
|---|--:|
| `gold` | 0 |
| `WildShard` | 1 |
| `BrainFreezeShard` | 2 |
| `ChomperShard` | 3 |
| `CitronShard` | 4 |
| `CptBrainzShard` | 5 |
| `DiscoShard` | 6 |
| `GargantuarShard` | 7 |
| `Grass_KnucklesShard` | 8 |
| `ImpfinityShard` | 9 |
| `NeptunaShard` | 10 |
| `NightCapShard` | 11 |
| `PenelopeaShard` | 12 |
| `ProfessorShard` | 13 |
| `RoseShard` | 14 |
| `ScortchwoodShard` | 15 |
| `SpudowShard` | 16 |
| `WallKnightShard` | 17 |
| `WitchShard` | 18 |
| `ZMechShard` | 19 |
| `CyborgShard` | 20 |
| `SunflowerShard` | 21 |
| `Spark` | 22 |
| `Gem` | 23 |

## `PvZCards.LandingScreen` (1 enums)

### `LandingScreenEventSortOrder`  *(TypeDefIndex 2879)*

| name | value |
|---|--:|
| `QuestUnlockTutorial` | 0 |
| `KeepHudDisabled` | 1 |
| `OtherTutorials` | 2 |
| `StoreTutorial` | 3 |
| `ZombieHqUnlock` | 4 |
| `QuestPopups` | 5 |
| `ScheduledEventDisplay` | 6 |
| `PvpSeasonReset` | 7 |
| `DraperInterstitial` | 8 |

## `PvZCards.Matchmaking` (1 enums)

### `FasterMatchmakingFaction`  *(TypeDefIndex 2619)*

| name | value |
|---|--:|
| `None` | 0 |
| `Plant` | 1 |
| `Zombie` | 2 |

## `PvZCards.Multiplayer.Config` (2 enums)

### `BuildEnvironment`  *(TypeDefIndex 16300)*

| name | value |
|---|--:|
| `Dev` | 0 |
| `Test` | 1 |
| `Stage` | 2 |
| `Prod` | 3 |
| `LoadTest` | 4 |

### `MpCompleteUrl`  *(TypeDefIndex 16303)*

| name | value |
|---|--:|
| `ImHere` | 0 |
| `MatchMaking` | 1 |
| `MatchPoll` | 2 |
| `PvPPlay` | 3 |
| `InitGame` | 4 |
| `PvPPoll` | 5 |
| `GameComplete` | 6 |
| `BaseUrl` | 7 |
| `CreateChallenge` | 8 |
| `Ranks` | 9 |
| `CancelMatchMaking` | 10 |

## `PvZCards.Multiplayer.Networking` (2 enums)

### `Connectivity`  *(TypeDefIndex 16239)*

| name | value |
|---|--:|
| `Connected` | 0 |
| `NotConnected` | 1 |
| `Inactive` | 2 |

### `WebRequestProcessorNamedType`  *(TypeDefIndex 16284)*

| name | value |
|---|--:|
| `AuthChecked` | 0 |

## `PvZCards.PVE` (1 enums)

### `PveMode`  *(TypeDefIndex 2276)*

| name | value |
|---|--:|
| `NotPve` | 0 |
| `Test` | 1 |
| `FTUE` | 2 |
| `Progression` | 3 |
| `DailyBattle` | 4 |
| `Cheat` | 5 |
| `RandomBattle` | 6 |

## `PvZCards.PVP` (4 enums)

### `CompletePvpSeasonRequestResult`  *(TypeDefIndex 2120)*

| name | value |
|---|--:|
| `ShowSeasonTransition` | 0 |
| `NoSeasonTransitionToShow` | 1 |

### `PvpRewardsState`  *(TypeDefIndex 2177)*

| name | value |
|---|--:|
| `None` | 0 |
| `FetchingData` | 1 |
| `Updated` | 2 |
| `Error` | 3 |

### `PvpSeasonEvent`  *(TypeDefIndex 2211)*

| name | value |
|---|--:|
| `UpdateRequested` | 0 |
| `UnacknowledgedSeasonAvailable` | 1 |
| `NotInCountdownWindow` | 2 |
| `InCountdownWindow` | 3 |
| `CurrentSeasonReadyToComplete` | 4 |
| `SeasonCompleteAcknowledged` | 5 |
| `MatchmakingSeasonExpired` | 6 |
| `SeasonCompletionStarted` | 7 |
| `SeasonCompletionEnded` | 8 |

### `PvpSeasonState`  *(TypeDefIndex 2210)*

| name | value |
|---|--:|
| `Init` | 0 |
| `DeterminingState` | 1 |
| `NeedToShowReset` | 2 |
| `WaitingForCountdown` | 3 |
| `CountingDown` | 4 |
| `ReadyToComplete` | 5 |
| `Completing` | 6 |

## `PvZCards.PrivacyPolicy` (1 enums)

### `PrivacyPolicyKey`  *(TypeDefIndex 2457)*

| name | value |
|---|--:|
| `UsageSharing` | 0 |
| `ThirdParty` | 1 |
| `EATargeted` | 2 |

## `PvZCards.Progression` (1 enums)

### `PlantFtueProgress`  *(TypeDefIndex 2435)*

| name | value |
|---|--:|
| `InNode1` | 0 |
| `InDeckbuilderTutorial1` | 1 |
| `InNode2` | 2 |
| `InDeckbuilderTutorial2` | 3 |
| `InNode3` | 4 |
| `InDeckbuilderTutorial3` | 5 |
| `Done` | 6 |

## `PvZCards.Purchasing` (8 enums)

### `CheatPurchaseFlowResult`  *(TypeDefIndex 2287)*

| name | value |
|---|--:|
| `SkuValidationInvalidSku` | 0 |
| `SkuValidationError` | 1 |
| `PlatformDeferred` | 2 |
| `PlatformUserCancel` | 3 |
| `PlatformError` | 4 |
| `FulfillRvsFailure` | 5 |
| `FulfillInsufficientCurrency` | 6 |
| `FulfillAlreadyGranted` | 7 |
| `FulfillError` | 8 |
| `Success` | 9 |

### `PlatformPurchaseServiceResults`  *(TypeDefIndex 2329)*

| name | value |
|---|--:|
| `AskToBuy` | 0 |
| `UserCancel` | 1 |
| `ErrorNotCharged` | 2 |
| `ErrorDoNotMessage` | 3 |
| `Error` | 4 |
| `Success` | 5 |

### `PurchaseEvent`  *(TypeDefIndex 2353)*

| name | value |
|---|--:|
| `PurchaseRequested` | 0 |
| `FulfillmentRequested` | 1 |
| `ServiceFailure` | 2 |
| `ServiceSuccess` | 3 |
| `IsPlatformPurchase` | 4 |
| `IsPurchaseWithGems` | 5 |

### `PurchaseFulfillmentServiceResults`  *(TypeDefIndex 2337)*

| name | value |
|---|--:|
| `Unknown` | 0 |
| `RvsFail` | 1 |
| `InsufficientFunds` | 2 |
| `PurchasingAlreadyGranted` | 3 |
| `Error` | 4 |
| `Success` | 5 |

### `PurchasePlatform`  *(TypeDefIndex 2328)*

| name | value |
|---|--:|
| `iOS` | 0 |
| `Android` | 1 |
| `Cheat` | 2 |
| `Origin` | 3 |

### `PurchaseRequestResult`  *(TypeDefIndex 2349)*

| name | value |
|---|--:|
| `None` | 0 |
| `Unknown` | 1 |
| `SkuValidationInvalid` | 2 |
| `SkuValidationError` | 3 |
| `PlatformPurchaseDelayedApproval` | 4 |
| `PlatformPurchaseError` | 5 |
| `PlatformPurchaseUserCancel` | 6 |
| `PlatformPurchaseErrorNotMessagedByGame` | 7 |
| `PlatformPurchaseErrorNotCharged` | 8 |
| `FulfillRvsFailure` | 9 |
| `FulfillInsufficientCurrency` | 10 |
| `FulfillAlreadyGranted` | 11 |
| `FulfillError` | 12 |
| `Success` | 13 |

### `PurchaseState`  *(TypeDefIndex 2352)*

| name | value |
|---|--:|
| `Inactive` | 0 |
| `GetCatalogInfo` | 1 |
| `ValidatingSku` | 2 |
| `DeterminePurchaseType` | 3 |
| `PlatformPurchase` | 4 |
| `GetCatalogInfoForFulfillment` | 5 |
| `PreFulfillment` | 6 |
| `Fulfillment` | 7 |
| `DetermineMetricsType` | 8 |
| `SendPurchaseWithGemsMetrics` | 9 |
| `SendGemPurchaseWithCashMetrics` | 10 |
| `SendPackPurchaseWithCashMetrics` | 11 |

### `SkuValidationServiceResults`  *(TypeDefIndex 2373)*

| name | value |
|---|--:|
| `Valid` | 0 |
| `Invalid` | 1 |
| `Error` | 2 |

## `PvZCards.Quests` (8 enums)

### `CraftTransactionType`  *(TypeDefIndex 1890)*

| name | value |
|---|--:|
| `Buy` | 0 |
| `Sell` | 1 |

### `QuestChainStatus`  *(TypeDefIndex 1832)*

| name | value |
|---|--:|
| `Complete` | 0 |
| `InProgress` | 1 |
| `Locked` | 2 |

### `QuestConstraintType`  *(TypeDefIndex 1882)*

| name | value |
|---|--:|
| `Source` | 0 |
| `Target` | 1 |
| `Trigger` | 2 |
| `EffectEntitySource` | 3 |
| `GameState` | 4 |

### `QuestIncrementType`  *(TypeDefIndex 1897)*

| name | value |
|---|--:|
| `SingleEvent` | 0 |
| `DamageDone` | 1 |
| `SunSpent` | 2 |
| `HealAmount` | 3 |
| `SunGained` | 4 |
| `AmtCrafted` | 5 |

### `QuestRewardType`  *(TypeDefIndex 1967)*

| name | value |
|---|--:|
| `None` | 0 |
| `Gems` | 1 |
| `Silver` | 2 |
| `Sparks` | 3 |
| `Pack` | 4 |
| `Card` | 5 |

### `QuestRollupQuestPresentationType`  *(TypeDefIndex 1972)*

| name | value |
|---|--:|
| `NewQuest` | 0 |
| `Progressed` | 1 |
| `Completed` | 2 |

### `QuestTriggerType`  *(TypeDefIndex 2004)*

| name | value |
|---|--:|
| `PlayCard` | 0 |
| `DoDamage` | 1 |
| `BonusAttack` | 2 |
| `BuffCard` | 3 |
| `MoveCard` | 4 |
| `GrantAbility` | 5 |
| `CreateCard` | 6 |
| `DestroyCard` | 7 |
| `DrawCard` | 8 |
| `BounceCard` | 9 |
| `FreezeCard` | 10 |
| `Heal` | 11 |
| `RevealFromGravestone` | 12 |
| `StartTurn` | 14 |
| `EndPlayerPhase` | 15 |
| `FrenzyAttack` | 16 |
| `LaneCombatStart` | 18 |
| `GainExtraSun` | 19 |
| `BuyCard` | 20 |
| `KillCard` | 21 |
| `WinGame` | 22 |
| `SellCard` | 23 |

### `ToastIdSource`  *(TypeDefIndex 2028)*

| name | value |
|---|--:|
| `FeatureUnlock` | 0 |
| `DeckWidgetInvalid` | 1 |

## `PvZCards.Quests.QuestConstraints` (1 enums)

### `GameType`  *(TypeDefIndex 2085)*

| name | value |
|---|--:|
| `Any` | 0 |
| `PvE` | 1 |
| `PvP` | 2 |

## `PvZCards.SettingsScreen` (1 enums)

### `SettingsTab`  *(TypeDefIndex 1730)*

| name | value |
|---|--:|
| `Default` | 0 |
| `Settings` | 1 |
| `Help` | 2 |

## `PvZCards.Social` (3 enums)

### `HeroesPresenceKey`  *(TypeDefIndex 4244)*

| name | value |
|---|--:|
| `KeepPreviousPresence` | 0 |
| `Default` | 1 |
| `Busy` | 2 |
| `Collection` | 3 |
| `DeckEditor` | 4 |
| `GamePve` | 5 |
| `GamePvePlant` | 6 |
| `GamePveZombie` | 7 |
| `GamePvp` | 8 |
| `GamePvpCasual` | 9 |
| `GamePvpRanked` | 10 |
| `LandingScreen` | 11 |
| `PvpSelectScreen` | 12 |
| `OpeningPacks` | 13 |
| `EndOfGameRewardFlow` | 14 |
| `Store` | 15 |
| `EventHub` | 16 |

### `SocialIdentityReconciliationStatus`  *(TypeDefIndex 4269)*

| name | value |
|---|--:|
| `ConflictResolvedNoRestartNeeded` | 0 |
| `ConflictResolvedRestartNeeded` | 1 |

### `SocialLoginStatus`  *(TypeDefIndex 4255)*

| name | value |
|---|--:|
| `None` | 0 |
| `LaunchedExternalAuthenticator` | 1 |
| `ExternalAuthenticatorFailedOrClosed` | 2 |
| `ConnectionLostAuthenticatorsClosed` | 3 |
| `UserLoggedInPotentialConflicts` | 4 |
| `UserLoggedInNoConflictsFound` | 5 |
| `IdentityConflictResolutionScreenShown` | 6 |
| `ConflictResolvedNoRestartNeeded` | 7 |
| `ConflictResolvedRestartNeeded` | 8 |

## `PvZCards.Store` (5 enums)

### `JsonTargetedOffersServiceNamedInjection`  *(TypeDefIndex 1543)*

| name | value |
|---|--:|
| `PersistentStoreDatasource` | 0 |
| `FeaturedOffersDatasource` | 1 |

### `OfferExpirationType`  *(TypeDefIndex 1605)*

| name | value |
|---|--:|
| `None` | 0 |
| `FromUserInstall` | 1 |
| `FixedTime` | 2 |

### `PlatformStoreServiceNamedInjection`  *(TypeDefIndex 1592)*

| name | value |
|---|--:|
| `CheatPlatformStoreService` | 0 |
| `ReleasePlatformStoreService` | 1 |

### `StoreButtonViewBadgeCountCategory`  *(TypeDefIndex 1624)*

| name | value |
|---|--:|
| `UnfulfilledPurchases` | 0 |
| `UnopenedPacks` | 1 |
| `NewOffers` | 2 |
| `AvailableSilverPacks` | 3 |

### `StoreTab`  *(TypeDefIndex 1586)*

| name | value |
|---|--:|
| `Packs` | 0 |
| `Gems` | 1 |
| `Featured` | 2 |
| `Default` | 3 |

## `PvZCards.Tutorial` (1 enums)

### `TutorialSeenLimit`  *(TypeDefIndex 1443)*

| name | value |
|---|--:|
| `OncePerUser` | 0 |
| `OncePerSession` | 1 |
| `Unlimited` | 2 |

## `PvZCards.UI` (2 enums)

### `BackButtonBehaviour`  *(TypeDefIndex 3544)*

| name | value |
|---|--:|
| `DoNothing` | 0 |
| `MinimizeApp` | 1 |
| `GoToPreviousScreen` | 2 |
| `ClosePopUp` | 3 |
| `PromptDeleteDialog` | 4 |
| `PopToScreenName` | 5 |
| `ConcedeGame` | 6 |
| `PopScreenByName` | 7 |

### `FancyPanelItemVisibility`  *(TypeDefIndex 3629)*

| name | value |
|---|--:|
| `Unknown` | 0 |
| `NotVisible` | 1 |
| `Visible` | 2 |

## `PvZCards.Utilities` (1 enums)

### `LocalizationMapperType`  *(TypeDefIndex 4934)*

| name | value |
|---|--:|
| `EnglishCheatsOnly` | 0 |

