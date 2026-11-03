const fs = require('node:fs');
const path = require('node:path');
const { spawn } = require('node:child_process');
const assert = require('node:assert/strict');
const { test } = require('node:test');
test('Chrome verifies CSP, live stock and AI controls without business writes', { timeout: 45000 }, async () => {
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
    await send('Page.addScriptToEvaluateOnNewDocument', { source: `window.__policyViolations=[];document.addEventListener('securitypolicyviolation',e=>window.__policyViolations.push({directive:e.effectiveDirective,blocked:e.blockedURI}));` });
    for (const route of ['/Products', '/Ia']) {
        await send('Page.navigate', { url: 'http://127.0.0.1:30080' + route });
        for (let i = 0; i < 100; i++) {
            const ready = await evaluate(route === '/Products'
                ? `document.readyState==='complete' && document.querySelector('#stockLiveStatus')?.textContent.includes('Disponibilidad en vivo.')`
                : `document.readyState==='complete' && typeof window.generarAnalisis==='function'`);
            if (ready) break;
            if (i === 99) throw new Error('Page scripts did not initialize: ' + route);
            await delay(100);
        }
        const result = await evaluate(`({path:location.pathname,violations:window.__policyViolations,stock:document.querySelector('#stockLiveStatus')?.textContent,inlineHandlers:document.querySelectorAll('[onclick],[onsubmit]').length})`);
        assert.equal(result.path, route);
        assert.equal(result.inlineHandlers, 0);
        assert.equal(result.violations.length, 0, 'Unexpected resource blocked by CSP');
        if (route === '/Ia') {
            const action = await evaluate(`(()=>{let called='';const original=window.generarAnalisis;window.generarAnalisis=type=>{called=type};document.querySelector('[data-analysis-type="ficha"]').click();window.generarAnalisis=original;return called})()`);
            assert.equal(action, 'ficha');
            result.aiButton = 'PASS; handler substituted to avoid calling AI service';
            result.selection = await evaluate(`(()=>{const species={id:987654321,name:'Synthetic <img src=x onerror=alert(1)>',scientific:'Synthetic species',code:'<script>alert(1)</script>'};speciesList.push(species);const input=document.querySelector('#speciesInput');input.value=species.name;input.dispatchEvent(new Event('input',{bubbles:true}));const dropdown=document.querySelector('#speciesDropdown');if(dropdown.querySelector('img,script'))throw new Error('Unescaped species text');document.querySelector('[data-species-id="'+species.id+'"]').click();if(document.querySelector('#speciesId').value!==String(species.id))throw new Error('Selection failed');return 'PASS; browser-only synthetic species, no database write'})()`);
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
