# 编译说明

## 前置要求

1. 安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. 有个可以执行命令的终端

## 编译文件

要实现"编译后的exe直接复制到任意Windows环境可以直接使用"，必须使用**独立发布模式**（`--self-contained true`）。

### 使用 .NET CLI

```bash
# 恢复 NuGet 包
dotnet restore

# 编译项目
dotnet build -c Release

# 发布为独立可执行文件（包含 .NET 运行时，可直接运行）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**编译后的文件位置**：`bin\Release\net8.0-windows\win-x64\publish\NeroUnfreeze.exe`

**说明**：
- 使用 `--self-contained true` 参数会将 .NET 运行时打包进 exe 文件
- 使用 `-p:PublishSingleFile=true` 参数会将所有依赖打包成单个 exe 文件
- 使用 `-p:IncludeNativeLibrariesForSelfExtract=true` 参数会包含本地库
- **这样编译出的 exe 文件可以直接复制到任何 Windows 10/11 系统运行**

## 配置文件说明

### 编译配置文件

- **`default-config.json`**：项目根目录的配置文件，包含所有设置的初始值
- **`ConfigService.cs`**：新增配置组合时，默认值存储位置，可以通过修改`LoadDefaultPreset()` 内容实现修改默认值
- **编译时自动生成**：编译时会自动根据 `default-config.json` 生成 `NeroUnfreezeConfig.json` 到输出目录

### 运行时配置文件

- **`NeroUnfreezeConfig.json`**：exe 同目录下的配置文件
- **读取**：程序启动时从 exe 同目录的 `NeroUnfreezeConfig.json` 加载配置
- **保存**：用户修改设置后，保存到 exe 同目录的 `NeroUnfreezeConfig.json`
- **资源相对路径**：配置文件中可以使用相对路径（相对于 exe 所在目录），程序会自动解析

### 配置文件示例

在 `default-config.json` 中可以设置初始值：

```json
{
  "DefaultPreset": {
    "Name": "圣诞节",
    "TargetDate": "",
    "CountdownDays": 14,
    "CharacterImagePath": "Nero.png",
    "IceImagePath": "FreezeNero.png",
    "AudioPath": "nero.mp3",
    "CharacterOpacity": 1.0,
    "IceOpacity": 1.0,
    "CharacterImageScale": 1.0,
    "IceImageScale": 1.0,
    "CharacterOffsetX": 0.0,
    "CharacterOffsetY": 0.0,
    "IceOffsetX": 0.0,
    "IceOffsetY": 0.0,
    "MaxAudioBlur": 1.0,
    "MinAudioVolume": 1.0
  },
  "DefaultConfig": {
    "AutoStart": true,
    "PreventMinimizeOnWinD": false
  }
}
```

### 推荐的目录结构

```
NeroUnfreeze/
├── NeroUnfreeze.exe
├── NeroUnfreezeConfig.json  (自动生成，首次运行后)
├── Nero.png                  (角色图片)
├── FreezeNero.png            (冰块图片)
└── nero.mp3                  (音频文件)
```

使用相对路径配置，整个文件夹可以任意移动，路径仍然有效。

**注意**：
- 确保所有依赖的 NuGet 包已正确安装
- 编译时，需准备文件 `icon.ico` 并放在项目根目录
- `TargetDate` 为空时，程序会自动使用当前年份的12月25日
