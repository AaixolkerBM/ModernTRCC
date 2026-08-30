using LibUsbDotNet;
using LibUsbDotNet.Main;
using ModernTRCC.Core.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DrawPixelFormat = System.Drawing.Imaging.PixelFormat;

namespace ModernTRCC.Core.Services;

/// <summary>
/// LCD 控制器 - 精确匹配原版 TRCC 协议
/// 协议来源：USBLCDNEW.dll 反编译
/// </summary>
public class LcdController : IDisposable, INotifyPropertyChanged
{
    #region 设备定义

    private static readonly List<(int Vid, int Pid, LcdDeviceType Type, string Name, string Brand)> KnownDevices = new()
    {
        // SCSI 协议 (0x87CD 和 0x87AD 都用同一个处理函数)
        (0x87CD, 0x70DB, LcdDeviceType.Scsi, "Thermalright LCD", "Thermalright"),
        (0x87AD, 0x70DB, LcdDeviceType.Scsi, "Thermalright LCD", "Thermalright"),
        // HID 协议
        (0x0416, 0x5302, LcdDeviceType.Hid, "ALi HID LCD", "ALi"),
        // ALi 协议
        (0x0416, 0x5406, LcdDeviceType.Ali, "ALi Corp LCD", "ALi"),
        // LY 协议
        (0x0416, 0x5408, LcdDeviceType.Ly, "ALi LY LCD", "ALi"),
        // LY1 协议
        (0x0416, 0x5409, LcdDeviceType.Ly1, "ALi LY1 LCD", "ALi"),
    };

    #endregion

    #region 字段

    private UsbDevice? _activeDevice;
    private UsbEndpointWriter? _writer;
    private UsbEndpointReader? _reader;
    private readonly object _sendLock = new();
    private int _targetFps = 30;
    private long _frameCount;
    private bool _isRunning;
    private Thread? _sendThread;
    private readonly Queue<byte[]> _frameQueue = new();
    private readonly AutoResetEvent _frameEvent = new(false);

    #endregion

    #region 属性

    public ObservableCollection<LcdDeviceInfo> Devices { get; } = new();

    private LcdDeviceInfo? _activeDeviceInfo;
    public LcdDeviceInfo? ActiveDeviceInfo
    {
        get => _activeDeviceInfo;
        private set { _activeDeviceInfo = value; OnPropertyChanged(); }
    }

    public int TargetFps
    {
        get => _targetFps;
        set { _targetFps = Math.Max(1, Math.Min(60, value)); OnPropertyChanged(); }
    }

    public long FrameCount => _frameCount;
    public bool IsConnected => _activeDevice != null && _isRunning;

    /// <summary>最近一次入队的 RGB888 帧（用于实时预览镜像）</summary>
    public byte[]? LastFrame { get; private set; }

    /// <summary>最近一次连接失败的具体原因（供 UI 展示给用户）</summary>
    public string LastError { get; private set; } = "";

    #endregion

    #region 事件

    public event EventHandler<LcdDeviceInfo>? DeviceConnected;
    public event EventHandler<long>? FrameSent;
    public event EventHandler<string>? ErrorOccurred;
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    #endregion

    #region 设备扫描

    public int ScanDevices()
    {
        Devices.Clear();
        var allDevices = UsbDevice.AllDevices;

        foreach (var (vid, pid, type, name, brand) in KnownDevices)
        {
            foreach (UsbRegistry reg in allDevices)
            {
                if (reg.Vid == vid && reg.Pid == pid)
                {
                    var info = new LcdDeviceInfo
                    {
                        Name = name, Brand = brand, Vid = vid, Pid = pid, Type = type
                    };
                    // 默认 480x480，连接后通过握手获取实际分辨率
                    info.Width = 480;
                    info.Height = 480;
                    Devices.Add(info);
                }
            }
        }
        return Devices.Count;
    }

    #endregion

    #region 连接（精确匹配原版协议）

