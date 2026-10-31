<!-- OPENSPEC:START -->
# OpenSpec Instructions

These instructions are for AI assistants working in this project.

Always open `@/openspec/AGENTS.md` when the request:
- Mentions planning or proposals (words like proposal, spec, change, plan)
- Introduces new capabilities, breaking changes, architecture shifts, or big performance/security work
- Sounds ambiguous and you need the authoritative spec before coding

Use `@/openspec/AGENTS.md` to learn:
- How to create and apply change proposals
- Spec format and conventions
- Project structure and guidelines

Keep this managed block so 'openspec update' can refresh the instructions.

<!-- OPENSPEC:END -->

# Newbe.BetterModLoader - AI Coding Assistant Instructions

## Project overview

Newbe.BetterModLoader ("BML", Better Mod Loader) is a Unity mod for the game "Escape from Duckov" that takes over the game's `ModManager` with Harmony patches and provides its own mod-management UI: mod groups, activation ordering, an activation progress panel, deployment/migration/rollback and backup, and developer tools for uploading mods to Steam Workshop. Per `README.md` the project is in **maintenance mode: bug fixes only, no new features**; new users are pointed to the SuperDucky launcher (`ducky7go/SuperDucky`). It is built on the `Ducky.Sdk` NuGet package (mod `info.ini` currently `version = 2.0.3`, Workshop id in `publishedFileId`). The repo is open source at `ducky7go/Newbe.BetterModLoader`. Specs/proposals use OpenSpec (`openspec/AGENTS.md`, `openspec/project.md`; no `specs/` or `changes/` directories are committed yet).

## Repository layout

- `BML.slnx` - solution (the mod project and the test project). It also lists `NewMods/Directory.Build.props`, `NewNewMods/Directory.Build.props` and `SDKlibs/Directory.Build.props`; of these `NewNewMods/` and `SDKlibs/` do not exist in this checkout.
- `NewMods/Newbe.BetterModLoader/` - the only mod project (`Newbe.BetterModLoader.csproj`: `ModName=Newbe.BetterModLoader`, `IncludeHarmony=true`, references `Ducky.Sdk`, `LiteDB`, `R3`, `ObservableCollections.R3`). Main folders: `Main/` (mod manager UI view/VM), `Patches/` (Harmony patches on `ModManager`, `FadeGroupButton`), `Deployments/` (deployment, migration, rollback, old-manager backup), `DeveloperModes/` and `DeveloperInfos/` (local-mod panel, Steam Workshop upload), `ModDataChecks/`, `UI/`, `Audio/`, `Utils/`. Entry point: `ModBehaviour.cs` (`ModBehaviourBase`, Harmony id `bettermodloader`); persistence in `ModLoaderSaveSystem.cs` (under `Application.persistentDataPath/Newbe/Newbe.BetterModLoader`); localization keys in `LK.cs` (`bml_` prefix, 10 languages), constants in `CK.cs`.
- `NewMods/Newbe.BetterModLoader/assets/` - `info.ini`, `preview.png`, `title/` and `description/` per language, `Locales/` (per-language CSV plus long-text files), generated `lkeys.json` / `keys.hash.txt`, and `Resources/` (fonts, PNG/vector icons, sounds).
- `Tests/Newbe.BetterModLoader.Tests/` - NUnit + NSubstitute + Shouldly tests (`DeveloperInfoPanelTests`, `ModActivationProgressPanelTests`, `UploadValidationTests`); the mod project exposes internals to it via `InternalsVisibleTo`.
- `deploy.sh` - release script (see below). `Directory.Packages.props` - central package versions. `nuget.config` - maps `Ducky.Sdk` to the MyGet feed `https://www.myget.org/F/ducky/api/v3/index.json`, everything else to nuget.org. `.claude/commands/openspec/` - OpenSpec slash commands.

## Build / test / run

