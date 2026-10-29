// ==================== 多语言 ====================
const I18N = {
    'zh-CN': {
        'nav.dashboard': '仪表盘', 'nav.media': '媒体库', 'nav.display': '显示',
        'nav.settings': '设置', 'nav.deviceInfo': '设备信息',
        'page.dashboard.title': '仪表盘', 'page.dashboard.sub': '管理你的水冷屏幕和媒体内容',
        'page.media.title': '媒体库', 'page.media.sub': '管理你的图片和视频素材',
        'page.display.title': '显示', 'page.display.sub': '调整屏幕显示效果和性能参数',
        'page.system.title': '设置', 'page.system.sub': '配置应用启动、外观和系统选项',
        'page.diagnostics.title': '设备信息', 'page.diagnostics.sub': '查看所有USB设备信息，帮助识别水冷屏幕',
        'status.connected': '已连接', 'status.disconnected': '未连接', 'screen.name': '屏幕 {n}',
        'btn.scan': '扫描设备', 'btn.uploadImage': '上传图片', 'btn.uploadVideo': '上传视频',
        'btn.clear': '清除屏幕', 'btn.clearLogs': '清除',
        'btn.save': '保存设置', 'btn.apply': '应用设置', 'btn.reset': '重置',
        'btn.enable': '启用', 'btn.disable': '禁用', 'btn.uploadFile': '上传文件',
        'stat.devices': '已连接设备', 'stat.images': '图片素材', 'stat.videos': '视频素材',
        'stat.playback': '播放状态', 'stat.imagesSub': 'PNG / JPG / BMP', 'stat.videosSub': 'MP4 / AVI / GIF',
        'filter.all': '全部', 'filter.image': '图片', 'filter.video': '视频',
        'search.placeholder': '搜索...',
        'test.bars': '彩条', 'test.gradient': '渐变', 'test.grid': '网格',
        'title.min': '最小化', 'title.max': '最大化', 'title.close': '关闭',
        'dash.preview': '当前播放', 'dash.testPattern': '测试图案', 'dash.testDesc': '选择测试图案验证屏幕是否正常工作',
        'media.empty.title': '暂无媒体文件', 'media.empty.desc': '上传一些图片或视频开始使用吧',
        'media.type.video': '视频', 'media.type.gif': 'GIF', 'media.type.image': '图片',
        'ctx.play': '播放', 'ctx.delete': '删除',
        'display.imageAdjust': '图像调整', 'display.brightness': '亮度', 'display.contrast': '对比度',
        'display.saturation': '饱和度', 'display.rotation': '屏幕旋转', 'display.fitMode': '缩放模式',
        'display.fit.cover': '填充', 'display.fit.contain': '适应', 'display.fit.fill': '拉伸',
        'display.performance': '性能设置',
        'display.targetFps': '屏幕刷新率', 'display.targetFps.desc': '每秒向屏幕发送的帧数，越高越流畅但占用更多USB带宽',
        'display.videoFps': '视频播放帧率', 'display.videoFps.desc': '视频每秒播放的帧数，决定播放速度，通常保持与原视频一致',
        'display.loop': '循环播放视频', 'display.loop.desc': '视频结束后自动重播',
        'sys.autostart': '开机自启动', 'sys.autostart.off': '自启动未启用', 'sys.autostart.on': '自启动已启用',
        'sys.autostart.desc': '应用将在系统启动时自动运行',
        'sys.startup': '启动选项',
        'sys.startMinimized': '启动时最小化', 'sys.startMinimized.desc': '自启动时以最小化方式运行',
        'sys.fastStartup': '快速启动（高优先级）', 'sys.fastStartup.desc': '注册为登录时最高优先级任务，确保最快启动',
        'sys.autoplay': '开机自动播放', 'sys.autoplay.desc': '启动并连接设备后自动播放选定的媒体',
        'sys.enableAutoplay': '启用自动播放', 'sys.enableAutoplay.desc': '开机后自动播放下方选择的图像或视频',
        'sys.autoplayMedia': '播放媒体', 'sys.autoplay.none': '（未选择媒体）',
        'sys.appearance': '外观设置',
        'sys.minimizeToTray': '最小化到系统托盘', 'sys.minimizeToTray.desc': '关闭窗口时最小化到托盘，继续后台运行',
        'sys.language': '语言', 'sys.about': '关于', 'sys.version': '版本 1.1.2',
        'sys.author': '作者', 'sys.protocol': '协议', 'sys.aboutCopy': '支持利民 Thermalright 全系列水冷屏幕',
        'diag.refresh': '刷新设备列表', 'diag.known': '已知设备列表', 'diag.allUsb': '所有USB设备',
        'diag.sidebar.devices': '设备列表',
        'diag.clickRefresh': '点击「刷新设备列表」扫描', 'diag.logs': '实时日志',
        'diag.waitingLogs': '等待日志...', 'diag.noKnown': '无已知设备', 'diag.noUsb': '未检测到任何USB设备',
        'diag.found': '已检测到', 'diag.notFound': '未检测到', 'diag.matched': '匹配已知设备',
        'diag.scanDone': '扫描完成，找到 {count} 个USB设备',
        'toast.scanning': '正在扫描设备...', 'toast.scanFound': '扫描到 {count} 个设备',
        'toast.scanNone': '未找到设备，请检查USB连接和驱动',
        'toast.sendingPattern': '发送测试图案: {pattern}',
        'toast.cleared': '已清除屏幕', 'toast.playing': '正在播放: {name}', 'toast.playStart': '开始播放...',
        'toast.playDone': '播放完成', 'toast.playFail': '视频播放失败：FFmpeg 不可用或视频格式不支持',
        'toast.settingsApplied': '设置已应用', 'toast.settingsReset': '设置已重置',
        'toast.scanningUsb': '正在扫描USB设备...',
        'toast.logsCleared': '日志已清除', 'toast.saved': '设置已保存',
        'toast.autostartOn': '自启动已启用', 'toast.autostartOff': '自启动已禁用',
        'toast.deviceNotConnected': '设备未连接', 'toast.patternSent': '测试图案已发送',
        'toast.sendFailed': '发送失败', 'toast.mediaAdded': '已添加 {count} 个文件',
        'toast.mediaDeleted': '已删除: {name}', 'toast.deleteFailed': '删除失败',
        'toast.action.closeConflicts': '关闭并重连',
        'playback.playing': '播放中', 'playback.idle': '空闲',
        'log.empty.cleared': '日志已清除',
    },
    'zh-TW': {
        'nav.dashboard': '儀表盤', 'nav.media': '媒體庫', 'nav.display': '顯示',
        'nav.settings': '設定', 'nav.deviceInfo': '裝置資訊',
        'page.dashboard.title': '儀表盤', 'page.dashboard.sub': '管理你的水冷螢幕與媒體內容',
        'page.media.title': '媒體庫', 'page.media.sub': '管理你的圖片與影片素材',
        'page.display.title': '顯示', 'page.display.sub': '調整螢幕顯示效果與效能參數',
        'page.system.title': '設定', 'page.system.sub': '設定應用程式啟動、外觀與系統選項',
        'page.diagnostics.title': '裝置資訊', 'page.diagnostics.sub': '查看所有USB裝置資訊，協助識別水冷螢幕',
        'status.connected': '已連線', 'status.disconnected': '未連線', 'screen.name': '螢幕 {n}',
        'btn.scan': '掃描裝置', 'btn.uploadImage': '上傳圖片', 'btn.uploadVideo': '上傳影片',
        'btn.clear': '清除螢幕', 'btn.clearLogs': '清除',
        'btn.save': '儲存設定', 'btn.apply': '套用設定', 'btn.reset': '重設',
        'btn.enable': '啟用', 'btn.disable': '停用', 'btn.uploadFile': '上傳檔案',
        'stat.devices': '已連線裝置', 'stat.images': '圖片素材', 'stat.videos': '影片素材',
        'stat.playback': '播放狀態', 'stat.imagesSub': 'PNG / JPG / BMP', 'stat.videosSub': 'MP4 / AVI / GIF',
        'filter.all': '全部', 'filter.image': '圖片', 'filter.video': '影片',
        'search.placeholder': '搜尋...',
        'test.bars': '彩條', 'test.gradient': '漸層', 'test.grid': '格線',
        'title.min': '最小化', 'title.max': '最大化', 'title.close': '關閉',
        'dash.preview': '目前播放', 'dash.testPattern': '測試圖案', 'dash.testDesc': '選擇測試圖案驗證螢幕是否正常運作',
        'media.empty.title': '暫無媒體檔案', 'media.empty.desc': '上傳一些圖片或影片開始使用吧',
        'media.type.video': '影片', 'media.type.gif': 'GIF', 'media.type.image': '圖片',
        'ctx.play': '播放', 'ctx.delete': '刪除',
        'display.imageAdjust': '影像調整', 'display.brightness': '亮度', 'display.contrast': '對比度',
        'display.saturation': '飽和度', 'display.rotation': '螢幕旋轉', 'display.fitMode': '縮放模式',
        'display.fit.cover': '填滿', 'display.fit.contain': '適應', 'display.fit.fill': '拉伸',
        'display.performance': '效能設定',
        'display.targetFps': '螢幕更新率', 'display.targetFps.desc': '每秒傳送給螢幕的幀數，越高越流暢但佔用更多USB頻寬',
        'display.videoFps': '影片播放幀率', 'display.videoFps.desc': '影片每秒播放的幀數，決定播放速度，通常與原影片一致',
        'display.loop': '循環播放影片', 'display.loop.desc': '影片結束後自動重播',
        'sys.autostart': '開機自啟動', 'sys.autostart.off': '自啟動未啟用', 'sys.autostart.on': '自啟動已啟用',
        'sys.autostart.desc': '應用程式將在系統啟動時自動執行',
        'sys.startup': '啟動選項',
        'sys.startMinimized': '啟動時最小化', 'sys.startMinimized.desc': '自啟動時以最小化方式執行',
        'sys.fastStartup': '快速啟動（高優先級）', 'sys.fastStartup.desc': '註冊為登入時最高優先級任務，確保最快啟動',
        'sys.autoplay': '開機自動播放', 'sys.autoplay.desc': '啟動並連線裝置後自動播放選定的媒體',
        'sys.enableAutoplay': '啟用自動播放', 'sys.enableAutoplay.desc': '開機後自動播放下方選擇的影像或影片',
        'sys.autoplayMedia': '播放媒體', 'sys.autoplay.none': '（未選擇媒體）',
        'sys.appearance': '外觀設定',
        'sys.minimizeToTray': '最小化到系統托盤', 'sys.minimizeToTray.desc': '關閉視窗時最小化到托盤，繼續背景執行',
        'sys.language': '語言', 'sys.about': '關於', 'sys.version': '版本 1.1.2',
        'sys.author': '作者', 'sys.protocol': '協定', 'sys.aboutCopy': '支援利民 Thermalright 全系列水冷螢幕',
        'diag.refresh': '重新整理裝置列表', 'diag.known': '已知裝置列表', 'diag.allUsb': '所有USB裝置',
        'diag.sidebar.devices': '裝置列表',
        'diag.clickRefresh': '點擊「重新整理裝置列表」掃描', 'diag.logs': '即時日誌',
        'diag.waitingLogs': '等待日誌...', 'diag.noKnown': '無已知裝置', 'diag.noUsb': '未偵測到任何USB裝置',
        'diag.found': '已偵測到', 'diag.notFound': '未偵測到', 'diag.matched': '匹配已知裝置',
        'diag.scanDone': '掃描完成，找到 {count} 個USB裝置',
        'toast.scanning': '正在掃描裝置...', 'toast.scanFound': '掃描到 {count} 個裝置',
        'toast.scanNone': '未找到裝置，請檢查USB連線和驅動',
        'toast.sendingPattern': '傳送測試圖案: {pattern}',
        'toast.cleared': '已清除螢幕', 'toast.playing': '正在播放: {name}', 'toast.playStart': '開始播放...',
        'toast.playDone': '播放完成', 'toast.playFail': '影片播放失敗：FFmpeg 不可用或影片格式不支援',
        'toast.settingsApplied': '設定已套用', 'toast.settingsReset': '設定已重設',
        'toast.scanningUsb': '正在掃描USB裝置...',
        'toast.logsCleared': '日誌已清除', 'toast.saved': '設定已儲存',
        'toast.autostartOn': '自啟動已啟用', 'toast.autostartOff': '自啟動已停用',
        'toast.deviceNotConnected': '裝置未連線', 'toast.patternSent': '測試圖案已傳送',
        'toast.sendFailed': '傳送失敗', 'toast.mediaAdded': '已新增 {count} 個檔案',
        'toast.mediaDeleted': '已刪除: {name}', 'toast.deleteFailed': '刪除失敗',
        'toast.action.closeConflicts': '關閉並重連',
        'playback.playing': '播放中', 'playback.idle': '空閒',
        'log.empty.cleared': '日誌已清除',
    },
    'en': {
        'nav.dashboard': 'Dashboard', 'nav.media': 'Media Library', 'nav.display': 'Display',
        'nav.settings': 'Settings', 'nav.deviceInfo': 'Device Info',
        'page.dashboard.title': 'Dashboard', 'page.dashboard.sub': 'Manage your water cooling screen and media',
        'page.media.title': 'Media Library', 'page.media.sub': 'Manage your images and videos',
        'page.display.title': 'Display', 'page.display.sub': 'Adjust display effects and performance',
        'page.system.title': 'Settings', 'page.system.sub': 'Configure startup, appearance and system options',
        'page.diagnostics.title': 'Device Info', 'page.diagnostics.sub': 'View all USB devices to identify your screen',
        'status.connected': 'Connected', 'status.disconnected': 'Disconnected', 'screen.name': 'Screen {n}',
        'btn.scan': 'Scan Devices', 'btn.uploadImage': 'Upload Image', 'btn.uploadVideo': 'Upload Video',
        'btn.clear': 'Clear Screen', 'btn.clearLogs': 'Clear',
        'btn.save': 'Save Settings', 'btn.apply': 'Apply Settings', 'btn.reset': 'Reset',
        'btn.enable': 'Enable', 'btn.disable': 'Disable', 'btn.uploadFile': 'Upload Files',
        'stat.devices': 'Connected Devices', 'stat.images': 'Images', 'stat.videos': 'Videos',
        'stat.playback': 'Playback', 'stat.imagesSub': 'PNG / JPG / BMP', 'stat.videosSub': 'MP4 / AVI / GIF',
        'filter.all': 'All', 'filter.image': 'Images', 'filter.video': 'Videos',
        'search.placeholder': 'Search...',
        'test.bars': 'Color Bars', 'test.gradient': 'Gradient', 'test.grid': 'Grid',
        'title.min': 'Minimize', 'title.max': 'Maximize', 'title.close': 'Close',
        'dash.preview': 'Now Playing', 'dash.testPattern': 'Test Patterns', 'dash.testDesc': 'Choose a pattern to verify the screen works',
        'media.empty.title': 'No media files', 'media.empty.desc': 'Upload some images or videos to get started',
        'media.type.video': 'Video', 'media.type.gif': 'GIF', 'media.type.image': 'Image',
        'ctx.play': 'Play', 'ctx.delete': 'Delete',
        'display.imageAdjust': 'Image Adjust', 'display.brightness': 'Brightness', 'display.contrast': 'Contrast',
        'display.saturation': 'Saturation', 'display.rotation': 'Rotation', 'display.fitMode': 'Fit Mode',
        'display.fit.cover': 'Fill', 'display.fit.contain': 'Fit', 'display.fit.fill': 'Stretch',
        'display.performance': 'Performance',
        'display.targetFps': 'Screen Refresh Rate', 'display.targetFps.desc': 'Frames sent to the screen per second. Higher is smoother but uses more USB bandwidth',
        'display.videoFps': 'Video Playback FPS', 'display.videoFps.desc': 'Frames played per second, controls playback speed. Keep close to the source video',
        'display.loop': 'Loop Videos', 'display.loop.desc': 'Automatically replay after the video ends',
        'sys.autostart': 'Auto Start', 'sys.autostart.off': 'Auto start disabled', 'sys.autostart.on': 'Auto start enabled',
        'sys.autostart.desc': 'Run automatically when the system starts',
        'sys.startup': 'Startup',
        'sys.startMinimized': 'Start Minimized', 'sys.startMinimized.desc': 'Run minimized on auto start',
        'sys.fastStartup': '⚡ Fast Startup (High Priority)', 'sys.fastStartup.desc': 'Register as the highest priority startup task for the fastest launch',
        'sys.autoplay': 'Auto Play on Startup', 'sys.autoplay.desc': 'Automatically play the selected media after startup and device connection',
        'sys.enableAutoplay': 'Enable Auto Play', 'sys.enableAutoplay.desc': 'Automatically play the image or video selected below after boot',
        'sys.autoplayMedia': 'Media to Play', 'sys.autoplay.none': '(No media selected)',
        'sys.appearance': 'Appearance',
        'sys.minimizeToTray': 'Minimize to Tray', 'sys.minimizeToTray.desc': 'Minimize to tray on close and keep running',
        'sys.language': 'Language', 'sys.about': 'About', 'sys.version': 'Version 1.1.2',
        'sys.author': 'Author', 'sys.protocol': 'Protocol', 'sys.aboutCopy': 'Supports all Thermalright water cooling screens',
        'diag.refresh': 'Refresh Devices', 'diag.known': 'Known Devices', 'diag.allUsb': 'All USB Devices',
        'diag.sidebar.devices': 'Devices',
        'diag.clickRefresh': 'Click "Refresh Devices" to scan', 'diag.logs': 'Live Logs',
        'diag.waitingLogs': 'Waiting for logs...', 'diag.noKnown': 'No known devices', 'diag.noUsb': 'No USB devices detected',
        'diag.found': 'Detected', 'diag.notFound': 'Not detected', 'diag.matched': 'Matches a known device',
        'diag.scanDone': 'Scan complete, found {count} USB devices',
        'toast.scanning': 'Scanning devices...', 'toast.scanFound': 'Found {count} devices',
        'toast.scanNone': 'No devices found. Check USB connection and drivers',
        'toast.sendingPattern': 'Sending test pattern: {pattern}',
        'toast.cleared': 'Screen cleared', 'toast.playing': 'Now playing: {name}', 'toast.playStart': 'Starting playback...',
        'toast.playDone': 'Playback finished', 'toast.playFail': 'Video playback failed: FFmpeg unavailable or unsupported format',
        'toast.settingsApplied': 'Settings applied', 'toast.settingsReset': 'Settings reset',
        'toast.cacheCleared': 'Cache cleared', 'toast.scanningUsb': 'Scanning USB devices...',
        'toast.logsCleared': 'Logs cleared', 'toast.saved': 'Settings saved',
        'toast.autostartOn': 'Auto start enabled', 'toast.autostartOff': 'Auto start disabled',
        'toast.deviceNotConnected': 'Device not connected', 'toast.patternSent': 'Test pattern sent',
        'toast.sendFailed': 'Send failed', 'toast.mediaAdded': 'Added {count} files',
        'toast.mediaDeleted': 'Deleted: {name}', 'toast.deleteFailed': 'Delete failed',
        'toast.action.closeConflicts': 'Close & Reconnect',
        'playback.playing': 'Playing', 'playback.idle': 'Idle',
        'log.empty.cleared': 'Logs cleared',
    },
    'ja': {
        'nav.dashboard': 'ダッシュボード', 'nav.media': 'メディアライブラリ', 'nav.display': '表示',
        'nav.settings': '設定', 'nav.deviceInfo': 'デバイス情報',
        'page.dashboard.title': 'ダッシュボード', 'page.dashboard.sub': '水冷スクリーンとメディアを管理',
        'page.media.title': 'メディアライブラリ', 'page.media.sub': '画像と動画を管理',
        'page.display.title': '表示', 'page.display.sub': '表示効果とパフォーマンスを調整',
        'page.system.title': '設定', 'page.system.sub': '起動・外観・システムオプションを設定',
        'page.diagnostics.title': 'デバイス情報', 'page.diagnostics.sub': 'USBデバイス情報を確認',
        'status.connected': '接続済み', 'status.disconnected': '未接続', 'screen.name': '画面 {n}',
        'btn.scan': 'デバイスをスキャン', 'btn.uploadImage': '画像をアップロード', 'btn.uploadVideo': '動画をアップロード',
        'btn.clear': '画面クリア', 'btn.clearLogs': 'クリア',
        'btn.save': '設定を保存', 'btn.apply': '設定を適用', 'btn.reset': 'リセット',
        'btn.enable': '有効にする', 'btn.disable': '無効にする', 'btn.uploadFile': 'ファイルをアップロード',
        'stat.devices': '接続デバイス', 'stat.images': '画像素材', 'stat.videos': '動画素材',
        'stat.playback': '再生状態', 'stat.imagesSub': 'PNG / JPG / BMP', 'stat.videosSub': 'MP4 / AVI / GIF',
        'filter.all': 'すべて', 'filter.image': '画像', 'filter.video': '動画',
        'search.placeholder': '検索...',
        'test.bars': 'カラーバー', 'test.gradient': 'グラデーション', 'test.grid': 'グリッド',
        'title.min': '最小化', 'title.max': '最大化', 'title.close': '閉じる',
        'dash.preview': '現在再生中', 'dash.testPattern': 'テストパターン', 'dash.testDesc': 'テストパターンで画面の動作を確認',
        'media.empty.title': 'メディアファイルがありません', 'media.empty.desc': '画像や動画をアップロードして開始しましょう',
        'media.type.video': '動画', 'media.type.gif': 'GIF', 'media.type.image': '画像',
        'ctx.play': '再生', 'ctx.delete': '削除',
        'display.imageAdjust': '画像調整', 'display.brightness': '明るさ', 'display.contrast': 'コントラスト',
        'display.saturation': '彩度', 'display.rotation': '画面回転', 'display.fitMode': '表示モード',
        'display.fit.cover': 'フィル', 'display.fit.contain': 'フィット', 'display.fit.fill': 'ストレッチ',
        'display.performance': 'パフォーマンス',
        'display.targetFps': '画面リフレッシュレート', 'display.targetFps.desc': '画面に送る1秒あたりのフレーム数。高いほど滑らかですがUSB帯域を消費します',
        'display.videoFps': '動画再生フレームレート', 'display.videoFps.desc': '動画の再生速度を決める1秒あたりのフレーム数。元動画と揃えるのが推奨',
        'display.loop': '動画をループ再生', 'display.loop.desc': '動画終了後に自動で繰り返す',
        'sys.autostart': '自動起動', 'sys.autostart.off': '自動起動は無効です', 'sys.autostart.on': '自動起動は有効です',
        'sys.autostart.desc': 'システム起動時に自動実行されます',
        'sys.startup': '起動オプション',
        'sys.startMinimized': '最小化して起動', 'sys.startMinimized.desc': '自動起動時に最小化して実行',
        'sys.fastStartup': '⚡ 高速起動（高優先度）', 'sys.fastStartup.desc': '最優先タスクとして登録し、最速で起動',
        'sys.autoplay': '起動時に自動再生', 'sys.autoplay.desc': '起動してデバイス接続後に選択したメディアを自動再生',
        'sys.enableAutoplay': '自動再生を有効化', 'sys.enableAutoplay.desc': '起動後に下で選択した画像または動画を自動再生',
        'sys.autoplayMedia': '再生メディア', 'sys.autoplay.none': '（未選択）',
        'sys.appearance': '外観設定',
        'sys.minimizeToTray': 'トレイに最小化', 'sys.minimizeToTray.desc': '閉じるとトレイに最小化してバックグラウンドで実行',
        'sys.language': '言語', 'sys.about': '情報', 'sys.version': 'バージョン 1.1.2',
        'sys.author': '作者', 'sys.protocol': 'プロトコル', 'sys.aboutCopy': 'Thermalright 水冷スクリーン全シリーズに対応',
        'diag.refresh': 'デバイス一覧を更新', 'diag.known': '既知のデバイス', 'diag.allUsb': 'すべてのUSBデバイス',
        'diag.sidebar.devices': 'デバイス一覧',
        'diag.clickRefresh': '「デバイス一覧を更新」をクリックしてスキャン', 'diag.logs': 'ライブログ',
        'diag.waitingLogs': 'ログを待機中...', 'diag.noKnown': '既知のデバイスなし', 'diag.noUsb': 'USBデバイスを検出できません',
        'diag.found': '検出済み', 'diag.notFound': '未検出', 'diag.matched': '既知デバイスと一致',
        'diag.scanDone': 'スキャン完了、{count} 個のUSBデバイスを発見',
        'toast.scanning': 'デバイスをスキャン中...', 'toast.scanFound': '{count} 個のデバイスを発見',
        'toast.scanNone': 'デバイスが見つかりません。USB接続とドライバを確認してください',
        'toast.sendingPattern': 'テストパターンを送信中: {pattern}',
        'toast.cleared': '画面をクリアしました', 'toast.playing': '再生中: {name}', 'toast.playStart': '再生を開始...',
        'toast.playDone': '再生が完了しました', 'toast.playFail': '動画の再生に失敗：FFmpegが無いか非対応フォーマットです',
        'toast.settingsApplied': '設定を適用しました', 'toast.settingsReset': '設定をリセットしました',
        'toast.cacheCleared': 'キャッシュを削除しました', 'toast.scanningUsb': 'USBデバイスをスキャン中...',
        'toast.logsCleared': 'ログを削除しました', 'toast.saved': '設定を保存しました',
        'toast.autostartOn': '自動起動が有効になりました', 'toast.autostartOff': '自動起動が無効になりました',
        'toast.deviceNotConnected': 'デバイスが接続されていません', 'toast.patternSent': 'テストパターンを送信しました',
        'toast.sendFailed': '送信に失敗しました', 'toast.mediaAdded': '{count} 個のファイルを追加しました',
        'toast.mediaDeleted': '削除しました: {name}', 'toast.deleteFailed': '削除に失敗しました',
        'toast.action.closeConflicts': '終了して再接続',
        'playback.playing': '再生中', 'playback.idle': 'アイドル',
        'log.empty.cleared': 'ログを削除しました',
    },
};

