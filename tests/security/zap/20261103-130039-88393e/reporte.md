# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 0 |
| Medium | 2 |
| Low | 0 |
| Informational | 2 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Low | Warning |  | ZAP errors logged - see the zap.log file for details | 44    |
| Info | Informational |  | Percentage of network failures | 31 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of responses with status code 2xx | 87 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of responses with status code 4xx | 12 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type image/svg+xml | 2 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type text/css | 15 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type text/html | 35 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type text/javascript | 30 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with method GET | 100 % |
| Info | Informational | http://127.0.0.1:8080 | Count of total endpoints | 39    |
| Info | Informational | http://127.0.0.1:8080 | Percentage of slow responses | 16 % |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| CSP: Wildcard Directive | Medium | Systemic |
| CSP: style-src unsafe-inline | Medium | Systemic |
| Information Disclosure - Suspicious Comments | Informational | 1 |
| Session Management Response Identified | Informational | 1 |




## Alert Detail



### [ CSP: Wildcard Directive ](https://www.zaproxy.org/docs/alerts/10055/)



##### Medium (High)

### Description

Content Security Policy (CSP) is an added layer of security that helps to detect and mitigate certain types of attacks. Including (but not limited to) Cross Site Scripting (XSS), and data injection attacks. These attacks are used for everything from data theft to site defacement or distribution of malware. CSP provides a set of standard HTTP headers that allow website owners to declare approved sources of content that browsers should be allowed to load on that page — covered types are JavaScript, CSS, HTML frames, fonts, images and embeddable objects such as Java applets, ActiveX, audio and video files.

* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-Y4P9mGvG+HkCHUeXb1Yq8x8wkLQsihgsqnpWzNbYzyU=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `The following directives either allow wildcard sources (or ancestors), are not defined, or are overly broadly defined:
img-src`
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-HEeekAUIXCItAiQjkZS7BfMBwNIzGLE+uoXx2NLhGAo=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `The following directives either allow wildcard sources (or ancestors), are not defined, or are overly broadly defined:
img-src`
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-sMCjqhwd17CB4QtK25KblStgc2OS44XQDU4D+xd6QP8=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `The following directives either allow wildcard sources (or ancestors), are not defined, or are overly broadly defined:
img-src`
* URL: http://127.0.0.1:8080/Products
  * Node Name: `http://127.0.0.1:8080/Products`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-Or9WLl9q1rMIbJRs3QQ38+4g3mdgMvW8xPI3EUEshG4=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `The following directives either allow wildcard sources (or ancestors), are not defined, or are overly broadly defined:
img-src`
* URL: http://127.0.0.1:8080/Public/Catalogo
  * Node Name: `http://127.0.0.1:8080/Public/Catalogo`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-roz+6AJe2PE1o+zxF6poqHXcnETPIWJM9F7htTeJeME=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `The following directives either allow wildcard sources (or ancestors), are not defined, or are overly broadly defined:
img-src`

Instances: Systemic


### Solution

Ensure that your web server, application server, load balancer, etc. is properly configured to set the Content-Security-Policy header.

### Reference


