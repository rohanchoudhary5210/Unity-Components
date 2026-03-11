# Refactored Scripts Reference

This file explains every refactored C# script found inside the subfolders of this directory. Each script section answers the same questions:

- key variables
- key methods
- what triggers it
- what it triggers next

## How Communication Works In This Refactor

### What `Action` means

`Action` is a C# delegate type. A delegate stores one or more methods so they can be called later.

- `Action` means no parameters, no return value.
- `Action<int>` means one integer parameter.
- `Action<int, int>` means two integer parameters.
- `Action<Vector3, float>` means a `Vector3` and a `float`.

If a script exposes an `Action<Vector3, float>`, another method can be attached to it as long as it accepts those exact parameter types.

### What an `event Action` is

An `event` is a controlled broadcast built on a delegate. In this project, `GameEventBus` exposes `event Action...` members so one script can announce that something happened and many other scripts can react.

Example:

- `OnScoreUpdated` is `event Action<int, int>`

That means:

1. another script can subscribe a handler,
2. a gameplay script raises the event when score changes,
3. all listeners receive the new score values.

### Event bus vs direct callback

This project uses both patterns:

- `event Action...`
  For broad notifications that many systems may care about.
- `Action...` without `event`
  For direct callbacks between a small number of scripts.

Examples:

- `GameEventBus.OnScoreUpdated` is a broadcast event.
- `SwipeInputController.OnSwipeCompleted` is a direct callback.

### Why `OnEnable` and `OnDisable` matter

Most listener scripts subscribe in `OnEnable` and unsubscribe in `OnDisable`.

That keeps subscriptions tied to the object's active lifetime and prevents:

- duplicate subscriptions,
- destroyed objects still receiving events,
- UI panels reacting while disabled.

### What trigger methods do

`GameEventBus` wraps event invocation in methods like:

- `TriggerScoreUpdated(...)`
- `TriggerTurnChanged(...)`
- `TriggerBagScored(...)`

Those methods are just named entry points for raising events. They make the code read like "what happened" instead of exposing raw event invocation everywhere.

### Typical runtime flow

1. Player swipes.
2. `SwipeInputController` calculates direction and speed.
3. `OnSwipeCompleted` passes that data to a throw method.
4. `ThrowController` applies physics.
5. `SandbagLogic` observes collisions and stability.
6. `SandbagLogic` raises `TriggerBagScored(this)`.
7. A mode controller handles scoring and turn logic.
8. That controller raises score, turn, timer, or round events.
9. UI scripts update because they are subscribed already.

## Core

### `Core/GameStateManager.cs`
Purpose: global pause, resume, and game-over state controller.

- Key variables:
  `Instance` is the singleton reference.
  `IsPaused` stores whether gameplay is currently paused.
- Key methods:
  `PauseGame()` sets `Time.timeScale = 0` and broadcasts `"Paused"`.
  `ResumeGame()` restores `Time.timeScale = 1` and broadcasts `"Playing"`.
  `TriggerGameOver()` broadcasts `"GameOver"`.
- What triggers it:
  Usually UI buttons, menu scripts, or future gameplay systems call these public methods.
- What it triggers next:
  It changes Unity time flow and raises `GameEventBus.TriggerGameStateChanged(...)`, which any UI or system listener can react to.

### `Core/PowerupManager.cs`
Purpose: placeholder manager for timed single-player powerup effects.

- Key variables:
  `_singlePlayerController` stores the local reference used to affect time-based gameplay.
- Key methods:
  `ActivateAddTimePowerup()` currently routes through a timed placeholder path.
  `ActivateWindStopPowerup()` currently adds time through `SinglePlayerModeController`.
  `ActivateTrajectoryPowerup()` starts a timed placeholder for trajectory help.
  `TrajectoryTimer()` and `WindModifier()` are async duration helpers.
- What triggers it:
  Powerup buttons or shop systems are expected to call these public methods.
- What it triggers next:
  Right now it mostly affects timer state through `_singlePlayerController.AddTime(...)` and provides the extension point where future events or gameplay changes would be raised.

### `Core/RoundManager.cs`
Purpose: tiny round-state container.

- Key variables:
  `CurrentRound` stores the current round number.
- Key methods:
  `IncrementRound()` advances the round count.
  `ResetRounds()` returns the count to round one.
- What triggers it:
  Match-flow controllers call it when a round starts or resets.
- What it triggers next:
  It does not raise events itself. It only updates round state for other scripts to read.

### `Core/SandbagLogic.cs`
Purpose: convert bag physics interactions into gameplay result flags.