let currentLang = 'zh-CN';

function t(key) {
    const dict = I18N[currentLang] || I18N['zh-CN'];
    return dict[key] !== undefined ? dict[key] : (I18N['zh-CN'][key] ?? key);
}

function tText(key, values) {
    const tmpl = t(key);
    if (!values) return tmpl;
    return tmpl.replace(/\{(\w+)\}/g, (m, k) => (values[k] !== undefined ? values[k] : m));
}

function applyLanguage(lang) {
    if (!I18N[lang]) lang = 'zh-CN';
    currentLang = lang;
    document.documentElement.lang = lang;
    document.title = 'Modern TRCC';
    // 静态元素
    document.querySelectorAll('[data-i18n]').forEach(el => {
        const text = tText(el.dataset.i18n);
        if (text) el.textContent = text;
    });
    document.querySelectorAll('[data-i18n-placeholder]').forEach(el => {
        const text = tText(el.dataset.i18nPlaceholder);
        if (text) el.placeholder = text;
    });
    document.querySelectorAll('[data-i18n-title]').forEach(el => {
        const text = tText(el.dataset.i18nTitle);
        if (text) el.title = text;
    });
    // 动态内容重新翻译
    refreshDynamicText();
}

// ==================== 动态文本刷新 ====================
function refreshDynamicText() {
    if (typeof mediaItems !== 'undefined' && mediaItems) renderMediaList({ items: mediaItems });
    const devEl = document.getElementById('statDeviceName');
    if (devEl && devEl.dataset.conn !== '1') devEl.textContent = tText('status.disconnected');
    const playEl = document.getElementById('statPlayback');
    if (playEl && playEl.dataset.playing === '1') playEl.textContent = tText('playback.playing');
    else if (playEl) playEl.textContent = tText('playback.idle');
    const autoText = document.getElementById('autoStartText');
    if (autoText && autoText.dataset.state === '1') autoText.textContent = tText('sys.autostart.on');
    else if (autoText) autoText.textContent = tText('sys.autostart.off');
    const autoBtn = document.getElementById('autoStartBtn');
    if (autoBtn) {
        autoBtn.textContent = autoStartEnabled ? tText('btn.disable') : tText('btn.enable');
    }
    const logEmpty = document.querySelector('#logPanel .log-empty');
    if (logEmpty) logEmpty.textContent = tText('diag.waitingLogs');
    const usbEmpty = document.querySelector('#usbDeviceList .empty-state p');
    if (usbEmpty && !usbEmpty.dataset.rendered) usbEmpty.textContent = tText('diag.clickRefresh');
}

