# Modern TRCC - 利民水冷屏幕控制器

现代化的 Thermalright 利民水冷屏幕控制软件。

> 因为 TRCC 原版臃肿难用，所以有了 Modern TRCC。

##  特性

###  现代化界面
- **顶栏导航** - 顶部导航栏切换页面，页面内左侧栏 + 右侧内容布局
- **四国语言** - 简体中文 / 繁體中文 / English / 日本語 一键切换
- **利民家多屏幕控制** - 分离配置窗口

###  屏幕控制
- **TRCC 原始协议** - 与官方软件兼容的 USB 通信协议（JPEG 压缩 + 64 字节帧头）
- **多协议支持** - SCSI / Bulk / HID / LY / LY1
- **图像调节** - 亮度 / 对比度 / 饱和度 / 旋转 / 缩放模式

###  媒体处理
- **FFmpeg 流式播放** - 视频流式解码播放，支持循环播放
- **多格式支持** - PNG / JPG / BMP / GIF / MP4 / AVI / MKV / WebM
- **媒体库管理** - 导入、分类筛选、搜索、右键播放/删除
- **开机自动播放** - 启动并连接设备后自动播放选定的媒体

###  系统功能
- **开机自启动** - 注册表 + 最高优先级任务计划（零延迟启动）
- **最小化到托盘** - 后台运行，WebView2 自动挂起省内存
- **配置持久化** - 设置自动保存

##  架构设计

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

## 数据目录

- **设置文件**: `%APPDATA%\ModernTRCC\settings.json`
- **媒体库**: `%APPDATA%\ModernTRCC\Media\`

##  支持的设备

| 协议 | VID | PID | 说明 |
|------|-----|-----|------|
| SCSI | 0x125f | 0x1300 | 利民老款设备 |
| Bulk | 0x125f | 0x1301 | 利民新款设备 |
| HID | - | - | HID 协议设备 |
| LY | 0x0416 | 0x5131 | 凌扬方案 |
| LY1 | 0x0416 | 0x5133 | 凌扬方案(新款) |
9.19:现已支持TROFEO VISION (9.16英寸副屏) VID`0x0416` : PID`0x5408`

##  技术栈

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