- Key variables:
  `stabilityThreshold` defines how much movement still counts as stable.
  `stableDuration` defines how long the bag must remain stable.
  `HasLandedOnBoard`, `HasHitGround`, and `HasScoredInHole` store the bag result.
  `_waitOneTenth` caches a wait object for the coroutine loop.
- Key methods:
  `OnTriggerEnter(...)` detects hole scoring.
  `OnCollisionEnter(...)` detects board or ground contact.
  `StartStabilityCheck()` starts the settle-check coroutine.
  `CheckIfStable()` waits until the bag stops moving, then announces the result.
- What triggers it:
  Unity physics triggers `OnTriggerEnter` and `OnCollisionEnter`.
  Other scripts call `StartStabilityCheck()` after a throw or spawn cycle begins.
- What it triggers next:
  It raises `GameEventBus.TriggerBagScored(this)`, sending the full bag result object to mode controllers.

### `Core/ScoreManager.cs`
Purpose: store player scores without any UI or flow logic.

- Key variables:
  `Player1Score` and `Player2Score` store current totals.
- Key methods:
  `AddScoreToPlayer(int playerId, int amount)` updates one player's score.
  `ResetScores()` clears both totals.
- What triggers it:
  Mode controllers call it after deciding how many points a throw is worth.
- What it triggers next:
  Nothing directly. Controllers usually read the new totals and then raise score update events.

### `Core/ThrowController.cs`
Purpose: apply throw force and manage bag rigidbody state.

- Key variables:
  `_rb` stores the required `Rigidbody`.
- Key methods:
  `ExecuteThrow(Vector3 direction, float speed)` converts swipe data into an impulse.
  `ResetPhysics()` clears velocity and disables gravity.
- What triggers it:
  Usually `SwipeInputController.OnSwipeCompleted` is wired to `ExecuteThrow(...)`.
- What it triggers next:
  It does not raise events. It changes rigidbody motion, which then leads to physics callbacks in `SandbagLogic`.

### `Core/TurnManager.cs`
Purpose: track active player and total throws in the round.

- Key variables:
  `MAX_BAGS_PER_ROUND` defines each player's bag count.
  `CurrentPlayer` stores which player is active.
  `BagsThrownThisRound` stores total throws across the round.
- Key methods:
  `RecordThrow()` increments total throws.
  `SwitchTurn()` flips active player between 0 and 1.
  `ResetRound()` clears counters and resets turn order.
- What triggers it:
  Mode controllers call it after a throw resolves or a new round begins.
- What it triggers next:
  Nothing directly. Controllers use its updated state to decide what events to raise next.

## Customization

### `Customization/BagSkinApplier.cs`
Purpose: react to bag-skin updates and apply bag materials.

- Key variables:
  `_meshRenderer` stores the renderer to change.
  `_overrideLocked` prevents later global events from overriding a forced player-specific skin.
- Key methods:
  `OnEnable()` subscribes to `GameEventBus.OnBagSkinChanged`.
  `OnDisable()` unsubscribes from that event.
  `ApplySkin(...)` applies a skin unless overrides are locked.
  `ApplySkinLocked(...)` force-applies a skin and blocks later global changes.
  `ApplySkinInternal(...)` sets the material.
- What triggers it:
  `CustomizationManager` or other systems raise `GameEventBus.TriggerBagSkinChanged(...)`.
  `SpawnManager` can call `ApplySkinLocked(...)` directly.
- What it triggers next:
  It updates the mesh material on the bag. It does not raise further events.

### `Customization/BoardSkinApplier.cs`
Purpose: react to board-skin updates and apply board materials.

- Key variables:
  `_meshRenderer` stores the renderer to change.
- Key methods:
  `OnEnable()` subscribes to `GameEventBus.OnBoardSkinChanged`.
  `OnDisable()` unsubscribes.
  `ApplySkin(...)` applies the incoming board material.
- What triggers it:
  `CustomizationManager` raises `GameEventBus.TriggerBoardSkinChanged(...)`.
- What it triggers next:
  It updates the board material only.

### `Customization/CustomizationManager.cs`
Purpose: manage equipped skins, equipped map, and simple offline map purchasing.

- Key variables:
  `_skinDatabase` and `_mapDatabase` provide lookup data.
  `enableMultiplayerSkins` controls per-player bag-skin support.
  `_equippedBagSkinId`, `_equippedBoardSkinId`, and `_equippedMapId` store current equipped selections.
  `BagSkinKeyP1`, `BagSkinKeyP2`, and `LegacyBagSkinKey` define save keys.