- Build: `dotnet build` at the repo root (README), which builds `BML.slnx`. Prerequisite per README: .NET 9.0 SDK. There is no `global.json` and no `.github/` CI in this checkout.
- Tests: `dotnet test Tests/Newbe.BetterModLoader.Tests/Newbe.BetterModLoader.Tests.csproj` (standard NUnit project; not run here). The tests reference the mod project, which needs the game assemblies.
- Target frameworks: `NewMods/Directory.Build.props` sets `netstandard2.1`, `LangVersion=preview`, `Nullable=enable`, `ImplicitUsings=false` for the mod. Because MSBuild picks the nearest `Directory.Build.props` (that file does not import the root one), the root `Directory.Build.props` (`net9.0`, `ModsDirectory=$(DuckovPath)Duckov_Data/Mods/`) applies to the test project, not to the mod.
- Local paths: both `Directory.Build.props` files import an optional, git-ignored `Directory.Build.props.user` (`*.user` is in `.gitignore`). Ducky.Sdk resolves the game from `SteamFolder` / `DuckovFolder` / `ManagedDirectory` / `ModsDirectory`; put those there (or pass `-p:`). The csproj has a hard-coded relative `HintPath` to `FMODUnity.dll` under `~/.local/share/Steam/steamapps/common/Escape from Duckov/...`; adjust it on other machines.
- Release/deploy (`./deploy.sh`): bumps the patch number of `version =` in `NewMods/Newbe.BetterModLoader/assets/info.ini`, runs `dotnet build -c Release`, then `ducky steam push "$MOD_PATH" --update-description` (requires `ducky-cli` and a `MOD_PATH` env value, see Gotchas). It publishes to the Steam Workshop, so do not run it casually.

## Conventions

- Code comments and log messages mix English and Chinese; keep the existing style of the file you edit. Log with `Ducky.Sdk.Logging.Log` and the `[BetterModLoader]` prefix; global usings are in `GlobalUsings.cs`.
- UI follows an MVVM split with R3 (`View.cs` / `Vm.cs` pairs, `ReactiveProperty`, `ObservableCollections`); async work uses UniTask (`.Forget()` for fire-and-forget).
- Localization: add keys to `LK.cs` (`bml_...`), let the Ducky.Sdk build regenerate `assets/Locales/*.csv`, `lkeys.json` and `keys.hash.txt`, then fill all 10 languages (`en, zh, zh-hant, ja, ko, fr, de, es, ru, pt`).
- Harmony patches must stay non-breaking across game updates (`openspec/project.md`); patches are applied selectively in `PatchFull()` in `ModBehaviour.cs`, not via blanket `PatchAll`.
- Git history in this checkout is a single "init version" commit; no commit-message convention is visible.

## Gotchas

- A `.env` file used to be **tracked in git** even though `.gitignore` lists `.env`; it is now untracked (it only ever held two local paths, `STEAM_BASE_DIR` and `MOD_PATH`, no credentials). `deploy.sh` sources it (`MOD_PATH`; README also mentions `STEAM_BASE_DIR`). Do not read, print or copy its contents, do not add new values to it, and never commit `.env`; keep local values untracked.
- `NewMods/Directory.Build.props` contains leftovers from the sibling `Newbe.Dod` repo (`WeCode`, `subModAssets/`, `Newbe.Dod.Common` project reference, `CopyDodShared` target); they are inactive here (`WeCode` is not set, `subModAssets/` does not exist). `DuckovPath` is referenced in both props files but only Ducky.Sdk's `SteamFolder` / `DuckovFolder` are used by the SDK.
- Large asset folders (`assets/Resources/Vector`, `PNG`, `Sounds`) are committed; avoid touching them in unrelated changes.
- On first enable BML disables itself and asks the user to migrate (activation marker `bettermodloader-enabled.txt`); testing activation flows in a real game profile can change which mods are enabled.

## Related repositories

- `ducky7go/Ducky.Sdk` - SDK package this mod uses (`Directory.Packages.props` pins `Ducky.Sdk` `0.1.7-dev.2`, a MyGet pre-release); the SDK has a `BmlSerilogLogProvider` that looks for the `Newbe.BetterModLoader` assembly.
- `ducky7go/ducky-cli` - packaging/publishing tool used by `deploy.sh` (`ducky steam push`).
- `ducky7go/SuperDucky` - launcher recommended to new users instead of BML (README).
- `Newbe.Dod` - sibling mod repo whose build leftovers remain in `NewMods/Directory.Build.props`.
