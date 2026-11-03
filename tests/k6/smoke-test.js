import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:30080';

export const options = {
    vus: 1,
    duration: '10s',
    thresholds: {
        http_req_failed: ['rate<0.01'],
        checks: ['rate==1'],
    },
};

export default function () {
    const res = http.get(`${baseUrl}/api/info`, { timeout: '10s' });
    let data = {};
    try { data = res.json(); } catch { /* Validated by checks below. */ }
    check(res, {
        'status is 200': (r) => r.status === 200,
        'version and pod present': () => Boolean(data.version && data.pod),
    });
    if (res.status === 200) {
        console.log(`VU ${__VU} | Pod: ${data.pod} | Version: ${data.version}`);
    }
    sleep(1);
}