- Key methods:
  `LoadEquippedSkins()` reads saved values from `PlayerPrefs`.
  `ApplyEquippedSkins()` resolves assets and pushes them into the scene.
  `EquipBagSkin(...)`, `EquipBoardSkin(...)`, and `EquipMap(...)` save and apply new selections.
  `EquipBagSkinForPlayer(...)` stores a player-specific bag skin.
  `GetBagSkinForPlayer(...)` resolves a player's bag skin.
  `TryPurchaseMap(...)` checks unlock status and coin cost.
- What triggers it:
  Unity calls `Start()` which calls `LoadEquippedSkins()`.
  Menus, shop buttons, or scene setup code call the public equip and purchase methods.
- What it triggers next:
  It raises bag and board skin events through `GameEventBus`.
  It calls `MapManager.Instance.EnableMap(...)`.
  It writes equipped and unlocked values into `PlayerPrefs`.

### `Customization/MapDatabase.cs`
Purpose: data registry for available maps.

- Key variables:
  `AvailableMaps` stores all configured `MapData` assets.
- Key methods:
  `GetMapByID(int id)` finds the requested map asset.
- What triggers it:
  `CustomizationManager` and future selection systems query it.
- What it triggers next:
  Nothing directly. It returns `MapData` to the caller.

### `Customization/SkinDatabase.cs`
Purpose: data registry for available bag and board skins.

- Key variables:
  `AvailableBagSkins` stores all bag cosmetics.
  `AvailableBoardSkins` stores all board cosmetics.
- Key methods:
  `GetBagSkinByID(...)` resolves bag data.
  `GetBoardSkinByID(...)` resolves board data.
- What triggers it:
  `CustomizationManager` and `SpawnManager` query it.
- What it triggers next:
  Nothing directly. It returns the requested skin data.

## Environment

### `Environment/MapManager.cs`
Purpose: activate the currently selected map and disable the rest.

- Key variables:
  `Instance` is the singleton reference.
  `maps` stores all map root GameObjects.
  `CurrentMapId` stores the active map index.
- Key methods:
  `EnableMap(int idx)` activates one map and deactivates the others.
  `DisableAllMaps()` turns all maps off.
- What triggers it:
  `CustomizationManager` and mode controllers call `EnableMap(...)`.
- What it triggers next:
  It changes scene object activation state. It does not raise events.

### `Environment/SpawnManager_New.cs`
Purpose: spawn sandbags and apply the correct player cosmetic at spawn time.

- Key variables:
  `Instance` is the singleton reference.
  `sandbagPrefab` is the bag object to instantiate.
  `playerSpawnPoints` stores per-player spawn transforms.
  `skinDatabase` is used for fallback skin resolution.
- Key methods:
  `SpawnSandbag(int playerId, int mapId = 0)` instantiates a bag and skins it.
  `SpawnSandbag()` is a single-player convenience overload.
  `ClearSandbags()` and `ClearSandbags(int playerId)` remove existing bags and optionally respawn.
  `DestroyAllSandbags()` finds and destroys all current bag objects.
  `ApplyPlayerSkin(...)` resolves and applies the correct bag skin.
- What triggers it:
  Network services or gameplay flow code call the spawn methods.
- What it triggers next:
  It creates a bag GameObject in the scene and applies appearance data.
  If the bag contains `BagSkinApplier`, that component updates the material immediately.

## Events

### `Events/GameEventBus.cs`
Purpose: central event hub for decoupled gameplay, UI, and customization communication.

- Key variables:
  `OnScoreUpdated`, `OnTurnChanged`, `OnBagScored`, `OnRoundEnded`, `OnMatchWon`, `OnTimerUpdated`, `OnTimerExpired`, `OnCoinsUpdated`, `OnBagsRemainingUpdated`, `OnRoundResult`, `OnPointsUpdated`, `OnGameStateChanged`, `OnBagSkinChanged`, and `OnBoardSkinChanged` are the shared broadcast channels.
- Key methods:
  Every `Trigger...` method safely invokes its matching event with the correct payload.
- What triggers it:
  Controllers, gameplay scripts, customization scripts, and managers call the trigger methods whenever state changes.
- What it triggers next:
  Every subscribed listener method on active scripts runs. This is how score UI, turn UI, result UI, skin appliers, and other systems update without direct references.

## GameModes

### `GameModes/GameModeManager.cs`
Purpose: bootstrap the scene into single-player or local multiplayer mode.

- Key variables:
  `Instance` is the singleton reference.
  `_isMultiplayerMode` selects which controller to use.
  `_multiplayerController` and `_singlePlayerController` are the mode-specific scene controllers.
