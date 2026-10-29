// ==================== 导航 ====================
const navItems = document.querySelectorAll('.nav-item');
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

// ==================== C# 通信桥 ====================
function postToCSharp(action, data = {}) {
    const msg = JSON.stringify({ action, data });
    if (window.chrome && window.chrome.webview) {
        window.chrome.webview.postMessage(msg);
    } else {
        console.log('[C# Bridge]', action, data);
    }
}

window.chrome.webview.addEventListener('message', e => {
    let msg;
    try { msg = JSON.parse(e.data); } catch { return; }
    handleMessage(msg);
});

function handleMessage(msg) {
    switch (msg.action) {
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
            showToast(msg.data.message, msg.data.type || 'info');
            break;
        case 'playbackStatus':
            updatePlaybackStatus(msg.data);
            break;
        case 'cacheInfo':
            updateCacheInfo(msg.data);
            break;
    }
}

// ==================== 设备状态 ====================
function updateDeviceStatus(data) {
    const dot = document.getElementById('statusDot');
    const text = document.getElementById('statusText');
    const device = document.getElementById('statusDevice');
    const statDevices = document.getElementById('statDevices');
    const statDeviceName = document.getElementById('statDeviceName');
    const screenRes = document.getElementById('screenRes');

    if (data.connected) {
        dot.classList.add('connected');
        text.textContent = '已连接';
        device.textContent = data.deviceName || '未知设备';
        statDevices.textContent = '1';
        statDeviceName.textContent = data.deviceName || '已连接';
        screenRes.textContent = data.resolution || '--×--';
    } else {
        dot.classList.remove('connected');
        text.textContent = '未连接';
        device.textContent = '未检测到设备';
        statDevices.textContent = '0';
        statDeviceName.textContent = '未连接';
        screenRes.textContent = '--×--';
    }
}

function updateScanResult(data) {
    document.getElementById('statDevices').textContent = data.count || 0;
    if (data.count > 0) {
        showToast(`扫描到 ${data.count} 个设备`, 'success');
    } else {
        showToast('未找到设备，请检查USB连接和驱动', 'warning');
    }
}

// ==================== 仪表盘操作 ====================
function scanDevices() {
    showToast('正在扫描设备...');
    postToCSharp('scanDevices');
}

function sendTestPattern(pattern) {
    postToCSharp('sendTestPattern', { pattern });
    showToast(`发送测试图案: ${pattern}`);
}

function clearDisplay() {
    postToCSharp('clearDisplay');
    showToast('已清除屏幕');
}

function updatePlaybackStatus(data) {
    const el = document.getElementById('statPlayback');
    const fps = document.getElementById('statFps');
    el.textContent = data.playing ? '播放中' : '空闲';
    fps.textContent = (data.fps || '--') + ' FPS';
}

// ==================== 媒体库 ====================
let currentFilter = 'all';
let mediaItems = [];

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
    renderMediaList({ items: filtered });
}

function renderMediaList(data) {
    if (data.items) mediaItems = data.items;
    const grid = document.getElementById('mediaGrid');
    const items = data.items || mediaItems;

    if (!items || items.length === 0) {
        grid.innerHTML = `
            <div class="empty-state">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                <h3>暂无媒体文件</h3>
                <p>上传一些图片或视频开始使用吧</p>
                <button class="btn btn-primary" onclick="uploadMedia('image')">上传文件</button>
            </div>`;
        return;
    }

    grid.innerHTML = items.map(m => `
        <div class="media-item" onclick="playMedia('${m.id}')">
            <div class="media-thumb">
                ${m.thumbnail ? `<img src="${m.thumbnail}" alt="${m.name}">` : ''}
            </div>
            <div class="media-type">${m.type === 'video' ? '🎬 视频' : m.type === 'gif' ? '🎞️ GIF' : '🖼️ 图片'}</div>
            <div class="media-info">
                <div class="media-name">${m.name}</div>
                <div class="media-res">${m.resolution || ''}</div>
            </div>
        </div>
    `).join('');
}

function uploadMedia(type) {
    postToCSharp('uploadMedia', { type });
}

function playMedia(id) {
    postToCSharp('playMedia', { id });
    showToast('开始播放...');
}

// ==================== 设置 ====================
function updateSlider(id, value, suffix) {
    document.getElementById('val' + id.charAt(0).toUpperCase() + id.slice(1)).textContent = value + suffix;
}

function setOption(key, value, btn) {
    btn.parentElement.querySelectorAll('.option-tab').forEach(t => t.classList.remove('active'));
    btn.classList.add('active');
}

