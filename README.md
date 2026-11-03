# Newbe.BetterModLoader

**Better Mod Loader | 更好的 mod 加载器**

## Project Status

This project is in maintenance mode, only bug fixes are provided, no new features will be added.

本项目已进入维护期，仅提供 bug 修复，不再添加新功能。

## Open Source

This project is now open source: https://github.com/ducky7go/Newbe.BetterModLoader

## Recommended for New Users

New users are recommended to use the more powerful launcher: [SuperDucky](https://github.com/ducky7go/SuperDucky)

推荐新用户使用功能更强大的启动器：[超级鸭鸭](https://github.com/ducky7go/SuperDucky)

## Development

### Prerequisites

- .NET 9.0 SDK
- [ducky-cli](https://github.com/ducky7go/ducky-cli) - Mod packaging and publishing tool
- Unity MelonLoader environment

### Build

```bash
dotnet build
```

### Deploy

Create a `.env` file in the project root with your local paths:

```bash
STEAM_BASE_DIR="/path/to/Steam"
MOD_PATH="/path/to/Escape from Duckov/Duckov_Data/Mods/Newbe.BetterModLoader/"
```

Then run the deployment script:

```bash
./deploy.sh
```

This will:
1. Bump the version automatically
2. Build the project
3. Publish to Steam Workshop with updated description

## License

See [LICENSE](LICENSE) file for details.