// ==================== 顶栏导航 ====================
const navItems = document.querySelectorAll('.top-nav-item');
const pages = document.querySelectorAll('.page');

navItems.forEach(item => {
    item.addEventListener('click', () => {
        const page = item.dataset.page;
        navItems.forEach(n => n.classList.remove('active'));
        item.classList.add('active');
        pages.forEach(p => p.classList.remove('active'));
        document.getElementById('page-' + page).classList.add('active');
    });
});

// ==================== 页内侧边栏切换 ====================
// 显示设置/设置/设备信息页：点击侧边栏按钮切换右侧内容区
document.querySelectorAll('.page-split').forEach(split => {
    const sidebar = split.querySelector('.page-sidebar');
    const content = split.querySelector('.page-content');
    if (!sidebar || !content) return;

    sidebar.addEventListener('click', (e) => {
        const btn = e.target.closest('.sidebar-item');
        if (!btn) return;
        sidebar.querySelectorAll('.sidebar-item').forEach(b => b.classList.remove('active'));
        btn.classList.add('active');
        const pane = content.querySelector('#pane-' + btn.dataset.section);
        if (!pane) return;
        content.querySelectorAll('.section-pane').forEach(p => p.classList.remove('active'));
        pane.classList.add('active');
    });
});

// ==================== 窗口控制 ====================
document.getElementById('minBtn')?.addEventListener('click', () => {
    postToCSharp('windowMinimize');
});
document.getElementById('closeBtn')?.addEventListener('click', () => {
    postToCSharp('windowClose');
});

