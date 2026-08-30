# Modern TRCC - 利民水冷屏幕控制器

一款现代化的 Thermalright 利民水冷屏幕控制软件，使用 .NET 8 WPF 开发，拥有黑灰金奢华风格界面。

> 因为 TRCC 原版臃肿难用，所以有了 Modern TRCC。

## ✨ 特性

### 🎨 现代化界面
- **黑灰金奢华主题** - 深色背景 + 金色点缀，自绘暗色标题栏
- **顶栏导航** - 顶部导航栏切换页面，页面内左侧栏 + 右侧内容布局
- **四国语言** - 简体中文 / 繁體中文 / English / 日本語 一键切换
- **圆角窗口** - 使用 Windows 11 DWM 原生圆角

### 🖥️ 屏幕控制
- **TRCC 原始协议** - 与官方软件兼容的 USB 通信协议（JPEG 压缩 + 64 字节帧头）
- **多协议支持** - SCSI / Bulk / HID / LY / LY1
- **图像调节** - 亮度 / 对比度 / 饱和度 / 旋转 / 缩放模式
- **实时日志** - 设备连接、自启动注册等事件实时记录

### 🎬 媒体处理
- **FFmpeg 流式播放** - 视频流式解码播放，支持循环播放
- **多格式支持** - PNG / JPG / BMP / GIF / MP4 / AVI / MKV / WebM
- **媒体库管理** - 导入、分类筛选、搜索、右键播放/删除
- **静态预览** - 播放时显示当前媒体的缩略图与信息（时长/帧率/大小）
- **开机自动播放** - 启动并连接设备后自动播放选定的媒体

### 🔧 系统功能
- **开机自启动** - 注册表 + 最高优先级任务计划（零延迟启动）
- **最小化到托盘** - 后台运行，WebView2 自动挂起省内存
- **启动画面** - Adobe 风格黑金启动画面
- **配置持久化** - 设置自动保存

## 🏗️ 架构设计

```
ModernTRCC.WPF/
├── src/
│   ├── ModernTRCC.Core/          # 核心库
│   │   ├── Models/               # 数据模型
│   │   │   ├── AppSettings.cs    # 应用设置
│   │   │   ├── LcdDeviceInfo.cs  # 设备信息
│   │   │   ├── MediaItem.cs      # 媒体项
│   │   │   └── PreRenderedFrame.cs
│   │   └── Services/             # 核心服务
│   │       ├── LcdController.cs  # LCD 控制器（USB通信）
│   │       ├── FFmpegService.cs  # FFmpeg 包装
│   │       ├── FramePrerenderService.cs
│   │       ├── MediaLibraryService.cs   # 媒体库管理
│   │       ├── SettingsService.cs       # 设置管理
│   │       └── LogService.cs            # 实时日志
│   └── ModernTRCC.App/           # WPF 应用
│       ├── Views/                # 视图
│       │   ├── MainWindow.xaml   # 主窗口
│       │   └── SplashWindow.xaml # 启动画面
│       ├── ui/                   # WebView2 前端界面
│       │   ├── index.html
│       │   ├── app.js
│       │   └── styles.css
│       └── Resources/            # 图标资源
│
├── installer.iss                 # Inno Setup 安装脚本
├── build.bat                     # 构建脚本
├── publish.bat                   # 发布脚本
└── ModernTRCC.sln                # 解决方案
```

## 🚀 快速开始

### 环境要求
- Windows 10/11（64 位）
- .NET 8.0 SDK（开发时需要）
- .NET 8.0 Desktop Runtime（运行时需要）
- 支持的利民水冷屏幕设备

### 构建项目

```bash
# 方法1：使用构建脚本
build.bat

# 方法2：使用 dotnet CLI
dotnet build ModernTRCC.sln -c Release
```

### 发布单文件版本

```bash
# 使用发布脚本
publish.bat

# 或手动执行
dotnet publish src/ModernTRCC.App/ModernTRCC.App.csproj ^
    -c Release -r win-x64 --self-contained false ^
    -p:PublishSingleFile=true -o publish
```

### 部署 FFmpeg

1. 从 [gyan.dev](https://www.gyan.dev/ffmpeg/builds/) 下载 FFmpeg Windows 构建（essential 版本即可）
2. 将 `ffmpeg.exe` 和 `ffprobe.exe` 复制到程序目录下的 `ffmpeg` 文件夹中

### 制作安装包

```bash
# 需要安装 Inno Setup 6
"C:\Program Files (x86)\Inno Setup 6\ISCC.exe" installer.iss
```

## 📁 数据目录

- **设置文件**: `%APPDATA%\ModernTRCC\settings.json`
- **媒体库**: `%APPDATA%\ModernTRCC\Media\`

## 🔌 支持的设备

| 协议 | VID | PID | 说明 |
|------|-----|-----|------|
| SCSI | 0x125f | 0x1300 | 利民老款设备 |
| Bulk | 0x125f | 0x1301 | 利民新款设备 |
| HID | - | - | HID 协议设备 |
| LY | 0x0416 | 0x5131 | 凌扬方案 |
| LY1 | 0x0416 | 0x5133 | 凌扬方案(新款) |

## 🛠️ 技术栈

- **框架**: .NET 8 WPF
- **UI**: WebView2 + 自定义样式（黑灰金奢华风格）
- **架构**: MVVM + 依赖注入
- **USB**: LibUsbDotNet
- **视频**: FFmpeg (外部进程)
- **图像**: System.Drawing.Common
- **MVVM**: CommunityToolkit.Mvvm
- **DI**: Microsoft.Extensions.DependencyInjection

## 📝 License

[MIT](LICENSE)
