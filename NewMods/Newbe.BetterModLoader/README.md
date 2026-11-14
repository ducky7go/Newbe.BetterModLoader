# Newbe.BetterModLoader

## 中文

《逃离鸭科夫》的模组加载器 BML(Better Mod Loader,更好的 mod 加载器)。它用 Harmony 补丁接管游戏自带的 `ModManager`,提供自己的 mod 管理界面:模组分组、启用顺序、激活进度面板、迁移与回退、备份,以及面向开发者的本地 mod 面板和 Steam Workshop 上传。

**项目已进入维护期,只修 bug,不再添加新功能。** 新用户建议使用功能更强的启动器 [SuperDucky](https://github.com/ducky7go/SuperDucky)。项目源码开源:<https://github.com/ducky7go/Newbe.BetterModLoader>。

### 功能

- **分组与排序**:模组按组管理(预设组如 `Default`、`Enabled Before`、`BML Preset`,以及自定义组);可拖动排序,双击把 mod 置顶或置底;组可以被锁定,锁定后不能增删 mod、不能改启用状态或排序;一键全部启用 / 全部禁用。
- **激活进度面板**:游戏启动时按配置启用 mod,并显示进度;完成后列出最快和最慢各 3 个 mod,失败时可直接打开日志目录。
- **模组数据检查**:检查缺少的依赖、加载顺序错误和需要更新的 mod;检查项可以忽略;对依赖 CustomThings 框架的 mod,会提示它们不启用也能工作,启用反而可能拉长加载时间。
- **迁移与回退**:首次启用时,BML 先禁用自己并弹出迁移确认;确认后记录当前已启用的 mod、用原生 API 禁用其余 mod、创建启用标记 `bettermodloader-enabled.txt`,重启后才真正接管。随时可以回退到游戏原生的 mod 管理器,迁移和回退不改写原管理器里的启用状态和排序。
- **备份**:启动时在后台检查 BML 自身程序集的 SHA256 哈希,有变化才备份 BML 所在的 mod 目录,并清理旧备份。默认开启,默认保留 3 份(限制在 1 到 10 份)。
- **开发者模式**:列出本地 mod,打开 mod 目录、Steam 页面;上传到 Steam Workshop 前有清单检查(`info.ini` 完整、预览图存在且小于 1MB、`description/` 和 `title/` 下有多语言文件、单个描述小于 8KB、版本和变更说明等)。开发者信息面板可以复制或打开配置目录、本地 mod 目录、Steam mod 目录、游戏持久化数据目录和日志目录。
- **界面语言**:英语、简体中文、繁体中文、日语、韩语、法语、德语、西班牙语、俄语、葡萄牙语。

### 使用

1. 订阅并启用 mod。首次启用时会看到迁移确认窗口,确认后游戏在几秒后关闭,重启后 BML 接管 mod 管理。
2. 在 BML 的 mod 管理界面里切换分组、启用或禁用 mod、调整顺序,选择「应用更改」(Apply Changes)保存;有未保存更改时需要选择「应用更改」或「取消并退出」才能离开。
3. 要退回原来的管理器,在回退窗口里确认,重启后生效。

### 设置项

通过 Ducky.Sdk 的 `ModOptions` 读写:

| 键 | 默认值 | 含义 |
| --- | --- | --- |
| `bml_developer_force_restart_setting` | `true` | 修改 mod 顺序后是否强制重启游戏(开发者信息面板里的「Force restart on order changes」开关)。 |
| `bml_backup_enabled` | `true` | 是否启用 BML 备份。 |
| `bml_max_backups` | `3` | 最多保留的备份数,限制在 1 到 10。 |
| `bml_audio_enabled` | `true` | 是否启用 BML 音效。 |
| `bml_toggle_audio_enabled` | `true` | 开关音效。 |
| `bml_click_audio_enabled` | `true` | 点击音效。 |
| `bml_drag_audio_enabled` | `true` | 拖动音效。 |

BML 自己的数据(配置、启用标记、旧管理器启用列表的备份)保存在 `Application.persistentDataPath` 下的 `Newbe/Newbe.BetterModLoader`。

### 兼容性与限制

- 它接管游戏的 `ModManager`,通过选择性的 Harmony 补丁(`InterceptFadeGroupButton`、`InterceptShouldActivateModPatch`、`PatchModManagerIsModActive`)工作。
- 在真实游戏档案里测试激活流程,会改变哪些 mod 处于启用状态。
- 项目处于维护期,不会新增功能。

### Workshop

<https://steamcommunity.com/sharedfiles/filedetails/?id=3596571766>

### 开发者构建

- 需要 .NET 9.0 SDK;打包与发布工具是 [ducky-cli](https://github.com/ducky7go/ducky-cli)。
- 在仓库根目录构建:`dotnet build`,构建 `BML.slnx`。构建需要本机装有游戏程序集,游戏位置可放在被 git 忽略的 `Directory.Build.props.user` 里,或用 `-p:` 传入。
- 测试:`dotnet test Tests/Newbe.BetterModLoader.Tests/Newbe.BetterModLoader.Tests.csproj`。
- 更多说明见仓库根目录的 [README](../../README.md)。

## English

BML (Better Mod Loader) is a mod loader for *Escape from Duckov*. It takes over the game's own `ModManager` with Harmony patches and provides its own mod-management UI: mod groups, activation order, an activation progress panel, migration and rollback, backups, and a local-mod panel and Steam Workshop upload for developers.

**The project is in maintenance mode: bug fixes only, no new features.** New users are recommended to use the more powerful launcher [SuperDucky](https://github.com/ducky7go/SuperDucky). The source is open: <https://github.com/ducky7go/Newbe.BetterModLoader>.

### Features

- **Groups and ordering**: mods are managed in groups (preset groups such as `Default`, `Enabled Before` and `BML Preset`, plus custom groups); drag to reorder, double-click to pin a mod to the top or bottom; a group can be locked, which blocks adding or removing mods and changing enabled state or order; enable all / disable all with one click.
- **Activation progress panel**: at game start it enables mods per the configuration and shows progress; when done it lists the 3 fastest and 3 slowest mods, and on failure you can open the logs folder directly.
- **Mod data check**: finds missing dependencies, load-order errors and mods that need an update; check items can be ignored; for mods that depend on the CustomThings framework it notes that they work without being enabled, and that enabling them may lengthen loading.
- **Migration and rollback**: on the first enable, BML disables itself and asks you to confirm a migration; once confirmed it records the currently enabled mods, disables the rest through the native API and creates the activation marker `bettermodloader-enabled.txt`, and only takes over after a restart. You can roll back to the game's native mod manager at any time; neither step rewrites the enabled state or order in the original manager.
- **Backups**: at start it checks, in the background, the SHA256 hash of BML's own assembly, backs up BML's mod folder only when the hash has changed, and cleans up old backups. On by default, keeping 3 by default (limited to 1 to 10).
- **Developer mode**: lists local mods, opens a mod's folder or Steam page; before uploading to Steam Workshop it runs a checklist (`info.ini` complete, preview image present and under 1MB, multilingual files under `description/` and `title/`, each description under 8KB, version and change notes, and so on). The developer info panel can copy or open the config folder, local mods folder, Steam mods folder, game persistent data folder and logs folder.
- **UI languages**: English, Simplified Chinese, Traditional Chinese, Japanese, Korean, French, German, Spanish, Russian, Portuguese.

### Usage

1. Subscribe to and enable the mod. On the first enable you see the migration prompt; after you confirm, the game closes in a few seconds, and BML takes over mod management after the restart.
2. In BML's mod-management screen, switch groups, enable or disable mods and change the order, then choose "Apply Changes" to save; with unsaved changes you must choose "Apply Changes" or "Cancel and Exit" to leave.
3. To go back to the original manager, confirm in the rollback window; it takes effect after a restart.

### Settings

Read and written through Ducky.Sdk's `ModOptions`:

| Key | Default | Meaning |
| --- | --- | --- |
| `bml_developer_force_restart_setting` | `true` | Whether changing the mod order forces a game restart (the "Force restart on order changes" toggle in the developer info panel). |
| `bml_backup_enabled` | `true` | Whether BML backups are on. |
| `bml_max_backups` | `3` | Maximum number of backups kept, limited to 1 to 10. |
| `bml_audio_enabled` | `true` | Whether BML sound effects are on. |
| `bml_toggle_audio_enabled` | `true` | Toggle sounds. |
| `bml_click_audio_enabled` | `true` | Click sounds. |
| `bml_drag_audio_enabled` | `true` | Drag sounds. |

BML's own data (configuration, the activation marker and the backup of the old manager's enabled list) is stored under `Newbe/Newbe.BetterModLoader` in `Application.persistentDataPath`.

### Compatibility and limits

- It takes over the game's `ModManager` through selective Harmony patches (`InterceptFadeGroupButton`, `InterceptShouldActivateModPatch`, `PatchModManagerIsModActive`).
- Testing the activation flow in a real game profile changes which mods are enabled.
- The project is in maintenance mode and gets no new features.

### Workshop

<https://steamcommunity.com/sharedfiles/filedetails/?id=3596571766>

### Building

- Needs the .NET 9.0 SDK; packaging and publishing use [ducky-cli](https://github.com/ducky7go/ducky-cli).
- Build at the repository root with `dotnet build`, which builds `BML.slnx`. The build needs the game's assemblies on the machine; put the game location in the git-ignored `Directory.Build.props.user`, or pass it with `-p:`.
- Tests: `dotnet test Tests/Newbe.BetterModLoader.Tests/Newbe.BetterModLoader.Tests.csproj`.
- More in the repository root [README](../../README.md).