* [ https://www.w3.org/TR/CSP/ ](https://www.w3.org/TR/CSP/)
* [ https://caniuse.com/#search=content+security+policy ](https://caniuse.com/#search=content+security+policy)
* [ https://content-security-policy.com/ ](https://content-security-policy.com/)
* [ https://github.com/HtmlUnit/htmlunit-csp ](https://github.com/HtmlUnit/htmlunit-csp)
* [ https://web.dev/articles/csp#resource-options ](https://web.dev/articles/csp#resource-options)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 15

#### Source ID: 3

### [ CSP: style-src unsafe-inline ](https://www.zaproxy.org/docs/alerts/10055/)



##### Medium (High)

### Description

Content Security Policy (CSP) is an added layer of security that helps to detect and mitigate certain types of attacks. Including (but not limited to) Cross Site Scripting (XSS), and data injection attacks. These attacks are used for everything from data theft to site defacement or distribution of malware. CSP provides a set of standard HTTP headers that allow website owners to declare approved sources of content that browsers should be allowed to load on that page — covered types are JavaScript, CSS, HTML frames, fonts, images and embeddable objects such as Java applets, ActiveX, audio and video files.

* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-Y4P9mGvG+HkCHUeXb1Yq8x8wkLQsihgsqnpWzNbYzyU=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `style-src includes unsafe-inline.`
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-HEeekAUIXCItAiQjkZS7BfMBwNIzGLE+uoXx2NLhGAo=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `style-src includes unsafe-inline.`
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-sMCjqhwd17CB4QtK25KblStgc2OS44XQDU4D+xd6QP8=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `style-src includes unsafe-inline.`
* URL: http://127.0.0.1:8080/Products
  * Node Name: `http://127.0.0.1:8080/Products`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-Or9WLl9q1rMIbJRs3QQ38+4g3mdgMvW8xPI3EUEshG4=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `style-src includes unsafe-inline.`
* URL: http://127.0.0.1:8080/Public/Catalogo
  * Node Name: `http://127.0.0.1:8080/Public/Catalogo`
  * Method: `GET`
  * Parameter: `Content-Security-Policy`
  * Attack: ``
  * Evidence: `default-src 'self'; base-uri 'self'; object-src 'none'; frame-ancestors 'self'; script-src 'self' 'nonce-roz+6AJe2PE1o+zxF6poqHXcnETPIWJM9F7htTeJeME=' https://www.paypal.com https://*.paypal.com https://www.paypalobjects.com https://*.paypalobjects.com; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; font-src 'self'; img-src 'self' data: https:; connect-src 'self' https://*.paypal.com https://*.paypalobjects.com ws://127.0.0.1:8080; frame-src https://*.paypal.com https://*.paypalobjects.com; form-action 'self' https://*.paypal.com;`
  * Other Info: `style-src includes unsafe-inline.`

Instances: Systemic


### Solution

Ensure that your web server, application server, load balancer, etc. is properly configured to set the Content-Security-Policy header.

### Reference


* [ https://www.w3.org/TR/CSP/ ](https://www.w3.org/TR/CSP/)
* [ https://caniuse.com/#search=content+security+policy ](https://caniuse.com/#search=content+security+policy)
* [ https://content-security-policy.com/ ](https://content-security-policy.com/)
* [ https://github.com/HtmlUnit/htmlunit-csp ](https://github.com/HtmlUnit/htmlunit-csp)
* [ https://web.dev/articles/csp#resource-options ](https://web.dev/articles/csp#resource-options)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 15

#### Source ID: 3

### [ Information Disclosure - Suspicious Comments ](https://www.zaproxy.org/docs/alerts/10027/)



##### Informational (Medium)

### Description

The response appears to contain suspicious comments which may help an attacker.

* URL: http://127.0.0.1:8080/lib/marked/marked.umd.js
  * Node Name: `http://127.0.0.1:8080/lib/marked/marked.umd.js`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `s file is generated from files in ./src/
 */`
  * Other Info: `The following pattern was used: \bFROM\b and was detected in likely comment: "/**
 * DO NOT EDIT THIS FILE
 * The code in this file is generated from files in ./src/
 */", see evidence field for the suspicious comment/snippet.`


Instances: 1

### Solution

Remove all comments that return information that may help an attacker and fix any underlying problems they refer to.

### Reference



#### CWE Id: [ 615 ](https://cwe.mitre.org/data/definitions/615.html)


#### WASC Id: 13

#### Source ID: 3

### [ Session Management Response Identified ](https://www.zaproxy.org/docs/alerts/10112/)



##### Informational (Medium)

### Description

The given response has been identified as containing a session management token. The 'Other Info' field contains a set of header tokens that can be used in the Header Based Session Management Method. If the request is in a context which has a Session Management Method set to "Auto-Detect" then this rule will change the session management to use the tokens identified.

* URL: http://127.0.0.1:8080/Products
  * Node Name: `http://127.0.0.1:8080/Products`
  * Method: `GET`
  * Parameter: `.AspNetCore.Antiforgery.dyzpht7DzB0`
  * Attack: ``
  * Evidence: `.AspNetCore.Antiforgery.dyzpht7DzB0`
  * Other Info: `cookie:.AspNetCore.Antiforgery.dyzpht7DzB0`


Instances: 1

### Solution

This is an informational alert rather than a vulnerability and so there is nothing to fix.

### Reference


* [ https://www.zaproxy.org/docs/desktop/addons/authentication-helper/session-mgmt-id/ ](https://www.zaproxy.org/docs/desktop/addons/authentication-helper/session-mgmt-id/)



#### Source ID: 3


