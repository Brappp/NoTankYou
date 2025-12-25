# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

BuffAlert is a Dalamud plugin for FFXIV that displays in-game warning icons when party members are missing role-specific actions (tank stances, pet summons, Dance Partner, etc.).

## Build Commands

```bash
# Build release (x64 only)
dotnet build BuffAlert.sln -c Release -p:Platform=x64

# Build debug
dotnet build BuffAlert.sln -c Debug -p:Platform=x64
```

Requirements: .NET SDK 7.0.0, Visual Studio 2022 (17.0+), Dalamud plugin development environment. No automated tests; testing requires running in-game via XIVLauncher/Dalamud.

## Architecture

### Plugin Lifecycle
1. **Construction** - Register commands, create controllers, add windows
2. **Login** - Load character-specific configurations
3. **Framework Update** (every frame) - Evaluate warnings, update displays
4. **Logout** - Clear configurations
5. **Disposal** - Clean up resources

### Key Components

- **BuffAlertPlugin.cs** - Main entry point (IDalamudPlugin)
- **System.cs** - Static system state container
- **Services.cs** - Dalamud service injection container

### Controllers (`Controllers/`)
- `ModuleController` - Manages all warning modules, evaluates warnings each frame
- `BlacklistController` - Zone blacklist management

### Module System (`Modules/`)
Each warning type is a module inheriting from `ModuleBase<T>`:
- `EvaluateWarnings(IPlayerData)` - Core warning evaluation logic
- `ShouldEvaluate(IPlayerData)` - Filter by job, level, location
- `DrawConfigUi()` - Render module settings

Modules: Tanks, Dancer, Scholar, Summoner, Sage, BlueMage, Monk, Reaper, Pictomancer, Food, SpiritBond, Gatherers, Chocobo, FreeCompany

### Player Data Abstraction (`PlayerDataInterface/`)
`IPlayerData` interface abstracts queries for local player vs. party members:
- `HasStatus()`, `MissingStatus()` - Status effect checking
- `GetLevel()`, `GetClassJob()` - Job/level info
- `HasPet()`, `IsTargetable()` - Entity queries

### Configuration (`Configuration/`)
Character-specific configs saved at: `PluginConfigs\BuffAlert\{CharacterId}\{FileName}`
- Inherit from `ModuleConfigBase`
- Saved via `Config.SaveCharacterConfig()`

## Adding a New Warning Module

1. Create class inheriting `ModuleBase<YourConfigClass>` in `Modules/`
2. Add entry to `ModuleName` enum in `Classes/ModuleName.cs`
3. Implement `EvaluateWarnings(IPlayerData)` and `ShouldEvaluate(IPlayerData)`
4. Module auto-discovered via reflection

## In-Game Commands

- `/buffalert` or `/ba` - Open configuration window

## Code Patterns

- Uses Dalamud IoC for service injection (`[PluginService]` attributes)
- Unsafe pointers for FFXIVClientStructs game data access
- Reflection-based module auto-discovery
- LINQ for filtering/mapping collections
- ImGui for all UI rendering