function loadSettings(data) {
    if (data.brightness) document.getElementById('brightness').value = data.brightness;
    if (data.contrast) document.getElementById('contrast').value = data.contrast;
    if (data.saturation) document.getElementById('saturation').value = data.saturation;
    if (data.targetFps) document.getElementById('targetFps').value = data.targetFps;
    if (data.videoFps) document.getElementById('videoFps').value = data.videoFps;
    if (data.loopVideo !== undefined) document.getElementById('loopVideo').checked = data.loopVideo;
    if (data.preRenderVideo !== undefined) document.getElementById('preRenderVideo').checked = data.preRenderVideo;
    if (data.startMinimized !== undefined) document.getElementById('startMinimized').checked = data.startMinimized;
    if (data.highPriority !== undefined) document.getElementById('highPriority').checked = data.highPriority;
    if (data.minimizeToTray !== undefined) document.getElementById('minimizeToTray').checked = data.minimizeToTray;
    if (data.enableMica !== undefined) document.getElementById('enableMica').checked = data.enableMica;

    // 更新显示值
    updateSlider('brightness', data.brightness || 100, '%');
    updateSlider('contrast', data.contrast || 100, '%');
    updateSlider('saturation', data.saturation || 100, '%');
    updateSlider('targetFps', data.targetFps || 30, ' FPS');
    updateSlider('videoFps', data.videoFps || 30, ' FPS');
}

function applySettings() {
    const settings = {
        brightness: +document.getElementById('brightness').value,
        contrast: +document.getElementById('contrast').value,
        saturation: +document.getElementById('saturation').value,
        targetFps: +document.getElementById('targetFps').value,
        videoFps: +document.getElementById('videoFps').value,
        loopVideo: document.getElementById('loopVideo').checked,
        preRenderVideo: document.getElementById('preRenderVideo').checked,
    };
    postToCSharp('applySettings', settings);
    showToast('设置已应用', 'success');
}

function resetSettings() {
    loadSettings({
        brightness: 100, contrast: 100, saturation: 100,
        targetFps: 30, videoFps: 30, loopVideo: true, preRenderVideo: true
    });
    showToast('设置已重置');
}

function clearCache() {
    postToCSharp('clearCache');
    showToast('缓存已清除', 'success');
}

function updateCacheInfo(data) {
    document.getElementById('cacheSize').textContent = data.size || '0 B';
    document.getElementById('cacheImages').textContent = data.images || 0;
    document.getElementById('cacheVideos').textContent = data.videos || 0;
}

// ==================== 自启动 ====================
let autoStartEnabled = false;

function updateAutoStartStatus(data) {
    autoStartEnabled = data.enabled;
    const dot = document.querySelector('.autostart-dot');
    const text = document.getElementById('autoStartText');
    const btn = document.getElementById('autoStartBtn');

    if (data.enabled) {
        dot.classList.add('active');
        text.textContent = '自启动已启用';
        btn.textContent = '禁用';
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-ghost');
    } else {
        dot.classList.remove('active');
        text.textContent = '自启动未启用';
        btn.textContent = '启用';
        btn.classList.add('btn-primary');
        btn.classList.remove('btn-ghost');
    }
}

function toggleAutoStart() {
    if (autoStartEnabled) {
        postToCSharp('disableAutoStart');
    } else {
        const startMinimized = document.getElementById('startMinimized').checked;
        const highPriority = document.getElementById('highPriority').checked;
        postToCSharp('enableAutoStart', { startMinimized, highPriority });
    }
}

function saveSystemSettings() {
    const settings = {
        startMinimized: document.getElementById('startMinimized').checked,
        highPriority: document.getElementById('highPriority').checked,
        minimizeToTray: document.getElementById('minimizeToTray').checked,
        enableMica: document.getElementById('enableMica').checked,
        language: document.getElementById('language').value,
    };
    postToCSharp('saveSystemSettings', settings);
    showToast('设置已保存', 'success');
}

// ==================== Toast ====================
function showToast(message, type = 'info') {
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = 'toast ' + type;
    toast.textContent = message;
    container.appendChild(toast);
    setTimeout(() => {
        toast.style.opacity = '0';
        toast.style.transform = 'translateX(120%)';
        toast.style.transition = '0.3s';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

// ==================== 初始化 ====================
document.addEventListener('DOMContentLoaded', () => {
    postToCSharp('init');
    postToCSharp('getSettings');
    postToCSharp('getAutoStartStatus');
    postToCSharp('getMediaList');
});