- Key methods:
  `Start()` creates local service implementations, registers the network service, enables one mode controller, disables the other, and calls the selected controller's `Initialize(...)`.
- What triggers it:
  Unity calls `Start()` when the scene begins.
- What it triggers next:
  It initializes services, sets `NetworkServiceRegistry.Current`, and starts either the multiplayer or single-player flow.

### `GameModes/LocalMultiplayerModeController.cs`
Purpose: run local hot-seat multiplayer round flow.

- Key variables:
  `_networkService` is the current network/spawn bridge.
  `_turnManager` stores turn state.
  `_scoreManager` stores point totals.
  `_mapId` stores the selected map.
  `_player1Throws` and `_player2Throws` track per-player bag counts.
  `_player1Points` and `_player2Points` build throw-history strings for UI.
- Key methods:
  `Initialize(...)` prepares state, loads map, sends loadouts, subscribes to bag results, and starts the round.
  `StartNewRound()` resets counters, updates UI state, and spawns the first bag.
  `HandleBagScored(...)` calculates points from the `SandbagLogic` payload and advances match flow.
  `EndRound()` raises round-end events.
  `SendInitialLoadouts()` pushes per-player bag skin IDs into the network service.
  `GetBagsRemainingForCurrentPlayer()` calculates the current player's remaining throws.
- What triggers it:
  `GameModeManager` calls `Initialize(...)`.
  `GameEventBus.OnBagScored` triggers `HandleBagScored(...)`.
- What it triggers next:
  It updates `ScoreManager` and `TurnManager`.
  It raises `TriggerPointsUpdated(...)`, `TriggerScoreUpdated(...)`, `TriggerTurnChanged(...)`, `TriggerBagsRemainingUpdated(...)`, `TriggerRoundEnded()`, and `TriggerRoundResult(...)`.
  It asks `_networkService.SpawnNetworkedBag(...)` for the next bag.

### `GameModes/SinglePlayerModeController.cs`
Purpose: run timed single-player gameplay.

- Key variables:
  `_networkService` is the current network/spawn bridge.
  `_turnManager` stores bag-count progress.
  `_scoreManager` stores score totals.
  `startingTime` is the configured match time.
  `_timeRemaining` stores live time left.
  `_isTimerRunning` controls whether the timer continues to tick.
- Key methods:
  `Initialize(...)` sets up services, score state, turn state, timer state, and the bag-scored subscription.
  `Update()` keeps the timer running.
  `HandleTimer()` reduces time and publishes timer events.
  `OnTimeUp()` creates the final `MatchResult`.
  `AddTime(float amount)` increases remaining time and republishes timer state.
  `HandleBagScored(...)` awards points, gives bonus time for hole shots, and spawns the next bag.
- What triggers it:
  `GameModeManager` calls `Initialize(...)`.
  Unity calls `Update()` every frame.
  `GameEventBus.OnBagScored` triggers `HandleBagScored(...)`.
- What it triggers next:
  It raises `TriggerTimerUpdated(...)`, `TriggerTimerExpired()`, `TriggerScoreUpdated(...)`, and `TriggerMatchWon(...)`.
  It asks `_networkService.SpawnNetworkedBag(0)` for the next throw.

## Input

### `Input/InputHandler.cs`
Purpose: abstract base class for future input providers.

- Key variables:
  None in the base class.
- Key methods:
  `IsInputActive()` is the abstract contract subclasses must implement.
- What triggers it:
  Other systems would call it to ask whether an input source is currently active.
- What it triggers next:
  Nothing directly. It defines a shared API shape.

### `Input/SwipeInputController.cs`
Purpose: convert touch or mouse dragging into throw direction and speed.

- Key variables:
  `MinSwipeDist` defines the minimum swipe distance.
  `horizontalSensitivity` and `verticalSensitivity` control direction conversion.
  `startTime`, `endTime`, `swipeDistance`, and `swipeTime` store swipe metrics.
  `startPos` and `endPos` store gesture positions.
  `_isHolding` stores whether input is currently active.
  `_mainCamera` is used to convert screen movement into world throw direction.
  `OnSwipeCompleted` is the direct callback for finished swipe data.
  `OnHolding` is the direct callback for live hold position.
- Key methods:
  `Update()` chooses between mouse and touch paths.
  `HandleMouseInput()` processes editor and standalone mouse input.
  `HandleTouchInput()` processes mobile touch input.
  `ProcessSwipe()` validates distance and time, then computes speed and direction.
  `CalculateDirection()` converts the swipe into a 3D throw vector.