// 标题栏拖动：整条顶栏均可拖动，拖动距离超过阈值才启动，避免影响导航点击
let dragState = null;
document.querySelector('.titlebar')?.addEventListener('mousedown', (e) => {
    if (e.button !== 0) return;
    if (e.target.closest('.win-btn')) return;
    dragState = { startX: e.screenX, startY: e.screenY, dragging: false };
});
window.addEventListener('mousemove', (e) => {
    if (!dragState || dragState.dragging) return;
    if (Math.abs(e.screenX - dragState.startX) > 3 || Math.abs(e.screenY - dragState.startY) > 3) {
        dragState.dragging = true;
        postToCSharp('windowDrag');
    }
});
window.addEventListener('mouseup', () => { dragState = null; });

// ==================== C# 通信桥 ====================
function postToCSharp(action, data = {}) {
    const msg = JSON.stringify({ action, data });
    if (window.chrome && window.chrome.webview) {
        // 用 postMessage 发送字符串，C# 端用 WebMessageAsString 接收
        window.chrome.webview.postMessage(msg);
    } else {
        console.log('[C# Bridge - no webview]', action, data);
    }
}

// 接收 C# 消息
function onCSharpMessage(e) {
    let msg;
    try { 
        msg = JSON.parse(e.data); 
    } catch { 
        console.error('[Parse error]', e.data);
        return; 
    }
    handleMessage(msg);
}

