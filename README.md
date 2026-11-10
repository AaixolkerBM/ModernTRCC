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

## 设备连接调试

- `0416:5302` 屏幕可通过 Windows 原生 HID 通信，保留系统 HID 驱动，无需替换成 WinUSB。设备信息页的 Windows 枚举结果仅说明设备存在；仪表盘在握手成功后才显示“已连接”。
- 型号码 PM `58` 使用 `240×320` 原生像素栅格、RGB565 小端编码。屏幕安装方向不同时，在“显示”页设置旋转；测试图案与图片使用相同的旋转流程。
- 本仓库的构建入口是 `ModernTRCC.sln`，实际应用代码位于 `src/ModernTRCC.App`。根目录另有未接入解决方案的应用代码。
- 构建：`dotnet build ModernTRCC.sln -c Release`。
- 协议与矩形屏旋转回归检查：`dotnet run --project tests/ModernTRCC.Core.Tests -c Release`；本机连接了 `0416:5302` 时，加 `-- --enumerate` 验证原生 HID 枚举。
- 传输 API 参考：[Microsoft HID 报告发送文档](https://learn.microsoft.com/en-us/windows-hardware/drivers/hid/sending-hid-reports)。PM 58 线协议参考：[实机协议记录](https://github.com/Lexonight1/thermalright-trcc-linux/issues/228)。

## 数据目录

- **设置文件**: `%APPDATA%\ModernTRCC\settings.json`
- **媒体库**: 程序目录下的 `media\`；缩略图位于 `thumbnails\`。

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