- What triggers it:
  Unity calls `Update()` every frame.
  Mouse or touch input triggers the internal handlers.
- What it triggers next:
  It invokes `OnHolding(...)` while dragging.
  It invokes `OnSwipeCompleted(direction, speed)` when the swipe ends successfully.

## Models

### `Models/BagSkinData.cs`
Purpose: data asset describing one bag cosmetic.

- Key variables:
  `SkinID`, `SkinName`, `BagMaterial`, `UnlockCostCoins`, and `IsDefault`.
- Key methods:
  None. It is a pure data `ScriptableObject`.
- What triggers it:
  Databases and customization systems load or reference it.
- What it triggers next:
  Nothing itself. Other scripts use its fields to apply cosmetics or pricing logic.

### `Models/BoardSkinData.cs`
Purpose: data asset describing one board cosmetic.

- Key variables:
  `SkinID`, `SkinName`, `BoardMaterial`, `UnlockCostCoins`, and `IsDefault`.
- Key methods:
  None.
- What triggers it:
  Databases and customization systems read it.
- What it triggers next:
  Nothing directly.

### `Models/MapData.cs`
Purpose: data asset describing one map entry.

- Key variables:
  `MapID`, `MapName`, `UnlockCostCoins`, and `IsDefault`.
- Key methods:
  None.
- What triggers it:
  Map databases and purchase/equip logic read it.
- What it triggers next:
  Nothing directly.

### `Models/MatchResult.cs`
Purpose: result payload describing a completed match.

- Key variables:
  `Player1Score`, `Player2Score`, `WinnerID`, `TotalRoundsPlayed`, and `CoinsEarned`.
- Key methods:
  The constructor packages all match result values into one object.
- What triggers it:
  Mode controllers create it when a match ends.
- What it triggers next:
  It is passed into events like `GameEventBus.TriggerMatchWon(...)` or backend submission methods.

### `Models/PlayerData.cs`
Purpose: lightweight player profile model.

- Key variables:
  `PlayerName`, `Coins`, `EquippedBagSkinId`, and `EquippedBoardSkinId`.
- Key methods:
  The constructor assigns guest/default values.
- What triggers it:
  Profile or save systems create and update it.
- What it triggers next:
  Nothing directly. It acts as state consumed by service or UI systems.

### `Models/PlayerLoadout.cs`
Purpose: compact payload for player-specific bag appearance.

- Key variables:
  `BagSkinId` stores the equipped bag skin for that player.
- Key methods:
  The constructor stores the chosen bag skin ID.
- What triggers it:
  Multiplayer and network-related systems create it before spawning bags.
- What it triggers next:
  It is passed into `INetworkService.SendPlayerLoadout(...)` and later read during spawn.

## Services

### `Services/NetworkServiceRegistry.cs`
Purpose: global holder for the currently active network service implementation.

- Key variables:
  `Current` stores the active `INetworkService`.
- Key methods:
  None beyond the static property.
- What triggers it:
  `GameModeManager` sets it during startup.
  Other systems read it when they need active network/loadout state.
- What it triggers next:
  Nothing directly. It provides shared access to the chosen service.

### `Services/Interfaces/IAchievementService.cs`
Purpose: interface contract for achievement systems.

- Key variables:
  None. It is an interface.
- Key methods:
  `UnlockAchievement(...)`
  `IncrementAchievement(...)`
- What triggers it:
  Gameplay, backend, or meta-progression code would call these methods on an implementation.
- What it triggers next:
  Whatever the chosen implementation does, such as a platform SDK update or local save change.

### `Services/Interfaces/IBackendService.cs`
Purpose: interface contract for backend login and result submission.

- Key variables:
  `IsLoggedIn` exposes login state.
- Key methods:
  `Initialize()`
  `AuthenticateUser()`
  `SubmitMatchResult(...)`
- What triggers it:
  Startup code and match-flow code call these methods on an implementation.
- What it triggers next:
  Implementation-specific backend behavior such as authentication or saving results.

### `Services/Interfaces/ILeaderboardService.cs`
Purpose: interface contract for leaderboard operations.

- Key variables:
  None.
- Key methods:
  `SubmitScore(...)`
  `FetchLeaderboard(...)`
- What triggers it:
  Result screens or online systems would call an implementation.
- What it triggers next:
  Implementation-specific leaderboard behavior.

### `Services/Interfaces/INetworkService.cs`
Purpose: interface contract for bag spawning and player loadout transfer.

- Key variables:
  `IsConnected` exposes connection state.