// 确保事件监听器只注册一次
if (window.chrome && window.chrome.webview) {
    window.chrome.webview.addEventListener('message', onCSharpMessage);
}

function handleMessage(msg) {
    switch (msg.action) {
        case 'screensList':
            updateScreensList(msg.data);
            break;
        case 'deviceStatus':
            updateDeviceStatus(msg.data);
            break;
        case 'scanResult':
            updateScanResult(msg.data);
            break;
        case 'mediaList':
            renderMediaList(msg.data);
            break;
        case 'settingsLoaded':
            loadSettings(msg.data);
            break;
        case 'autoStartStatus':
            updateAutoStartStatus(msg.data);
            break;
        case 'toast':
            if (msg.data.key) showToast(tText(msg.data.key, msg.data.values || {}), msg.data.type || 'info', msg.data.action || null);
            else showToast(msg.data.message, msg.data.type || 'info', msg.data.action || null);
            break;
        case 'playbackStatus':
            updatePlaybackStatus(msg.data);
            break;
        case 'diagnosticsData':
            renderDiagnostics(msg.data);
            break;
        case 'log':
            appendLog(msg.data);
            break;
        case 'allLogs':
            renderAllLogs(msg.data);
            break;
    }
}

// ==================== 设备状态 ====================
function updateDeviceStatus(data) {
    // 非当前屏的状态只更新切换条徽标，不刷新主界面
    if (data.screen !== undefined && data.screen !== currentScreen) {
        updateScreenBarBadge(data);
        return;
    }
    const dot = document.getElementById('statusDot');
    const text = document.getElementById('statusText');
    const statDeviceName = document.getElementById('statDeviceName');
    const screenRes = document.getElementById('screenRes');

    if (data.connected) {
        dot.classList.add('connected');
        text.textContent = tText('status.connected');
        statDeviceName.textContent = data.deviceName || tText('status.connected');
        statDeviceName.dataset.conn = '1';
        screenRes.textContent = data.resolution || '--×--';
    } else {
        dot.classList.remove('connected');
        text.textContent = tText('status.disconnected');
        statDeviceName.textContent = tText('status.disconnected');
        statDeviceName.dataset.conn = '0';
        screenRes.textContent = '--×--';
    }
}

function updateScanResult(data) {
    document.getElementById('statDevices').textContent = data.count || 0;
    if (data.count > 0) {
        showToast(tText('toast.scanFound', { count: data.count }), 'success');
    } else {
        showToast(tText('toast.scanNone'), 'warning');
    }
}

// ==================== 多屏幕切换 ====================
function updateScreensList(data) {
    screensData = data.screens || [];
    // 当前屏幕越界（如只剩单屏）时回退到 0，并重新拉取该屏设置
    const prev = currentScreen;
    if (!screensData.some(s => s.index === currentScreen)) {
        currentScreen = 0;
        if (prev !== currentScreen) {
            postToCSharp('getSettings', { screen: currentScreen });
            postToCSharp('getDeviceStatus', { screen: currentScreen });
            postToCSharp('getPlaybackStatus', { screen: currentScreen });
        }
    }
    renderScreenBar();
    const statDevices = document.getElementById('statDevices');
    if (statDevices) {
        const cnt = data.connectedCount !== undefined ? data.connectedCount : screensData.filter(s => s.connected).length;
        statDevices.textContent = cnt;
    }
    updateScreenLabels();
}

// 渲染顶部屏幕切换条：仅当检测到多块屏幕时显示
function renderScreenBar() {
    const bar = document.getElementById('screenBar');
    const inner = document.getElementById('screenBarInner');
    if (!bar || !inner) return;

    if (!screensData || screensData.length < 2) {
        bar.style.display = 'none';
        inner.innerHTML = '';
        return;
    }
    bar.style.display = 'flex';
    inner.innerHTML = '';
    screensData.forEach(s => {
        const btn = document.createElement('button');
        btn.className = 'screen-bar-item' + (s.index === currentScreen ? ' active' : '');
        btn.dataset.screen = s.index;
        const res = s.resolution ? ` · ${s.resolution}` : '';
        btn.textContent = s.name + res;
        btn.addEventListener('click', () => switchScreen(s.index));
        inner.appendChild(btn);
    });
}

// 非当前屏的 deviceStatus 只更新切换条上的连接标记
function updateScreenBarBadge(data) {
    const inner = document.getElementById('screenBarInner');
    if (!inner) return;
    const btn = inner.querySelector(`.screen-bar-item[data-screen="${data.screen}"]`);
    if (btn) btn.classList.toggle('connected', !!data.connected);
}