    public bool Connect(LcdDeviceInfo deviceInfo)
    {
        try
        {
            Disconnect();

            var finder = new UsbDeviceFinder(deviceInfo.Vid, deviceInfo.Pid);
            var device = UsbDevice.OpenUsbDevice(finder);
            if (device == null)
            {
                LastError = "无法打开 USB 设备（可能被其他程序占用或驱动异常）";
                ErrorOccurred?.Invoke(this, LastError);
                return false;
            }

            _activeDevice = device;
            ActiveDeviceInfo = deviceInfo;

            // 原版：SetConfiguration(1) + ClaimInterface(0)
            if (device is IUsbDevice usbDev)
            {
                usbDev.SetConfiguration(1);
                usbDev.ClaimInterface(0);
            }

            // 原版 SCSI 设备：Ep01 读写
            // 原版 HID 设备：Ep01 读, Ep02 写
            // 原版 ALi 设备：Ep01 读, Ep02 写
            // 原版 LY 设备：Ep01 读, Ep09 写
            switch (deviceInfo.Type)
            {
                case LcdDeviceType.Scsi:
                    _reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                    _writer = device.OpenEndpointWriter(WriteEndpointID.Ep01);
                    break;
                case LcdDeviceType.Hid:
                    _reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                    _writer = device.OpenEndpointWriter(WriteEndpointID.Ep02);
                    break;
                case LcdDeviceType.Ali:
                    _reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                    _writer = device.OpenEndpointWriter(WriteEndpointID.Ep02);
                    break;
                case LcdDeviceType.Ly:
                case LcdDeviceType.Ly1:
                    _reader = device.OpenEndpointReader(ReadEndpointID.Ep01);
                    _writer = device.OpenEndpointWriter(WriteEndpointID.Ep09);
                    break;
            }

            // 执行握手
            bool handshakeOk = deviceInfo.Type switch
            {
                LcdDeviceType.Scsi => ScsiHandshake(),
                LcdDeviceType.Hid => HidHandshake(),
                LcdDeviceType.Ali => AliHandshake(),
                LcdDeviceType.Ly => LyHandshake(false),
                LcdDeviceType.Ly1 => LyHandshake(true),
                _ => false
            };

            if (!handshakeOk)
            {
                LastError = "设备握手失败（协议不匹配或设备未就绪）";
                ErrorOccurred?.Invoke(this, LastError);
                Disconnect();
                return false;
            }

            // 记录握手结果
            ErrorOccurred?.Invoke(this, $"[INFO] 握手成功! 分辨率: {deviceInfo.Width}x{deviceInfo.Height}");

            deviceInfo.IsInitialized = true;
            _isRunning = true;
            _sendThread = new Thread(SendThreadProc)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            };
            _sendThread.Start();

            LastError = "";
            DeviceConnected?.Invoke(this, deviceInfo);
            return true;
        }
        catch (Exception ex)
        {
            LastError = ex.Message;
            ErrorOccurred?.Invoke(this, ex.Message);
            return false;
        }
    }

    public void Disconnect()
    {
        _isRunning = false;
        _frameEvent.Set();

        // 等待发送线程退出，确保设备句柄被释放后再重连，避免“设备被占用”
        try { _sendThread?.Join(500); } catch { }
        _sendThread = null;

        try
        {
            if (_activeDevice is IUsbDevice usbDev)
                usbDev.ReleaseInterface(0);
        }
        catch { }

        try { _reader?.Dispose(); } catch { }
        try { _writer?.Dispose(); } catch { }
        _reader = null;
        _writer = null;

        try { _activeDevice?.Close(); } catch { }
        _activeDevice = null;

        if (ActiveDeviceInfo != null)
            ActiveDeviceInfo.IsInitialized = false;
        ActiveDeviceInfo = null;

        lock (_sendLock) _frameQueue.Clear();
    }

    #endregion

    #region 握手协议（精确匹配原版）

    /// <summary>
    /// SCSI 握手 - 发送 [0x12, 0x34, 0x56, 0x78...] 64字节，读取 1024 字节响应
    /// </summary>
    private bool ScsiHandshake()
    {
        // 原版命令：64 字节，byte[0-3]=[0x12,0x34,0x56,0x78], byte[56]=1
        byte[] cmd = new byte[64];
        cmd[0] = 0x12; cmd[1] = 0x34; cmd[2] = 0x56; cmd[3] = 0x78;
        cmd[56] = 0x01; // SSCRM_CMD_TYPE_DEV_INFO

        byte[] response = new byte[1024];

        // 原版：100ms 超时，先提交写再提交读
        if (_writer!.SubmitAsyncTransfer(cmd, 0, cmd.Length, 100, out var writeCtx) != 0)
        {
            ErrorOccurred?.Invoke(this, "SCSI 握手写入失败");
            return false;
        }
        if (_reader!.SubmitAsyncTransfer(response, 0, response.Length, 100, out var readCtx) != 0)
        {
            ErrorOccurred?.Invoke(this, "SCSI 握手读取失败");
            return false;
        }

        // 等待完成
        Thread.Sleep(200);
        try
        {
            if (!writeCtx.IsCompleted) writeCtx.Cancel();
            writeCtx.Wait(out _);
            writeCtx.Dispose();

            if (!readCtx.IsCompleted) readCtx.Cancel();
            readCtx.Wait(out _);
            readCtx.Dispose();
        }
        catch { }

        // 解析响应：byte[24] 必须非零
        if (response[24] == 0)
        {
            ErrorOccurred?.Invoke(this, "SCSI 握手响应无效 (byte[24]==0)");
            return false;
        }

        // 解析分辨率
        // 原版通过 response 中的字节推断分辨率
        // byte[32] -> width factor, byte[40] -> height factor
        ParseScsiResolution(response);

        return true;
    }

    private void ParseScsiResolution(byte[] response)
    {
        if (ActiveDeviceInfo == null) return;

        // 原版 TRCC 对 SCSI 设备固定以 fbl=72 初始化（480x480，691200 = 480*480*3）
        ApplyFblResolution(ActiveDeviceInfo, 72);
    }

    /// <summary>
    /// HID 握手 - 发送 [0xDA, 0xDB, 0xDC, 0xDD...] 512字节，读取 512 字节响应
    /// </summary>
    private bool HidHandshake()
    {
        var cmd = new byte[512];
        cmd[0] = 0xDA; cmd[1] = 0xDB; cmd[2] = 0xDC; cmd[3] = 0xDD;
        cmd[12] = 0x01;

        _writer!.SubmitAsyncTransfer(cmd, 0, cmd.Length, 2000, out var writeCtx);

        var response = new byte[512];
        _reader!.SubmitAsyncTransfer(response, 0, response.Length, 2000, out var readCtx);

        Thread.Sleep(200);
        try
        {
            if (!writeCtx.IsCompleted) writeCtx.Cancel();
            writeCtx.Wait(out _); writeCtx.Dispose();
            if (!readCtx.IsCompleted) readCtx.Cancel();
            readCtx.Wait(out _); readCtx.Dispose();
        }
        catch { }

        // 验证：bytes[0-3] == [0xDA,0xDB,0xDC,0xDD], byte[12]==1, byte[16]==16
        if (response[0] == 0xDA && response[1] == 0xDB &&
            response[2] == 0xDC && response[3] == 0xDD &&
            response[12] == 1 && response[16] == 16)
        {
            if (ActiveDeviceInfo != null)
            {
                // 原版 TRCC：HID 设备分辨率由响应 byte[5] 的 fbl 码决定
                // （USBLCDNEW 写入 shareMemoryValRGB[1]=array[5]，TRCC 以它为 fbl 传入 FormCZTVInit）
                ApplyFblResolution(ActiveDeviceInfo, response[5]);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// ALi 握手 - 发送 [0xF5, 0x00, 0x01, 0x00...] + 1024字节，读取 1024 字节响应
    /// </summary>
    private bool AliHandshake()
    {
        var header = new byte[16]
        {
            0xF5, 0x00, 0x01, 0x00, 0xBC, 0xFF, 0xB6, 0xC8,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00
        };
        var cmd = header.Concat(new byte[1024]).ToArray();

        _writer!.SubmitAsyncTransfer(cmd, 0, cmd.Length, 2000, out var writeCtx);

        var response = new byte[1024];
        _reader!.SubmitAsyncTransfer(response, 0, response.Length, 2000, out var readCtx);

        Thread.Sleep(200);
        try
        {
            if (!writeCtx.IsCompleted) writeCtx.Cancel();
            writeCtx.Wait(out _); writeCtx.Dispose();
            if (!readCtx.IsCompleted) readCtx.Cancel();
            readCtx.Wait(out _); readCtx.Dispose();
        }
        catch { }

        // 验证：byte[0] == 54 (0x36) or 101 or 102
        if (response[0] == 54 || response[0] == 101 || response[0] == 102)
        {
            if (ActiveDeviceInfo != null)
            {
                // 原版 USBLCDNEW ALi 路径：num2=204800（320*320*2 RGB565）为默认，
                // array[0]==54 时 num2=153600（320*240*2），即 320x240；
                // TRCC 侧 fbl=array[0]-1，101/102 → fbl 100/101 → is320x320
                ActiveDeviceInfo.Width = 320;
                ActiveDeviceInfo.Height = response[0] == 54 ? 240 : 320;
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// 按 fbl 码解析屏幕分辨率（映射关系取自 TRCC 反编译 FormCZTVInit）：
    /// 72/129 → 480x480；64 → 640x480；54 → 360x360；
    /// 100/101/102 → 320x320；36/37 → 240x240；128 → 1280x480；
    /// 59 → 640x172；60 → 176x320；其它默认 480x480。
    /// </summary>
    private static void ApplyFblResolution(LcdDeviceInfo info, int fbl)
    {
        info.Fbl = (byte)fbl;
        (info.Width, info.Height) = fbl switch
        {
            72 or 129 => (480, 480),
            64 => (640, 480),
            54 => (360, 360),
            100 or 101 or 102 => (320, 320),
            36 or 37 => (240, 240),
            128 => (1280, 480),
            59 => (640, 172),
            60 => (176, 320),
            _ => (480, 480),
        };
    }

    /// <summary>
    /// LY 握手 - 发送 [0x02, 0xFF...] 2048字节，读取 512 字节响应
    /// </summary>
    private bool LyHandshake(bool isLy1)
    {
        var header = new byte[16]
        {
            0x02, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        var cmd = header.Concat(new byte[2032]).ToArray();

        _writer!.SubmitAsyncTransfer(cmd, 0, cmd.Length, 2000, out var writeCtx);

        var response = new byte[512];
        _reader!.SubmitAsyncTransfer(response, 0, response.Length, 2000, out var readCtx);

        Thread.Sleep(200);
        try
        {
            if (!writeCtx.IsCompleted) writeCtx.Cancel();
            writeCtx.Wait(out _); writeCtx.Dispose();
            if (!readCtx.IsCompleted) readCtx.Cancel();
            readCtx.Wait(out _); readCtx.Dispose();
        }
        catch { }

        // 验证：byte[0]==3, byte[1]==0xFF, byte[8]==1
        if (response[0] == 3 && response[1] == 0xFF && response[8] == 1)
        {
            if (ActiveDeviceInfo != null)
            {
                ActiveDeviceInfo.Width = 480;
                ActiveDeviceInfo.Height = 480;
            }
            return true;
        }
        return false;
    }

    #endregion

    #region 帧发送

    /// <summary>
    /// 发送 RGB888 帧（匹配原版协议）
    /// 输入：RGB888 数据 (width * height * 3 bytes)
    /// </summary>
    public bool SendFrameRgb888(byte[] rgb888Data)
    {
        if (!_isRunning || _activeDevice == null || ActiveDeviceInfo == null)
            return false;

        lock (_sendLock)
        {
            if (_frameQueue.Count >= 2) return false;
            _frameQueue.Enqueue(rgb888Data);
            LastFrame = rgb888Data;
        }
        _frameEvent.Set();
        return true;
    }

    /// <summary>
    /// 兼容方法：将 RGB565 转为 RGB888 再发送
    /// </summary>
    public bool SendPreRenderedFrame(PreRenderedFrame frame)
    {
        if (ActiveDeviceInfo == null) return false;
        var rgb888 = Rgb565ToRgb888(frame.Data);
        return SendFrameRgb888(rgb888);
    }

    /// <summary>
    /// 清空待发送队列（用于清除屏幕时立即停止残留的视频帧）
    /// </summary>
    public void ClearFrameQueue()
    {
        lock (_sendLock) _frameQueue.Clear();
    }

    private void SendThreadProc()
    {
        var sw = new System.Diagnostics.Stopwatch();
        while (_isRunning)
        {
            byte[]? frameData = null;
            lock (_sendLock)
            {
                if (_frameQueue.Count > 0)
                    frameData = _frameQueue.Dequeue();
            }

            if (frameData != null)
            {
                try
                {
                    // 每个实际发送的帧都镜像到预览，保证实时预览跟随屏幕
                    LastFrame = frameData;
                    sw.Restart();
                    SendFrameInternal(frameData);
                    _frameCount++;
                    FrameSent?.Invoke(this, _frameCount);
                }
                catch (Exception ex)
                {
                    ErrorOccurred?.Invoke(this, $"发送失败: {ex.Message}");
                }
                // 按目标帧率限速：扣除实际发送耗时后休眠剩余时间，修改设置后即时生效
                double intervalMs = 1000.0 / Math.Max(1, _targetFps);
                int sleepMs = (int)(intervalMs - sw.Elapsed.TotalMilliseconds);
                if (sleepMs > 0) Thread.Sleep(sleepMs);
            }
            else
            {
                _frameEvent.WaitOne(100);
            }
        }
    }

    private void SendFrameInternal(byte[] rgb888Data)
    {
        if (_writer == null || ActiveDeviceInfo == null) return;

        switch (ActiveDeviceInfo.Type)
        {
            case LcdDeviceType.Scsi:
                SendScsiFrame(rgb888Data);
                break;
            case LcdDeviceType.Hid:
                SendHidFrame(rgb888Data);
                break;
            case LcdDeviceType.Ali:
                SendAliFrame(rgb888Data);
                break;
            case LcdDeviceType.Ly:
            case LcdDeviceType.Ly1:
                SendLyFrame(rgb888Data);
                break;
        }
    }

    /// <summary>
    /// SCSI 帧发送（精确匹配原版 TRCC 协议）
    ///
    /// 原版协议：
    /// 1. 图片先 JPEG 压缩（质量 95）
    /// 2. 64 字节帧头 + JPEG 数据
    /// 3. 帧头格式：
    ///    byte[0-3] = 0x12, 0x34, 0x56, 0x78 (magic)
    ///    byte[4] = 2 (SSCRM_CMD_TYPE_PICTURE)
    ///    byte[8-9] = 宽度（小端）
    ///    byte[12-13] = 高度（小端）
    ///    byte[56] = 2 (SSCRM_CMD_TYPE_PICTURE)
    ///    byte[60-63] = JPEG 数据长度（小端）
    /// 4. 总长度 % 512 == 0 时发零长度包
    /// 5. 超时 100ms，sleep 15ms
    /// </summary>
    private void SendScsiFrame(byte[] rgb888Data)
    {
        if (_writer == null || ActiveDeviceInfo == null) return;

        int w = ActiveDeviceInfo.Width;
        int h = ActiveDeviceInfo.Height;

        // 1. RGB888 → Bitmap → JPEG 压缩
        // 注意：Format24bppRgb 的内存布局是 BGR，直接拷贝 RGB888 会导致红蓝互换。
        // 需要先把 R/G/B 交换为 B/G/R 再写入，JPEG 编码结果才与屏幕颜色一致。
        byte[] jpegData;
        using (var bmp = new Bitmap(w, h, DrawPixelFormat.Format24bppRgb))
        {
            var rect = new Rectangle(0, 0, w, h);
            var bmpData = bmp.LockBits(rect, ImageLockMode.WriteOnly, DrawPixelFormat.Format24bppRgb);

            byte[] bgr = new byte[rgb888Data.Length];
            for (int i = 0; i < rgb888Data.Length; i += 3)
            {
                bgr[i] = rgb888Data[i + 2];      // B
                bgr[i + 1] = rgb888Data[i + 1];  // G
                bgr[i + 2] = rgb888Data[i];      // R
            }
            Marshal.Copy(bgr, 0, bmpData.Scan0, bgr.Length);
            bmp.UnlockBits(bmpData);

            // JPEG 压缩，质量 95
            var jpegEncoder = ImageCodecInfo
                .GetImageEncoders()
                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 95L);

            using var ms = new MemoryStream();
            bmp.Save(ms, jpegEncoder, encoderParams);
            encoderParams.Dispose();
            jpegData = ms.ToArray();
        }

        int dataLen = jpegData.Length;
        int totalLen = dataLen + 64;

        // 2. 构建 64 字节帧头（精确匹配原版 is480x480 + myDevicePingMu==5）
        byte[] frame = new byte[totalLen];
        frame[0] = 0x12; frame[1] = 0x34; frame[2] = 0x56; frame[3] = 0x78; // magic
        frame[4] = 0x02;  // SSCRM_CMD_TYPE_PICTURE
        // byte[8-9] = 宽度小端
        frame[8] = (byte)(w & 0xFF);
        frame[9] = (byte)((w >> 8) & 0xFF);
        // byte[12-13] = 高度小端
        frame[12] = (byte)(h & 0xFF);
        frame[13] = (byte)((h >> 8) & 0xFF);
        // byte[56] = 2 (SSCRM_CMD_TYPE_PICTURE)
        frame[56] = 0x02;
        // byte[60-63] = JPEG 数据长度（小端）
        frame[60] = (byte)(dataLen & 0xFF);
        frame[61] = (byte)((dataLen >> 8) & 0xFF);
        frame[62] = (byte)((dataLen >> 16) & 0xFF);
        frame[63] = (byte)((dataLen >> 24) & 0xFF);

        // 3. 复制 JPEG 数据
        Buffer.BlockCopy(jpegData, 0, frame, 64, dataLen);

        // 4. 发送
        if (_writer.SubmitAsyncTransfer(frame, 0, totalLen, 100, out var ctx) != 0)
        {
            ErrorOccurred?.Invoke(this, "SCSI 帧写入提交失败");
            return;
        }

        // 5. 零长度包
        if (totalLen % 512 == 0)
        {
            _writer.SubmitAsyncTransfer(frame, 0, 0, 100, out _);
        }

        // 6. sleep 15ms，然后 cancel+wait+dispose
        Thread.Sleep(15);
        try
        {
            if (!ctx.IsCompleted) ctx.Cancel();
            ctx.Wait(out _);
            ctx.Dispose();
        }
        catch { }
    }

    /// <summary>
    /// HID 帧发送（原版 ThreadSendDeviceDataH 逻辑）
    /// 20 字节头 + RGB888 数据，对齐到 512 字节
    /// </summary>
    private void SendHidFrame(byte[] rgb888Data)
    {
        if (_writer == null || ActiveDeviceInfo == null) return;

        int dataLen = rgb888Data.Length;
        int totalLen = dataLen + 20;
        // 对齐到 512
        totalLen = (totalLen / 512 + (totalLen % 512 != 0 ? 1 : 0)) * 512;

        byte[] frame = new byte[totalLen];
        // 20 字节头，bytes 16-19: 数据长度（大端）
        frame[16] = (byte)((dataLen >> 24) & 0xFF);
        frame[17] = (byte)((dataLen >> 16) & 0xFF);
        frame[18] = (byte)((dataLen >> 8) & 0xFF);
        frame[19] = (byte)(dataLen & 0xFF);
        Buffer.BlockCopy(rgb888Data, 0, frame, 20, dataLen);

        _writer.Transfer(frame, 0, frame.Length, 3000, out _);
    }

    /// <summary>
    /// ALi 帧发送（原版 ThreadSendDeviceDataALi 逻辑）
    /// 16 字节头 + RGB565 数据（ALi 用 RGB565！）
    /// </summary>
    private void SendAliFrame(byte[] rgb888Data)
    {
        if (_writer == null || ActiveDeviceInfo == null) return;

        // ALi 用 RGB565，需要转换
        byte[] rgb565 = Rgb888ToRgb565(rgb888Data, ActiveDeviceInfo.Width, ActiveDeviceInfo.Height);
        int dataLen = rgb565.Length;

        // 16 字节头
        var header = new byte[16]
        {
            0xF5, 0x01, 0x01, 0x00, 0xBC, 0xFF, 0xB6, 0xC8,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };
        header[12] = (byte)(dataLen & 0xFF);
        header[13] = (byte)((dataLen >> 8) & 0xFF);
        header[14] = (byte)((dataLen >> 16) & 0xFF);
        header[15] = (byte)((dataLen >> 24) & 0xFF);

        byte[] frame = header.Concat(rgb565).ToArray();

        _writer.Write(frame, 3000, out _);
        // 读取 16 字节响应
        _reader?.Read(new byte[16], 0, 16, 100, out _);
    }

    /// <summary>
    /// LY 帧发送（原版 ThreadSendDeviceDataLY 逻辑）
    /// 512 字节分块，16 字节头 + 496 字节数据
    /// </summary>
    private void SendLyFrame(byte[] rgb888Data)
    {
        if (_writer == null || ActiveDeviceInfo == null) return;

        int dataLen = rgb888Data.Length;
        int chunkPayloadSize = 496;
        int totalChunks = dataLen / chunkPayloadSize + 1;
        int lastChunkSize = dataLen % chunkPayloadSize;

        // 对齐到 4 的倍数
        int paddedChunks = totalChunks;
        int remainder = totalChunks % 4;
        if (remainder != 0) paddedChunks = totalChunks + (4 - remainder);

        byte[] allData = new byte[paddedChunks * 512];

        for (int i = 0; i < totalChunks; i++)
        {
            int offset = i * 512;
            int payloadSize = (i == totalChunks - 1) ? lastChunkSize : chunkPayloadSize;

            // 16 字节头
            allData[offset + 0] = 0x01;
            allData[offset + 1] = 0xFF;
            allData[offset + 2] = (byte)(dataLen & 0xFF);
            allData[offset + 3] = (byte)((dataLen >> 8) & 0xFF);
            allData[offset + 4] = (byte)((dataLen >> 16) & 0xFF);
            allData[offset + 5] = (byte)((dataLen >> 24) & 0xFF);
            allData[offset + 6] = (byte)(payloadSize & 0xFF);
            allData[offset + 7] = (byte)((payloadSize >> 8) & 0xFF);
            allData[offset + 8] = 0x01;
            allData[offset + 9] = (byte)(totalChunks & 0xFF);
            allData[offset + 10] = (byte)((totalChunks >> 8) & 0xFF);
            allData[offset + 11] = (byte)(i & 0xFF);
            allData[offset + 12] = (byte)((i >> 8) & 0xFF);

            Buffer.BlockCopy(rgb888Data, i * chunkPayloadSize, allData, offset + 16, payloadSize);
        }

        // 分 4096 字节写入
        int written = 0;
        while (written < allData.Length)
        {
            int writeSize = Math.Min(4096, allData.Length - written);
            _writer.Write(allData, written, writeSize, 100, out _);
            written += 4096;
        }

        // 读取 512 字节响应
        _reader?.Read(new byte[512], 0, 512, 100, out _);
    }

    #endregion

    #region 颜色转换

    /// <summary>
    /// RGB565 转 RGB888
    /// </summary>
    public static byte[] Rgb565ToRgb888(byte[] rgb565)
    {
        int pixels = rgb565.Length / 2;
        byte[] rgb888 = new byte[pixels * 3];
        for (int i = 0, j = 0; i < rgb565.Length; i += 2, j += 3)
        {
            // Big-endian RGB565
            ushort val = (ushort)((rgb565[i] << 8) | rgb565[i + 1]);
            byte r = (byte)((val >> 11) & 0x1F);
            byte g = (byte)((val >> 5) & 0x3F);
            byte b = (byte)(val & 0x1F);
            // 扩展到 8 位
            rgb888[j] = (byte)(r << 3 | r >> 2);
            rgb888[j + 1] = (byte)(g << 2 | g >> 4);
            rgb888[j + 2] = (byte)(b << 3 | b >> 2);
        }
        return rgb888;
    }

    /// <summary>
    /// RGB888 转 RGB565
    /// </summary>
    public static byte[] Rgb888ToRgb565(byte[] rgb888, int width, int height)
    {
        int pixels = width * height;
        byte[] rgb565 = new byte[pixels * 2];
        for (int i = 0, j = 0; i < pixels * 3 && j < rgb565.Length; i += 3, j += 2)
        {
            byte r = rgb888[i];
            byte g = rgb888[i + 1];
            byte b = rgb888[i + 2];
            ushort val = (ushort)(((r >> 3) << 11) | ((g >> 2) << 5) | (b >> 3));
            rgb565[j] = (byte)(val >> 8);
            rgb565[j + 1] = (byte)(val & 0xFF);
        }
        return rgb565;
    }

    /// <summary>
    /// ARGB32 转 RGB888
    /// 输入为 System.Drawing Format32bppArgb 的内存布局 [B, G, R, A]（与 FFmpeg RGB24 转 BGRA 后一致）
    /// </summary>
    public static byte[] ArgbToRgb888(byte[] argb, int width, int height,
        int brightness = 100, int contrast = 100, int saturation = 100)
    {
        int pixels = width * height;
        byte[] rgb888 = new byte[pixels * 3];

        float bFactor = brightness / 100f;
        float cFactor = contrast / 100f;
        float sFactor = saturation / 100f;

        for (int i = 0, j = 0; i < pixels * 4; i += 4, j += 3)
        {
            byte b = argb[i];      // B
            byte g = argb[i + 1];  // G
            byte r = argb[i + 2];  // R

            // 亮度
            r = (byte)Math.Clamp(r * bFactor, 0, 255);
            g = (byte)Math.Clamp(g * bFactor, 0, 255);
            b = (byte)Math.Clamp(b * bFactor, 0, 255);

            // 对比度
            r = (byte)Math.Clamp((r - 128) * cFactor + 128, 0, 255);
            g = (byte)Math.Clamp((g - 128) * cFactor + 128, 0, 255);
            b = (byte)Math.Clamp((b - 128) * cFactor + 128, 0, 255);

            // 饱和度
            float gray = 0.299f * r + 0.587f * g + 0.114f * b;
            r = (byte)Math.Clamp(gray + (r - gray) * sFactor, 0, 255);
            g = (byte)Math.Clamp(gray + (g - gray) * sFactor, 0, 255);
            b = (byte)Math.Clamp(gray + (b - gray) * sFactor, 0, 255);

            rgb888[j] = r;
            rgb888[j + 1] = g;
            rgb888[j + 2] = b;
        }
        return rgb888;
    }

    /// <summary>
    /// RGB24（FFmpeg rawvideo 输出，字节序 R,G,B）转 RGB888，并应用亮度/对比度/饱和度
    /// 相比 ArgbToRgb888 省去 4 字节中间缓冲，降低视频预渲染内存占用
    /// </summary>
    public static byte[] Rgb24ToRgb888(byte[] rgb24, int width, int height,
        int brightness = 100, int contrast = 100, int saturation = 100)
    {
        int pixels = width * height;
        byte[] rgb888 = new byte[pixels * 3];

        float bFactor = brightness / 100f;
        float cFactor = contrast / 100f;
        float sFactor = saturation / 100f;

        for (int i = 0; i < pixels * 3; i += 3)
        {
            byte r = rgb24[i];
            byte g = rgb24[i + 1];
            byte b = rgb24[i + 2];

            // 亮度
            r = (byte)Math.Clamp(r * bFactor, 0, 255);
            g = (byte)Math.Clamp(g * bFactor, 0, 255);
            b = (byte)Math.Clamp(b * bFactor, 0, 255);

            // 对比度
            r = (byte)Math.Clamp((r - 128) * cFactor + 128, 0, 255);
            g = (byte)Math.Clamp((g - 128) * cFactor + 128, 0, 255);
            b = (byte)Math.Clamp((b - 128) * cFactor + 128, 0, 255);

            // 饱和度
            float gray = 0.299f * r + 0.587f * g + 0.114f * b;
            r = (byte)Math.Clamp(gray + (r - gray) * sFactor, 0, 255);
            g = (byte)Math.Clamp(gray + (g - gray) * sFactor, 0, 255);
            b = (byte)Math.Clamp(gray + (b - gray) * sFactor, 0, 255);

            rgb888[i] = r;
            rgb888[i + 1] = g;
            rgb888[i + 2] = b;
        }
        return rgb888;
    }

    // 兼容旧接口
    public static byte[] RgbaToRgb565(byte[] rgba, int width, int height,
        int brightness = 100, int contrast = 100, int saturation = 100)
    {
        var rgb888 = ArgbToRgb888(rgba, width, height, brightness, contrast, saturation);
        return Rgb888ToRgb565(rgb888, width, height);
    }

    public static byte[] RotateRgb565(byte[] data, int width, int height, int rotation) => data;

    #endregion

    #region IDisposable

    public void Dispose()
    {
        Disconnect();
    }

    #endregion
}