- Key methods:
  `Initialize()`
  `SpawnNetworkedBag(int playerId)`
  `SendPlayerLoadout(int playerId, PlayerLoadout loadout)`
  `TryGetPlayerLoadout(int playerId, out PlayerLoadout loadout)`
- What triggers it:
  Startup, mode controllers, and spawn-related code call into an implementation.
- What it triggers next:
  Implementation-specific spawning or loadout storage behavior.

### `Services/Interfaces/IPlayerProfileService.cs`
Purpose: interface contract for player progression and profile updates.

- Key variables:
  `CurrentPlayer` exposes the current player profile object.
- Key methods:
  `UpdateCoins(...)`
  `UnlockSkin(...)`
- What triggers it:
  Store, rewards, and progression systems would call these methods.
- What it triggers next:
  Implementation-specific save or profile updates.

### `Services/LocalImplementations/LocalBackendService.cs`
Purpose: offline stub implementation of `IBackendService`.

- Key variables:
  `IsLoggedIn` stores local login state.
- Key methods:
  `Initialize()` logs local backend setup.
  `AuthenticateUser()` marks the offline player as logged in.
  `SubmitMatchResult(...)` logs the result submission.
- What triggers it:
  `GameModeManager` and future backend callers use it through the interface.
- What it triggers next:
  It updates local login state and logs actions. A fuller implementation could save locally here.

### `Services/LocalImplementations/LocalNetworkService.cs`
Purpose: offline stub implementation of `INetworkService`.

- Key variables:
  `IsConnected` always reports false.
  `_loadouts` stores per-player loadouts in memory.
- Key methods:
  `Initialize()` logs offline network setup.
  `SpawnNetworkedBag(...)` logs a local spawn request.
  `SendPlayerLoadout(...)` stores loadout data by player.
  `TryGetPlayerLoadout(...)` returns stored loadout data.
- What triggers it:
  `GameModeManager` initializes it.
  Mode controllers call it to store loadouts and request the next bag.
- What it triggers next:
  It updates its loadout dictionary and logs spawn actions. Future real implementations would actually network-spawn here.

## UI

### `UI/BagsRemainingUI.cs`
Purpose: display how many bags are left for the active player.

- Key variables:
  `bagsRemainingText` is the target text UI.
  `prefix` is the label prefix shown before the number.
- Key methods:
  `OnEnable()` subscribes to `OnBagsRemainingUpdated`.
  `OnDisable()` unsubscribes.
  `UpdateBagsRemaining(int count)` redraws the label.
- What triggers it:
  `LocalMultiplayerModeController` raises `GameEventBus.TriggerBagsRemainingUpdated(...)`.
- What it triggers next:
  It updates the visible UI text only.

### `UI/ButtonSfxHook.cs`
Purpose: play click sounds for a set of buttons.

- Key variables:
  `buttons` stores the target buttons.
  `sfxName` stores the sound key to play.
- Key methods:
  `OnEnable()` attaches `PlayClick()` to each button.
  `OnDisable()` removes the listeners.
  `PlayClick()` routes the click into `AudioManager`.
- What triggers it:
  UI button clicks invoke the registered listeners.
- What it triggers next:
  It asks `AudioManager.Instance.PlaySfx(sfxName)` to play a sound.

### `UI/CanvasGroupFader.cs`
Purpose: reusable fade helper for `CanvasGroup` UI panels.

- Key variables:
  `fadeDuration` defines how long the fade takes.
- Key methods:
  `FadeCanvasGroup(...)` starts the fade coroutine.
  `FadeRoutine(...)` interpolates alpha over time and updates interaction flags.
  `HoldFor(float seconds)` starts a delayed hold.
  `HoldSeconds(...)` waits and then nearly freezes time.
- What triggers it:
  Menu or transition scripts call the public methods.
- What it triggers next:
  It changes panel alpha, interaction state, and sometimes `Time.timeScale`.

### `UI/CanvasGroupSwitcher.cs`
Purpose: immediate show/hide utility for multiple UI panels.

- Key variables:
  `canvasGroups` stores all managed panels.
- Key methods:
  `EnableCanvasGroup(...)` shows one group and hides the rest.
  `EnableCanvasGroupAndPause(...)` shows one group and pauses time.
  `EnableCanvasGroupAndResume(...)` shows one group and resumes time.
  `PauseTime()`, `ResumeTime()`, and `SetTimeScale(...)` control Unity time.
  `EnableParticularCanvasGroup(...)`, `DisableAllCanvasGroups()`, and `DisableParticularCanvasGroup(...)` manage visibility.
- What triggers it:
  UI button methods or menu scripts call it.