// 切换当前管理的屏幕：刷新切换条并重新拉取该屏设置与状态
function switchScreen(index) {
    if (index === currentScreen) return;
    currentScreen = index;
    renderScreenBar();
    updateScreenLabels();
    postToCSharp('getSettings', { screen: index });
    postToCSharp('getDeviceStatus', { screen: index });
    postToCSharp('getPlaybackStatus', { screen: index });
}

// 更新「显示」页与「开机自动播放」处的当前屏幕标签
function updateScreenLabels() {
    const cur = screensData.find(s => s.index === currentScreen);
    const label = cur
        ? (cur.name || tText('screen.name', { n: cur.index + 1 })) + (cur.resolution ? ` · ${cur.resolution}` : '')
        : '';
    document.querySelectorAll('.cur-screen-label').forEach(el => {
        el.textContent = label;
    });
}

// ==================== 仪表盘操作 ====================
function scanDevices() {
    showToast(tText('toast.scanning'));
    postToCSharp('scanDevices');
}

function sendTestPattern(pattern) {
    postToCSharp('sendTestPattern', { pattern, screen: currentScreen });
    showToast(tText('toast.sendingPattern', { pattern }));
}

function clearDisplay() {
    postToCSharp('clearDisplay', { screen: currentScreen });
    showToast(tText('toast.cleared'));
}

function updatePlaybackStatus(data) {
    if (data.screen !== undefined && data.screen !== currentScreen) return;
    const el = document.getElementById('statPlayback');
    const fps = document.getElementById('statFps');
    el.dataset.playing = data.playing ? '1' : '0';
    el.textContent = data.playing ? tText('playback.playing') : tText('playback.idle');
    fps.textContent = (data.fps || '--') + ' FPS';
    renderStaticPreview(data);
}

// ==================== 静态屏幕预览 ====================
// 不再实时推送屏幕帧，改为静态展示当前播放媒体的缩略图与信息
function renderStaticPreview(data) {
    const img = document.getElementById('screenPreviewImg');
    const info = document.getElementById('previewInfo');
    const res = document.getElementById('screenRes');
    const overlay = document.getElementById('screenPreview');
    const media = data && data.media;

    if (!media || !media.thumbnail) {
        if (img) { img.style.display = 'none'; img.removeAttribute('src'); }
        if (info) info.style.display = 'none';
        if (res) res.textContent = '--×--';
        if (overlay) overlay.classList.remove('has-image');
        return;
    }

    if (img) { img.src = media.thumbnail; img.style.display = 'block'; }
    if (overlay) overlay.classList.add('has-image');
    if (res) res.textContent = media.resolution || '--×--';

    if (info) {
        const lines = [media.name || ''];
        if (media.type === 'video' || media.type === 'gif') {
            if (media.duration > 0) lines.push(formatDuration(media.duration));
            if (media.fps > 0) lines.push(media.fps.toFixed(1) + ' FPS');
        }
        if (media.fileSize > 0) lines.push(formatSize(media.fileSize));
        info.innerHTML = lines.map(l => '<div class="preview-info-line">' + escapeHtml(l) + '</div>').join('');
        info.style.display = 'block';
    }
}

function formatDuration(ms) {
    const s = Math.round(ms / 1000);
    const h = Math.floor(s / 3600);
    const m = Math.floor((s % 3600) / 60);
    const sec = s % 60;
    const pad = n => String(n).padStart(2, '0');
    return h > 0 ? `${h}:${pad(m)}:${pad(sec)}` : `${m}:${pad(sec)}`;
}

function formatSize(bytes) {
    if (!bytes || bytes <= 0) return '';
    const units = ['B', 'KB', 'MB', 'GB'];
    let i = 0, v = bytes;
    while (v >= 1024 && i < units.length - 1) { v /= 1024; i++; }
    return v.toFixed(v >= 100 || i === 0 ? 0 : 1) + ' ' + units[i];
}

// ==================== 媒体库 ====================
let currentFilter = 'all';
let mediaItems = [];
// 当前管理的屏幕索引与后端推送的屏幕列表（多屏切换）
let currentScreen = 0;
let screensData = [];
// 服务端保存的自动播放媒体 ID（媒体列表加载完成后据此回填下拉框，
// 解决“列表先于设置到达导致选中丢失”的问题）
let savedAutoplayMediaId = '';

function setFilter(filter) {
    currentFilter = filter;
    document.querySelectorAll('.filter-tab').forEach(t => {
        t.classList.toggle('active', t.dataset.filter === filter);
    });
    filterMedia();
}

function filterMedia() {
    const search = document.getElementById('searchInput').value.toLowerCase();
    const filtered = mediaItems.filter(m => {
        if (currentFilter === 'image' && m.type !== 'image') return false;
        if (currentFilter === 'video' && m.type !== 'video' && m.type !== 'gif') return false;
        if (search && !m.name.toLowerCase().includes(search)) return false;
        return true;
    });
    renderMediaList({ items: filtered, preserveSource: true });
}

function mediaTypeLabel(type) {
    if (type === 'video') return tText('media.type.video');
    if (type === 'gif') return tText('media.type.gif');
    return tText('media.type.image');
}

