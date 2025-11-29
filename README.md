# NeroUnfreeze
又到了圣诞节解封的时候了！Pa→do↑ru→Pa↑do↓ru↑

## 写给小白

看到右边的Releases了么？点进去下载Zip文件，解压到同名目录下运行exe就可以了。

## 使用方法

### 首次运行

1. 运行 `NeroUnfreeze.exe`，程序会自动最小化到系统托盘
2. 右键点击系统托盘图标，选择"设置"

### 配置设置

1. **选择或创建组合**：
   - 从下拉列表选择现有组合，或点击"新增"创建新组合
   - 新组合的节日日期默认为当前年份的12月25日

2. **基本设置**：
   - **名称**：组合的名称
   - **节日日期**：目标节日的日期
   - **倒计时天数**：从多少天前开始显示效果（1-30天）

3. **资源文件**：
   - **角色图片**：点击"浏览"选择角色图片（支持 PNG、JPG、JPEG、BMP、GIF）
   - **冰块图片**：点击"浏览"选择冰块图片（姑且先这么叫了，等想到好听了叫法再改-_-||）
   - **音频路径**：点击"浏览"选择音频文件（支持 MP3、WAV、M4A、AAC）
   - 可以使用相对路径（相对于 exe 运行目录），方便分发

4. **高级设置**：
   - **角色透明度**：角色图片的透明度（0.0-1.0）
   - **冰块透明度**：冰块图片的初始透明度（0.0-1.0）
   - **图片缩放**：角色和冰块图片的缩放比例
   - **图片位置**：调整角色和冰块图片的相对位置
   - **音频最大模糊度**：音频的最大模糊程度（0.0-1.0）
   - **音频最小音量**：音频的最小音量（0.0-1.0）

5. **保存配置**：点击"确定"保存设置

6. **开机自启动**：勾选"开机自启动"选项，程序会在每次开机时自动运行

7. **防止 Win+D 最小化**：在设置中勾选"防止 Win+D 最小化"，可以防止按 `Win+D` 时窗口被最小化。

## 配置文件

### 运行时配置

- **位置**：exe 同目录下的 `NeroUnfreezeConfig.json`
- **自动创建**：首次运行后自动创建
- **相对路径**：支持使用相对路径（相对于 exe 运行目录）

### 默认配置

- **编译时**：项目根目录的 `default-config.json` 用于设置默认值
- **编译后**：自动生成 `NeroUnfreezeConfig.json` 到发布目录
- **修改默认值**：编辑 `ConfigService.cs` 中 `LoadDefaultPreset()` 的内容后，重新编译

## 系统要求

- **Windows 10 或更高版本**
- **无需安装 .NET 运行时**（如果使用独立发布模式）

## 编译说明

### 独立可执行文件（推荐）

要实现"编译后的exe直接复制到任意Windows环境就可以直接使用"，请使用以下命令：

```bash
# 发布为独立可执行文件（包含 .NET 运行时）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

**编译后的文件**：`bin\Release\net8.0-windows\win-x64\publish\NeroUnfreeze.exe`

**重要说明**：
- 使用 `--self-contained true` 会将 .NET 运行时打包进 exe
- 使用 `-p:PublishSingleFile=true` 会将所有依赖打包成单个文件
- **这样编译出的 exe 可以直接复制到任何 Windows 10/11 系统运行，无需安装任何依赖**

### 依赖框架模式

如果需要较小的文件大小（需要用户安装 .NET 8.0 Runtime）：

```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

**注意**：这种方式需要目标系统安装 .NET 8.0 Runtime。

详细编译说明请参考 [BUILD.md](BUILD.md)

## 使用技巧

### 推荐的运行目录结构

```
NeroUnfreeze/
├── NeroUnfreeze.exe
├── NeroUnfreezeConfig.json  (自动生成，首次运行后)
├── Nero.png                  (角色图片)
├── FreezeNero.png            (冰块图片)
└── nero.mp3                  (音频文件)
```

使用相对路径配置，整个文件夹可以任意移动，路径仍然有效。

## 写在最后
圣诞快乐^_^