- What it triggers next:
  It changes panel visibility and time scale.

### `UI/CoinsUI.cs`
Purpose: keep coin labels synchronized with current coin count.

- Key variables:
  `coinsText` stores one or more target text elements.
- Key methods:
  `OnEnable()` subscribes to `DataManager.OnCoinsUpdated` and performs an immediate refresh.
  `OnDisable()` unsubscribes.
  `UpdateCoins(int coins)` redraws all configured coin texts.
- What triggers it:
  `DataManager.OnCoinsUpdated` and the initial refresh in `OnEnable()`.
- What it triggers next:
  It updates visible UI text only.

### `UI/MultiplayerRoundResultUI.cs`
Purpose: display who won the local multiplayer round.

- Key variables:
  `resultText` is the main result label.
  `canvasGroups` stores panels that can be shown or hidden.
  `gameOverGroupName` stores the target result panel name.
- Key methods:
  `OnEnable()` subscribes to `OnRoundResult`.
  `OnDisable()` unsubscribes.
  `ShowResult(int p1Score, int p2Score)` decides the winner label.
  `ShowGroup(string name)` activates only the matching panel.
- What triggers it:
  `LocalMultiplayerModeController` raises `GameEventBus.TriggerRoundResult(...)`.
- What it triggers next:
  It updates result text and UI visibility.

### `UI/MultiplayerScoreUI.cs`
Purpose: display multiplayer totals and throw-history strings.

- Key variables:
  `player1ScoreTexts` and `player2ScoreTexts` store score text targets.
  `player1PointsText` and `player2PointsText` store point-history labels.
- Key methods:
  `OnEnable()` subscribes to `OnScoreUpdated` and `OnPointsUpdated`.
  `OnDisable()` unsubscribes.
  `UpdateScores(...)` redraws total scores.
  `UpdatePoints(...)` redraws per-throw history strings.
- What triggers it:
  `LocalMultiplayerModeController` raises score and points events.
- What it triggers next:
  It updates multiple UI text elements.

### `UI/ResultUI.cs`
Purpose: show the single-player result screen.

- Key variables:
  `_resultPanel` is the panel to reveal.
  `_winnerText` displays the winner message.
  `_coinsText` displays the coin reward.
- Key methods:
  `OnEnable()` subscribes to `OnMatchWon`.
  `OnDisable()` unsubscribes.
  `ShowResultScreen(MatchResult result)` fills the labels and shows the panel.
- What triggers it:
  `SinglePlayerModeController` raises `GameEventBus.TriggerMatchWon(...)`.
- What it triggers next:
  It activates the result panel and redraws the labels.

### `UI/SceneMusicController.cs`
Purpose: choose the correct background music for the current scene.

- Key variables:
  `streetSceneIndices`, `stadiumSceneIndices`, and `lawnroofSceneIndices` map build indices to track groups.
- Key methods:
  `Start()` reads the current scene and updates music.
  `OnDestroy()` stops scene-specific tracks.
  `UpdateMusicForScene(...)` selects and plays the correct music track.
  `StopAllTracks()` stops known gameplay tracks.
  `Contains(...)` checks whether a scene index belongs to a configured group.
- What triggers it:
  Unity calls `Start()` when the scene loads and `OnDestroy()` when the object is destroyed.
- What it triggers next:
  It calls `AudioManager` to stop or start music tracks.

### `UI/SceneNavigationManager.cs`
Purpose: hold temporary cross-scene map selection and perform scene changes.

- Key variables:
  `Instance` is the singleton reference.
  `MENU_SCENE` and `MULTIPLAYER_SCENE` are scene constants.
  `singlePlayerScenes` maps map IDs to single-player scene names.
  `PendingMapIdToLoad` stores which map the next scene should use.
- Key methods:
  `LoadMainMenu()` loads the home scene.
  `RestartCurrentScene()` reloads the active scene.
  `LoadMultiplayerScene(int mapId)` stores the selected map and loads multiplayer.
  `LoadSinglePlayerScene(int mapId)` stores the selected map and loads the mapped single-player scene.
  `GetSinglePlayerScene(int mapId)` resolves the scene name safely.
- What triggers it:
  UI buttons or menu logic call its public methods.
- What it triggers next:
  It changes Unity scenes through `SceneManager.LoadScene(...)`.
  It also updates `PendingMapIdToLoad`, which mode controllers read later.

### `UI/ScoreUI.cs`
Purpose: display simple score text for two players.

- Key variables:
  `_player1ScoreText` and `_player2ScoreText` store the text targets.
