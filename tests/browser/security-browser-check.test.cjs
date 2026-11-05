const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');
const assert = require('node:assert/strict');
const { test } = require('node:test');
test('Chrome verifies CSP, live stock and AI controls without business writes', { timeout: process.env.BIOGAMA_TEST_LIVE_AI === '1' ? 180000 : 45000 }, async () => {
const delay = ms => new Promise(resolve => setTimeout(resolve, ms));
const profile = path.resolve('.local/browser-security/' + crypto.randomUUID());
fs.mkdirSync(profile, { recursive: true });
const browser = spawn('C:/Program Files/Google/Chrome/Application/chrome.exe', [
    '--headless=new', '--remote-debugging-port=0', '--user-data-dir=' + profile,
    '--no-first-run', '--no-default-browser-check', '--disable-background-networking', 'about:blank'
], { windowsHide: true, stdio: 'ignore' });
let socket;
const results = [];
try {
    const portFile = path.join(profile, 'DevToolsActivePort');
    for (let i = 0; i < 100 && !fs.existsSync(portFile); i++) await delay(100);
    if (!fs.existsSync(portFile)) throw new Error('Chrome did not expose its isolated debugging endpoint');
    const port = fs.readFileSync(portFile, 'utf8').split('\n')[0];
    const page = await (await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: 'PUT' })).json();
    socket = new WebSocket(page.webSocketDebuggerUrl);
    await new Promise((resolve, reject) => { socket.onopen = resolve; socket.onerror = reject; });
    let sequence = 0;
    const pending = new Map();
    socket.onmessage = event => {
        const message = JSON.parse(event.data);
        if (!message.id) return;
        const request = pending.get(message.id);
        if (!request) return;
        pending.delete(message.id);
        if (message.error) request.reject(new Error(message.error.message)); else request.resolve(message.result);
    };
    const send = (method, params = {}) => new Promise((resolve, reject) => {
        const id = ++sequence;
        pending.set(id, { resolve, reject });
        socket.send(JSON.stringify({ id, method, params }));
    });
    const evaluate = async expression => {
        const response = await send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
        if (response.exceptionDetails) throw new Error(response.exceptionDetails.text + ': ' + response.exceptionDetails.exception?.description);
        return response.result.value;
    };
    await send('Page.enable');
    await send('Runtime.enable');
    if (process.env.BIOGAMA_TEST_LIVE_AI === '1')
        await send('Emulation.setDeviceMetricsOverride', { width: 1440, height: 1000, deviceScaleFactor: 1, mobile: false });
    await send('Page.addScriptToEvaluateOnNewDocument', { source: `window.__policyViolations=[];document.addEventListener('securitypolicyviolation',e=>window.__policyViolations.push({directive:e.effectiveDirective,blocked:e.blockedURI}));` });
    const routes = ['/', '/Products', '/Ia', '/Courses', '/Identity/Account/Login', '/Identity/Account/Register', '/Identity/Account/ForgotPassword'];
    for (const route of routes) {
        await send('Page.navigate', { url: (process.env.BIOGAMA_BASE_URL || 'https://localhost:30080') + route });
        for (let i = 0; i < 100; i++) {
            const ready = await evaluate(route === '/Products'
                ? `document.readyState==='complete' && document.querySelector('#stockLiveStatus')?.textContent.includes('Disponibilidad en vivo.')`
                : route === '/Ia'
                    ? `document.readyState==='complete' && typeof window.generarAnalisis==='function'`
                    : `document.readyState==='complete' && location.pathname===${JSON.stringify(route)}`);
            if (ready) break;
            if (i === 99) throw new Error('Page scripts did not initialize: ' + route);
            await delay(100);
        }
        const result = await evaluate(`({path:location.pathname,violations:window.__policyViolations,stock:document.querySelector('#stockLiveStatus')?.textContent,inlineHandlers:document.querySelectorAll('[onclick],[onsubmit]').length})`);
        assert.equal(result.path, route);
        assert.equal(result.inlineHandlers, 0);
        assert.equal(result.violations.length, 0, 'Unexpected resource blocked by CSP: ' + JSON.stringify(result));
        if (route === '/Ia') {
            const action = await evaluate(`(()=>{let called='';const original=window.generarAnalisis;window.generarAnalisis=type=>{called=type};document.querySelector('[data-analysis-type="ficha"]').click();window.generarAnalisis=original;return called})()`);
            assert.equal(action, 'ficha');
            result.aiButton = 'PASS; handler substituted to avoid calling AI service';
            result.selection = await evaluate(`(()=>{const species={id:987654321,name:'Synthetic <img src=x onerror=alert(1)>',scientific:'Synthetic species',code:'<script>alert(1)</script>'};speciesList.push(species);const input=document.querySelector('#speciesInput');input.value=species.name;input.dispatchEvent(new Event('input',{bubbles:true}));const dropdown=document.querySelector('#speciesDropdown');if(dropdown.querySelector('img,script'))throw new Error('Unescaped species text');document.querySelector('[data-species-id="'+species.id+'"]').click();if(document.querySelector('#speciesId').value!==String(species.id))throw new Error('Selection failed');return 'PASS; browser-only synthetic species, no database write'})()`);
            if (process.env.BIOGAMA_TEST_LIVE_AI === '1') {
                const prompt = 'Responde en una frase en español: ¿por qué debemos cuidar los bosques del Ecuador?';
                const started = Date.now();
                await evaluate(`document.getElementById('speciesInput').value='';document.getElementById('speciesId').value='';document.getElementById('custom-tab').click();document.getElementById('customPrompt').value=${JSON.stringify(prompt)};document.querySelector('[data-analysis-type="libre"]').click();true`);
                let completed = false;
                for (let i = 0; i < 1500; i++) {
                    const status = await evaluate(`({success:!document.getElementById('stateSuccess').classList.contains('d-none'),error:!document.getElementById('stateError').classList.contains('d-none'),message:document.getElementById('errorMessage').textContent})`);
                    if (status.error) throw new Error('Live AI: ' + status.message);
                    if (status.success) { completed = true; break; }
                    await delay(100);
                }
                assert.ok(completed, 'Live AI did not finish within the test deadline');
                result.liveGeneration = { prompt, elapsedSeconds: (Date.now() - started) / 1000,
                    response: await evaluate(`document.getElementById('aiMarkdownContent').textContent`) };
                assert.ok(result.liveGeneration.response.trim().length > 0);
                assert.deepEqual(await evaluate('window.__policyViolations'), []);
                await evaluate(`document.getElementById('stateSuccess').scrollIntoView({block:'center'});true`);
                const folder = path.resolve('.local/evidencias-ia/20261104-v16');
                fs.mkdirSync(folder, { recursive: true });
                const screenshot = await send('Page.captureScreenshot', { format: 'png' });
                fs.writeFileSync(path.join(folder, '01-ia-respuesta-real.png'), Buffer.from(screenshot.data, 'base64'));
            }
        }
        if (route === '/Courses') {
            const details = await evaluate(`document.querySelector('a[href^="/Courses/Details/"]')?.getAttribute('href')`);
            if (details && !details.includes('?')) routes.push(details);
        }
        results.push(result);
    }
    fs.writeFileSync('tests/quality/browser-security-check.json', JSON.stringify({ time: new Date().toISOString(), browser: 'Isolated Chrome headless', results }, null, 2));
    console.log(JSON.stringify(results, null, 2));
    await send('Browser.close').catch(() => {});
} finally {
    if (socket) socket.close();
    browser.kill();
}
});
