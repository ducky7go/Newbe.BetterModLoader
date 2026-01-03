# Project Context

## Purpose
Newbe.BetterModLoader is an enhanced mod loading system for "Escape from Duckov" (a Unity game). It serves as a drop-in replacement for the game's default ModManager, providing:

- Harmony-based runtime patching to intercept and enhance all mod operations
- Dynamic modular world effects (blessings and curses) system
- Seamless integration with Steam Workshop mod management
- Advanced mod deployment with backup, migration, and rollback capabilities

## Tech Stack
- **Language**: C# (.NET 9.0)
- **Game Engine**: Unity
- **Mod Framework**: Harmony (runtime method interception/patching)
- **Data Storage**: LiteDB (embedded NoSQL database)
- **Reactive Extensions**: R3 (reactive programming)
- **Audio**: FMODUnity
- **Game SDK**: Ducky.Sdk

## Project Conventions

### Code Style
- Follow C# naming conventions (PascalCase for public members, _camelCase for private fields)
- Use async/await for I/O operations
- Leverage reactive programming (R3) for event-driven patterns

### Architecture Patterns
- **Harmony Patching**: Use Harmony prefixes/postfixes to intercept game methods
- **Modular Design**: Separate concerns into distinct components (UI, deployment, save system, patches)
- **Reactive Programming**: Use R3 observables for state management and event handling
- **Repository Pattern**: LiteDB for data persistence

### Testing Strategy
- Comprehensive test suite included
- Test Harmony patches in isolation
- Verify mod loading/unloading cycles

### Git Workflow
- Main branch: `main`
- Use OpenSpec for spec-driven development

## Domain Context
- **Target Game**: Escape from Duckov
- **Steam Workshop Integration**: Mods are managed through Steam's workshop system
- **Harmony Patches**: Runtime method patching is core to intercepting game mod operations
- **Mod Groups**: Organize mods into logical groups with configurable settings

## Important Constraints
- Must maintain compatibility with existing Steam Workshop mods
- Harmony patches must be non-breaking when the original game updates
- LiteDB storage must handle concurrent mod operations safely
- UI must integrate seamlessly with Unity's existing UI system

## External Dependencies
- **Steam Workshop**: For mod distribution and management
- **Harmony Library**: MIT-licensed runtime patching framework
- **Ducky.Sdk**: Game-specific SDK provided by the game developers
- **LiteDB**: Lightweight embedded database for mod metadata and state
- **R3**: Reactive extensions for .NET
