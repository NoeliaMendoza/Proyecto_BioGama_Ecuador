import http from 'k6/http';
import { check, sleep } from 'k6';

const baseUrl = __ENV.BASE_URL || 'http://localhost:30080';
const path = __ENV.TEST_PATH || '/api/info';

export const options = {
    vus: 100,
    duration: '30s',
    thresholds: {
        http_req_failed: ['rate<0.01'],
        http_req_duration: ['p(95)<250'],
        checks: ['rate==1'],
    },
};

export default function () {
    const res = http.get(`${baseUrl}${path}`, {
        timeout: '10s',
        redirects: 0,
    });
    check(res, { 'status is 200': (r) => r.status === 200 });
    sleep(0.1);
}