- Key methods:
  `OnEnable()` subscribes to `OnScoreUpdated`.
  `OnDisable()` unsubscribes.
  `UpdateScores(...)` redraws both labels.
- What triggers it:
  Gameplay controllers raise `GameEventBus.TriggerScoreUpdated(...)`.
- What it triggers next:
  It updates score text only.

### `UI/ShareButtonHook.cs`
Purpose: connect one or more buttons to the sharing system.

- Key variables:
  `shareManager` stores the target sharing controller.
  `buttons` stores the buttons to wire.
- Key methods:
  `OnEnable()` attaches `Share()` to each button.
  `OnDisable()` removes the listeners.
  `Share()` calls into `shareManager`.
- What triggers it:
  Button clicks invoke the attached listener.
- What it triggers next:
  It calls `shareManager.ShareTextAndScreenshot()`.

### `UI/ShareManager.cs`
Purpose: capture a screenshot and pass it to the share plugin.

- Key variables:
  `Instance` is the singleton reference.
- Key methods:
  `ShareTextAndScreenshot()` starts the capture coroutine.
  `CaptureAndShare()` waits for end of frame, captures a screenshot, waits for the file, and sends it to `Share.Items(...)`.
- What triggers it:
  `ShareButtonHook` or other UI code calls `ShareTextAndScreenshot()`.
- What it triggers next:
  It creates a screenshot file and calls the external share plugin with text plus file path.

### `UI/SinglePlayerMenuUI.cs`
Purpose: switch among single-player menu panels and control pause behavior.

- Key variables:
  `canvasGroups` stores all managed panels.
  `inGameGroupName`, `pauseGroupName`, `gameOverGroupName`, `settingsGroupName`, and `warningGroupName` store target panel names.
- Key methods:
  `Start()` shows the in-game group by default.
  `ShowPause()`, `ShowResume()`, `ShowGameOver()`, `ShowSettings()`, and `ShowWarning()` switch to specific panels.
  `RestartScene()` reloads the current scene.
  `GoHome()` loads the home scene.
  `ShowGroup(...)` is the shared panel activation helper and timescale setter.
- What triggers it:
  UI button methods and `TimerUI` call its public methods.
- What it triggers next:
  It changes panel visibility, adjusts `Time.timeScale`, and may trigger scene loads.

### `UI/TimerUI.cs`
Purpose: display time remaining and react when time expires.

- Key variables:
  `timerText` displays remaining seconds.
  `timerBar` stores the image that changes sprite at low time.
  `timerSprites` stores normal and low-time visuals.
  `timerSlider` stores normalized time progress.
  `lowTimeThreshold` defines when the urgent sprite appears.
  `menuUI` stores the UI controller used at expiry.
- Key methods:
  `OnEnable()` subscribes to timer events.
  `OnDisable()` unsubscribes.
  `UpdateTimer(...)` redraws the timer number, slider, and optional sprite.
  `HandleTimerExpired()` tells the menu UI to show game over.
- What triggers it:
  `SinglePlayerModeController` raises `TriggerTimerUpdated(...)` and `TriggerTimerExpired()`.
- What it triggers next:
  It updates timer UI and may call `menuUI.ShowGameOver()`.

### `UI/TurnUI.cs`
Purpose: briefly display which player's turn is active.

- Key variables:
  `_turnText` stores the turn label.
  `_turnPanel` stores the panel to show and hide.
  `displaySeconds` defines how long the message remains visible.
- Key methods:
  `OnEnable()` subscribes to `OnTurnChanged`.
  `OnDisable()` unsubscribes.
  `ShowTurnPanel(int playerId)` updates the message and starts the hide timer.
  `HidePanelAfterDelay(...)` hides the panel after a wait.
- What triggers it:
  `LocalMultiplayerModeController` raises `GameEventBus.TriggerTurnChanged(...)`.
- What it triggers next:
  It updates turn text and panel visibility.

### `UI/TutorialUI.cs`
Purpose: show a one-time tutorial overlay for new players.

- Key variables:
  `tutorialGroup` stores the tutorial panel.
  `displaySeconds` defines how long the tutorial stays visible.
- Key methods:
  `Start()` checks saved tutorial state and starts the overlay if needed.
  `ShowTutorial()` displays the overlay, waits, hides it, and marks the tutorial complete.
  `SetGroupVisible(...)` updates panel alpha and interaction flags.
- What triggers it:
  Unity calls `Start()` when the object is initialized.
  `DataManager` state determines whether the tutorial actually shows.
- What it triggers next:
  It changes tutorial panel visibility and calls `DataManager.Instance.TutorialCompleted()`.
