# ChineseItemDescriptions

《深空当铺：可能是偷的》（Probably Stolen）的非官方模组源码。模组可以按物品 ID 替换简体中文的悬停描述，目前内置厨刀（`kitchen_knife`）映射。原版描述、名称和物品属性由游戏保管。

## 构建

需要 .NET SDK 和已安装的游戏。下列 PowerShell 命令从仓库根目录运行，提示时输入游戏安装目录。

```powershell
$gameDir = Read-Host '游戏安装目录'
dotnet build './ChineseItemDescriptions.csproj' -c Release "-p:GameDir=$gameDir"
```

构建从游戏安装目录引用程序集，并把生成的模组 DLL 复制到游戏目录的 `Mods/ChineseItemDescriptions`。如需将 DLL 输出到另一个模组目录，可以传入 `-p:ModInstallDir=<目标目录>`。

安装时，在模组目录准备符合官方格式的 `manifest.xml`，将 `ChineseItemDescriptions.dll` 列入 `Assemblies`，并在 `Localization/zh.csv` 中填写自己的文案。下面是一行示例。

```csv
kitchen_knife_desc,这是一把刀
```

在游戏主菜单启用模组后重启。更新文案时，用 UTF-8 保存 CSV 并重启游戏。更换物品映射则需要编辑 `ModEntry.cs` 并重新构建。

[官方中文模组指南](https://github.com/dragonpan2/probably-stolen-modding/blob/main/ModdingGuide.zh-CN.md)
