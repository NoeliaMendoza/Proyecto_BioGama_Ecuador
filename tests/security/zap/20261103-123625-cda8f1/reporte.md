# ZAP Scanning Report

ZAP by [Checkmarx](https://checkmarx.com/).


## Summary of Alerts

| Risk Level | Number of Alerts |
| --- | --- |
| High | 0 |
| Medium | 3 |
| Low | 2 |
| Informational | 1 |




## Insights

| Level | Reason | Site | Description | Statistic |
| --- | --- | --- | --- | --- |
| Low | Warning |  | ZAP errors logged - see the zap.log file for details | 37    |
| Info | Informational |  | Percentage of network failures | 31 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of responses with status code 2xx | 85 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of responses with status code 4xx | 14 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type image/svg+xml | 3 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type text/css | 12 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type text/html | 43 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with content type text/javascript | 21 % |
| Info | Informational | http://127.0.0.1:8080 | Percentage of endpoints with method GET | 100 % |
| Info | Informational | http://127.0.0.1:8080 | Count of total endpoints | 32    |
| Info | Informational | http://127.0.0.1:8080 | Percentage of slow responses | 19 % |







## Alerts

| Name | Risk Level | Number of Instances |
| --- | --- | --- |
| Content Security Policy (CSP) Header Not Set | Medium | Systemic |
| Missing Anti-clickjacking Header | Medium | Systemic |
| Sub Resource Integrity Attribute Missing | Medium | Systemic |
| Cross-Domain JavaScript Source File Inclusion | Low | 3 |
| X-Content-Type-Options Header Missing | Low | Systemic |
| Session Management Response Identified | Informational | 1 |




## Alert Detail



### [ Content Security Policy (CSP) Header Not Set ](https://www.zaproxy.org/docs/alerts/10038/)



##### Medium (High)

### Description

Content Security Policy (CSP) is an added layer of security that helps to detect and mitigate certain types of attacks, including Cross Site Scripting (XSS) and data injection attacks. These attacks are used for everything from data theft to site defacement or distribution of malware. CSP provides a set of standard HTTP headers that allow website owners to declare approved sources of content that browsers should be allowed to load on that page — covered types are JavaScript, CSS, HTML frames, fonts, images and embeddable objects such as Java applets, ActiveX, audio and video files.

* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/Courses
  * Node Name: `http://127.0.0.1:8080/Courses`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/Ia
  * Node Name: `http://127.0.0.1:8080/Ia`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/Products
  * Node Name: `http://127.0.0.1:8080/Products`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: ``
  * Other Info: ``

Instances: Systemic


### Solution

Ensure that your web server, application server, load balancer, etc. is configured to set the Content-Security-Policy header.

### Reference


* [ https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP ](https://developer.mozilla.org/en-US/docs/Web/HTTP/Guides/CSP)
* [ https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html ](https://cheatsheetseries.owasp.org/cheatsheets/Content_Security_Policy_Cheat_Sheet.html)
* [ https://www.w3.org/TR/CSP/ ](https://www.w3.org/TR/CSP/)
* [ https://w3c.github.io/webappsec-csp/ ](https://w3c.github.io/webappsec-csp/)
* [ https://web.dev/articles/csp ](https://web.dev/articles/csp)
* [ https://caniuse.com/#feat=contentsecuritypolicy ](https://caniuse.com/#feat=contentsecuritypolicy)
* [ https://content-security-policy.com/ ](https://content-security-policy.com/)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 15

#### Source ID: 3

### [ Missing Anti-clickjacking Header ](https://www.zaproxy.org/docs/alerts/10020/)



##### Medium (Medium)

### Description

The response does not protect against 'ClickJacking' attacks. It should include either Content-Security-Policy with 'frame-ancestors' directive or X-Frame-Options.

* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: `x-frame-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: `x-frame-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/Courses
  * Node Name: `http://127.0.0.1:8080/Courses`
  * Method: `GET`
  * Parameter: `x-frame-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/Ia
  * Node Name: `http://127.0.0.1:8080/Ia`
  * Method: `GET`
  * Parameter: `x-frame-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``
* URL: http://127.0.0.1:8080/Public/Catalogo
  * Node Name: `http://127.0.0.1:8080/Public/Catalogo`
  * Method: `GET`
  * Parameter: `x-frame-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: ``

Instances: Systemic


### Solution

Modern Web browsers support the Content-Security-Policy and X-Frame-Options HTTP headers. Ensure one of them is set on all web pages returned by your site/app.
If you expect the page to be framed only by pages on your server (e.g. it's part of a FRAMESET) then you'll want to use SAMEORIGIN, otherwise if you never expect the page to be framed, you should use DENY. Alternatively consider implementing Content Security Policy's "frame-ancestors" directive.

### Reference


* [ https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/X-Frame-Options ](https://developer.mozilla.org/en-US/docs/Web/HTTP/Reference/Headers/X-Frame-Options)


#### CWE Id: [ 1021 ](https://cwe.mitre.org/data/definitions/1021.html)


#### WASC Id: 15

#### Source ID: 3

### [ Sub Resource Integrity Attribute Missing ](https://www.zaproxy.org/docs/alerts/90003/)



##### Medium (High)

### Description

The integrity attribute is missing on a script or link tag served by an external server. The integrity tag prevents an attacker who have gained access to this server from injecting a malicious content.

* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />`
  * Other Info: ``
* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>`
  * Other Info: ``
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />`
  * Other Info: ``
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>`
  * Other Info: ``
* URL: http://127.0.0.1:8080/Products
  * Node Name: `http://127.0.0.1:8080/Products`
  * Method: `GET`
  * Parameter: ``
  * Attack: ``
  * Evidence: `<link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" />`
  * Other Info: ``

Instances: Systemic


### Solution

Provide a valid integrity attribute to the tag.

### Reference


* [ https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/Subresource_Integrity ](https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/Subresource_Integrity)


#### CWE Id: [ 345 ](https://cwe.mitre.org/data/definitions/345.html)


#### WASC Id: 15

#### Source ID: 3

### [ Cross-Domain JavaScript Source File Inclusion ](https://www.zaproxy.org/docs/alerts/10017/)



##### Low (Medium)

### Description

The page includes one or more script files from a third-party domain.

* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: `https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js`
  * Attack: ``
  * Evidence: `<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>`
  * Other Info: ``
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: `https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js`
  * Attack: ``
  * Evidence: `<script src="https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js"></script>`
  * Other Info: ``
* URL: http://127.0.0.1:8080/Ia
  * Node Name: `http://127.0.0.1:8080/Ia`
  * Method: `GET`
  * Parameter: `https://cdn.jsdelivr.net/npm/marked/marked.min.js`
  * Attack: ``
  * Evidence: `<script src="https://cdn.jsdelivr.net/npm/marked/marked.min.js"></script>`
  * Other Info: ``


Instances: 3

### Solution

Ensure JavaScript source files are loaded from only trusted sources, and the sources can't be controlled by end users of the application.

### Reference



#### CWE Id: [ 829 ](https://cwe.mitre.org/data/definitions/829.html)


#### WASC Id: 15

#### Source ID: 3

### [ X-Content-Type-Options Header Missing ](https://www.zaproxy.org/docs/alerts/10021/)



##### Low (Medium)

### Description

The Anti-MIME-Sniffing header X-Content-Type-Options was not set to 'nosniff'. This allows older versions of Internet Explorer and Chrome to perform MIME-sniffing on the response body, potentially causing the response body to be interpreted and displayed as a content type other than the declared content type. Current (early 2014) and legacy versions of Firefox will use the declared content type (if one is set), rather than performing MIME-sniffing.

* URL: http://127.0.0.1:8080
  * Node Name: `http://127.0.0.1:8080`
  * Method: `GET`
  * Parameter: `x-content-type-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: `This issue still applies to error type pages (401, 403, 500, etc.) as those pages are often still affected by injection issues, in which case there is still concern for browsers sniffing pages away from their actual content type.
At "High" threshold this scan rule will not alert on client or server error responses.`
* URL: http://127.0.0.1:8080/
  * Node Name: `http://127.0.0.1:8080/`
  * Method: `GET`
  * Parameter: `x-content-type-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: `This issue still applies to error type pages (401, 403, 500, etc.) as those pages are often still affected by injection issues, in which case there is still concern for browsers sniffing pages away from their actual content type.
At "High" threshold this scan rule will not alert on client or server error responses.`
* URL: http://127.0.0.1:8080/Courses
  * Node Name: `http://127.0.0.1:8080/Courses`
  * Method: `GET`
  * Parameter: `x-content-type-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: `This issue still applies to error type pages (401, 403, 500, etc.) as those pages are often still affected by injection issues, in which case there is still concern for browsers sniffing pages away from their actual content type.
At "High" threshold this scan rule will not alert on client or server error responses.`
* URL: http://127.0.0.1:8080/Ia
  * Node Name: `http://127.0.0.1:8080/Ia`
  * Method: `GET`
  * Parameter: `x-content-type-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: `This issue still applies to error type pages (401, 403, 500, etc.) as those pages are often still affected by injection issues, in which case there is still concern for browsers sniffing pages away from their actual content type.
At "High" threshold this scan rule will not alert on client or server error responses.`
* URL: http://127.0.0.1:8080/Products
  * Node Name: `http://127.0.0.1:8080/Products`
  * Method: `GET`
  * Parameter: `x-content-type-options`
  * Attack: ``
  * Evidence: ``
  * Other Info: `This issue still applies to error type pages (401, 403, 500, etc.) as those pages are often still affected by injection issues, in which case there is still concern for browsers sniffing pages away from their actual content type.
At "High" threshold this scan rule will not alert on client or server error responses.`

Instances: Systemic


### Solution

Ensure that the application/web server sets the Content-Type header appropriately, and that it sets the X-Content-Type-Options header to 'nosniff' for all web pages.
If possible, ensure that the end user uses a standards-compliant and modern web browser that does not perform MIME-sniffing at all, or that can be directed by the web application/web server to not perform MIME-sniffing.

### Reference


* [ https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/compatibility/gg622941(v=vs.85) ](https://learn.microsoft.com/en-us/previous-versions/windows/internet-explorer/ie-developer/compatibility/gg622941(v=vs.85))
* [ https://owasp.org/www-community/Security_Headers ](https://owasp.org/www-community/Security_Headers)


#### CWE Id: [ 693 ](https://cwe.mitre.org/data/definitions/693.html)


#### WASC Id: 15

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