function renderMediaList(data) {
    // 只有从服务端加载完整列表时才更新 mediaItems
    if (data.items && !data.preserveSource) {
        mediaItems = data.items;
        // 同步仪表盘素材计数
        const imgCount = mediaItems.filter(m => m.type === 'image').length;
        const vidCount = mediaItems.filter(m => m.type === 'video' || m.type === 'gif').length;
        const imgEl = document.getElementById('statImages');
        const vidEl = document.getElementById('statVideos');
        if (imgEl) imgEl.textContent = imgCount;
        if (vidEl) vidEl.textContent = vidCount;
        populateAutoplayMediaList();
    }
    const grid = document.getElementById('mediaGrid');
    const items = data.items || mediaItems;

    if (!items || items.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                <h3>${tText('media.empty.title')}</h3>
                <p>${tText('media.empty.desc')}</p>
                <button class="btn btn-primary" onclick="uploadMedia('all')">${tText('btn.uploadFile')}</button>
            </div>`;
        return;
    }

    grid.innerHTML = items.map(m => `
        <div class="media-item" data-id="${m.id}" onclick="playMedia('${m.id}')">
            <div class="media-thumb">
                ${m.thumbnail ? `<img src="${m.thumbnail}" alt="${m.name}">` : ''}
            </div>
            <div class="media-type">${mediaTypeLabel(m.type)}</div>
            <div class="media-info">
                <div class="media-name">${escapeHtml(m.name)}</div>
                <div class="media-res">${m.resolution || ''}</div>
            </div>
        </div>
    `).join('');
}

function uploadMedia(type) {
    postToCSharp('uploadMedia', { type });
}

function playMedia(id) {
    postToCSharp('playMedia', { id, screen: currentScreen });
    showToast(tText('toast.playStart'));
}

// 填充“开机自动播放”媒体下拉框，保持当前已选项
function populateAutoplayMediaList() {
    const select = document.getElementById('autoplayMedia');
    if (!select) return;
    select.innerHTML = '<option value="">' + tText('sys.autoplay.none') + '</option>';
    if (mediaItems && mediaItems.length) {
        mediaItems.forEach(m => {
            const option = document.createElement('option');
            option.value = m.id;
            option.textContent = m.name;
            select.appendChild(option);
        });
    }
    // 优先按服务端保存的 ID 回填
    if (savedAutoplayMediaId) select.value = savedAutoplayMediaId;
    else select.value = '';
}

// ==================== 媒体右键菜单 ====================
let _ctxTargetId = null;

function initMediaContextMenu() {
    const grid = document.getElementById('mediaGrid');
    const menu = document.getElementById('ctxMenu');
    if (!grid || !menu) return;

    // 右键媒体项：定位并显示菜单
    grid.addEventListener('contextmenu', (e) => {
        const item = e.target.closest('.media-item');
        if (!item) return;
        e.preventDefault();
        _ctxTargetId = item.dataset.id;
        menu.style.display = 'block';
        // 限制在窗口内
        const rect = menu.getBoundingClientRect();
        const x = Math.min(e.clientX, window.innerWidth - rect.width - 8);
        const y = Math.min(e.clientY, window.innerHeight - rect.height - 8);
        menu.style.left = x + 'px';
        menu.style.top = y + 'px';
    });

    // 点击其他区域/滚动时隐藏
    document.addEventListener('click', hideContextMenu);
    document.addEventListener('scroll', hideContextMenu, true);
    document.addEventListener('contextmenu', (e) => {
        if (!e.target.closest('.media-item')) hideContextMenu();
    });

    document.getElementById('ctxPlay')?.addEventListener('click', () => {
        if (_ctxTargetId) playMedia(_ctxTargetId);
        hideContextMenu();
    });
    document.getElementById('ctxDelete')?.addEventListener('click', () => {
        if (_ctxTargetId) deleteMedia(_ctxTargetId);
        hideContextMenu();
    });
}

function hideContextMenu() {
    const menu = document.getElementById('ctxMenu');
    if (menu) menu.style.display = 'none';
}

function deleteMedia(id) {
    postToCSharp('deleteMedia', { id });
}

// ==================== 设置 ====================
let _settingsApplyTimer = null;

function updateSlider(id, value, suffix) {
    document.getElementById('val' + id.charAt(0).toUpperCase() + id.slice(1)).textContent = value + suffix;
    scheduleApply();
}

function setOption(key, value, btn) {
    btn.parentElement.querySelectorAll('.option-tab').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
    scheduleApply();
}

function getDisplaySettings() {
    const rotationEl = document.querySelector('.option-tab.active[data-rotation]');
    const fitEl = document.querySelector('.option-tab.active[data-fit]');
    return {
        screen: currentScreen,
        brightness: +document.getElementById('brightness').value,
        contrast: +document.getElementById('contrast').value,
        saturation: +document.getElementById('saturation').value,
        targetFps: +document.getElementById('targetFps').value,
        videoFps: +document.getElementById('videoFps').value,
        loopVideo: document.getElementById('loopVideo').checked,
        rotation: rotationEl ? +rotationEl.dataset.rotation : 0,
        fitMode: fitEl ? fitEl.dataset.fit : 'cover',
    };
}

// 调整后自动生效：防抖 400ms 后应用（避免拖动滑块时频繁触发）
function scheduleApply() {
    clearTimeout(_settingsApplyTimer);
    _settingsApplyTimer = setTimeout(() => {
        postToCSharp('applySettings', getDisplaySettings());
    }, 400);
}

function loadSettings(data) {
    // 非当前屏的设置到达时忽略（切换屏幕后各自拉取）
    if (data.screen !== undefined && data.screen !== currentScreen) return;
    if (data.brightness !== undefined) document.getElementById('brightness').value = data.brightness;
    if (data.contrast !== undefined) document.getElementById('contrast').value = data.contrast;
    if (data.saturation !== undefined) document.getElementById('saturation').value = data.saturation;
    if (data.targetFps !== undefined) document.getElementById('targetFps').value = data.targetFps;
    if (data.videoFps !== undefined) document.getElementById('videoFps').value = data.videoFps;
    if (data.loopVideo !== undefined) document.getElementById('loopVideo').checked = data.loopVideo;
    if (data.startMinimized !== undefined) document.getElementById('startMinimized').checked = data.startMinimized;
    if (data.highPriority !== undefined) document.getElementById('fastStartup').checked = data.highPriority;
    if (data.minimizeToTray !== undefined) document.getElementById('minimizeToTray').checked = data.minimizeToTray;
    if (data.enableAutoplay !== undefined) document.getElementById('enableAutoplay').checked = data.enableAutoplay;
    if (data.autoplayMediaId !== undefined) {
        savedAutoplayMediaId = data.autoplayMediaId;
        const select = document.getElementById('autoplayMedia');
        if (select) select.value = data.autoplayMediaId;
    }

    if (data.rotation !== undefined) {
        document.querySelectorAll('[data-rotation]').forEach(t => {
            t.classList.toggle('active', +t.dataset.rotation === data.rotation);
        });
    }
    if (data.fitMode) {
        document.querySelectorAll('[data-fit]').forEach(t => {
            t.classList.toggle('active', t.dataset.fit === data.fitMode);
        });
    }

    const langEl = document.getElementById('language');
    if (data.language && langEl) {
        langEl.value = data.language;
        applyLanguage(data.language);
    }

    updateSliderLabel('brightness', data.brightness !== undefined ? data.brightness : 100, '%');
    updateSliderLabel('contrast', data.contrast !== undefined ? data.contrast : 100, '%');
    updateSliderLabel('saturation', data.saturation !== undefined ? data.saturation : 100, '%');
    updateSliderLabel('targetFps', data.targetFps !== undefined ? data.targetFps : 30, ' FPS');
    updateSliderLabel('videoFps', data.videoFps !== undefined ? data.videoFps : 30, ' FPS');
}

// 仅更新滑块数值显示，不触发自动应用（用于从后端回读设置）
function updateSliderLabel(id, value, suffix) {
    document.getElementById('val' + id.charAt(0).toUpperCase() + id.slice(1)).textContent = value + suffix;
}

function applySettings() {
    postToCSharp('applySettings', getDisplaySettings());
    showToast(tText('toast.settingsApplied'), 'success');
}

function resetSettings() {
    loadSettings({
        brightness: 100, contrast: 100, saturation: 100, rotation: 0,
        targetFps: 30, videoFps: 30, loopVideo: true, fitMode: 'cover'
    });
    postToCSharp('applySettings', getDisplaySettings());
    showToast(tText('toast.settingsReset'));
}

// 性能设置开关也立即生效
document.getElementById('loopVideo')?.addEventListener('change', scheduleApply);

// ==================== 自启动 ====================
let autoStartEnabled = false;

function updateAutoStartStatus(data) {
    autoStartEnabled = data.enabled;
    const dot = document.querySelector('.autostart-dot');
    const text = document.getElementById('autoStartText');
    const btn = document.getElementById('autoStartBtn');

    if (data.enabled) {
        dot.classList.add('active');
        text.dataset.state = '1';
        text.textContent = tText('sys.autostart.on');
        btn.textContent = tText('btn.disable');
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-ghost');
    } else {
        dot.classList.remove('active');
        text.dataset.state = '0';
        text.textContent = tText('sys.autostart.off');
        btn.textContent = tText('btn.enable');
        btn.classList.add('btn-primary');
        btn.classList.remove('btn-ghost');
    }
}

function toggleAutoStart() {
    if (autoStartEnabled) {
        postToCSharp('disableAutoStart');
    } else {
        const startMinimized = document.getElementById('startMinimized').checked;
        const highPriority = document.getElementById('fastStartup').checked;
        postToCSharp('enableAutoStart', { startMinimized, highPriority });
    }
}

function getSystemSettings() {
    const langEl = document.getElementById('language');
    return {
        screen: currentScreen,
        startMinimized: document.getElementById('startMinimized').checked,
        highPriority: document.getElementById('fastStartup').checked,
        minimizeToTray: document.getElementById('minimizeToTray').checked,
        language: langEl ? langEl.value : 'zh-CN',
        enableAutoplay: document.getElementById('enableAutoplay').checked,
        autoplayMedia: document.getElementById('autoplayMedia').value,
    };
}

function saveSystemSettings() {
    postToCSharp('saveSystemSettings', getSystemSettings());
    showToast(tText('toast.saved'), 'success');
}

// 语言切换：立即应用并保存
document.getElementById('language')?.addEventListener('change', (e) => {
    const lang = e.target.value;
    applyLanguage(lang);
    postToCSharp('saveSystemSettings', getSystemSettings());
    showToast(tText('toast.saved'), 'success');
});

// 启动选项/开机自动播放：变更后立即自动保存（无需“保存设置”按钮）
document.getElementById('startMinimized')?.addEventListener('change', saveSystemSettings);
document.getElementById('fastStartup')?.addEventListener('change', saveSystemSettings);
document.getElementById('minimizeToTray')?.addEventListener('change', saveSystemSettings);
document.getElementById('enableAutoplay')?.addEventListener('change', saveSystemSettings);
document.getElementById('autoplayMedia')?.addEventListener('change', (e) => {
    savedAutoplayMediaId = e.target.value;
    saveSystemSettings();
});

// ==================== Toast ====================
function showToast(message, type = 'info', action = null) {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = 'toast ' + type;
    if (action) {
        // 带操作按钮的 toast：消息 + 按钮（如“关闭并重连”）
        const text = document.createElement('span');
        text.textContent = message;
        const btn = document.createElement('button');
        btn.className = 'toast-action';
        btn.textContent = tText('toast.action.' + action);
        btn.addEventListener('click', () => {
            postToCSharp(action);
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(120%)';
            toast.style.transition = '0.3s';
            setTimeout(() => toast.remove(), 300);
        });
        toast.appendChild(text);
        toast.appendChild(btn);
    } else {
        toast.textContent = message;
    }
    container.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(120%)';
        toast.style.transition = '0.3s';
        setTimeout(() => toast.remove(), 300);
    }, 6000);
}

// ==================== 设备信息 ====================
// 启动时 init 扫描 和 getDiagnostics 都会触发 renderDiagnostics，
// 只有用户手动点击“刷新”时才弹“扫描完成”toast，避免启动时弹出两次。
let _diagToastRequested = false;
function refreshDiagnostics() {
    showToast(tText('toast.scanningUsb'));
    _diagToastRequested = true;
    postToCSharp('getDiagnostics');
}

function clearLogs() {
    document.getElementById('logPanel').innerHTML = `<div class="log-empty">${tText('toast.logsCleared')}</div>`;
    postToCSharp('clearLogs');
}

function appendLog(entry) {
    const panel = document.getElementById('logPanel');
    // 移除空状态
    const empty = panel.querySelector('.log-empty');
    if (empty) empty.remove();

    const line = document.createElement('div');
    line.className = 'log-line ' + (entry.Level || 'info').toLowerCase();
    line.innerHTML = `<span class="log-time">${entry.Timestamp}</span><span class="log-level">${entry.Level}</span><span class="log-msg">${escapeHtml(entry.Message)}</span>`;
    panel.appendChild(line);
    panel.scrollTop = panel.scrollHeight;

    // 限制 500 条
    while (panel.children.length > 500) panel.removeChild(panel.firstChild);
}

function renderAllLogs(data) {
    const panel = document.getElementById('logPanel');
    panel.innerHTML = '';
    if (!data.logs || data.logs.length === 0) {
        panel.innerHTML = `<div class="log-empty">${tText('diag.waitingLogs')}</div>`;
        return;
    }
    data.logs.forEach(entry => {
        const line = document.createElement('div');
        line.className = 'log-line ' + (entry.Level || 'info').toLowerCase();
        line.innerHTML = `<span class="log-time">${entry.Timestamp}</span><span class="log-level">${entry.Level}</span><span class="log-msg">${escapeHtml(entry.Message)}</span>`;
        panel.appendChild(line);
    });
    panel.scrollTop = panel.scrollHeight;
}

function escapeHtml(text) {
    if (!text) return '';
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

function renderDiagnostics(data) {
    if (_diagToastRequested) {
        showToast(tText('diag.scanDone', { count: data.usbDevices?.length || 0 }), 'success');
        _diagToastRequested = false;
    }

    // 已知设备列表
    const knownEl = document.getElementById('knownDevices');
    if (data.knownDevices && data.knownDevices.length > 0) {
        knownEl.innerHTML = data.knownDevices.map(d => `
            <div class="known-device">
                <span class="kd-vidpid">VID:${d.vid} PID:${d.pid}</span>
                <span class="kd-name">${escapeHtml(d.name)} (${escapeHtml(d.brand)})</span>
                <span class="kd-status ${d.found ? 'found' : ''}">${d.found ? tText('diag.found') : tText('diag.notFound')}</span>
            </div>
        `).join('');
    } else {
        knownEl.innerHTML = `<div style="padding:20px;text-align:center;color:var(--text-tertiary);font-size:13px">${tText('diag.noKnown')}</div>`;
    }

    // 所有USB设备
    const listEl = document.getElementById('usbDeviceList');
    if (data.usbDevices && data.usbDevices.length > 0) {
        const knownVids = new Set((data.knownDevices || []).map(d => parseInt(d.vid, 16)));
        listEl.innerHTML = data.usbDevices.map(d => {
            const matched = knownVids.has(parseInt(d.vidStr, 16));
            return `
                <div class="usb-device ${matched ? 'matched' : ''}">
                    <span class="ud-vidpid">VID:${d.vidStr} PID:${d.pidStr}</span>
                    <span class="ud-name">${escapeHtml(d.name)}</span>
                    <span class="ud-status">${matched ? tText('diag.matched') : escapeHtml(d.status || '')}</span>
                </div>
            `;
        }).join('');
    } else {
        listEl.innerHTML = `<div class="empty-state" style="padding:30px"><p data-rendered="1">${tText('diag.noUsb')}</p></div>`;
    }
}

// ==================== 初始化 ====================
// 用 window.onload 确保 WebView2 完全就绪
window.addEventListener('load', () => {
    initMediaContextMenu();
    // 延迟一点确保 webview bridge 就绪
    setTimeout(() => {
        postToCSharp('init');
        postToCSharp('getScreens');
        postToCSharp('getSettings');
        postToCSharp('getAutoStartStatus');
        postToCSharp('getMediaList');
        postToCSharp('getDiagnostics');
        postToCSharp('getLogs');
    }, 100);
});
