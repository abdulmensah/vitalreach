# Contact form and chat spam protection

VitalReach uses reCAPTCHA v3, following KumiWebsite's score-based integration. Verification runs in the background when submitting contact messages, starting chat, and sending each chat message. There is no checkbox widget. Both applications use the legacy SiteVerify endpoint, not the Enterprise CreateAssessment API.

Register a **v3 / score-based** key. A v2 checkbox key is incompatible. Supply settings through managed .NET user secrets for local Development, the ignored `.env`, or the appropriate VPS environment file:

```
Recaptcha__SiteKey=your-public-site-key
Recaptcha__SecretKey=your-server-only-secret
Recaptcha__AllowedHosts=test.vitalreachhub.com
Recaptcha__ScoreThreshold=0.5
Recaptcha__UseRecaptchaNet=false
```

Managed secrets use colon-separated names such as `Recaptcha:SiteKey`. Nonblank environment settings take precedence; blank `.env` placeholders are ignored. `UseRecaptchaNet=true` switches both the browser script and verification endpoint to www.recaptcha.net. The default score threshold matches Kumi's 0.5; tune only after reviewing legitimate traffic.

Server verification requires success, an exact expected action (`contact`, `support_start`, or `support_chat`), a score meeting the threshold, an allowed hostname, a recent timestamp, and no provider error codes. Missing configuration, failed scripts, invalid tokens, low scores, wrong actions, and provider failures block storage or AI requests. Contact input is retained for retry. Existing honeypot and chat usage limits remain.

Use exact comma-separated hostnames without protocols, ports or paths. Google allows subdomains of registered vitalreachhub.com, but the application's allowlist requires each served hostname explicitly. Configure QA, test and production separately. Redirect-only domains do not need a widget. Never commit secrets or place them in client code. Restart after configuration changes.

For local testing, open the site using localhost and register localhost in Google. `Recaptcha__DevelopmentBypass=true` bypasses verification only when ASPNETCORE_ENVIRONMENT=Development; it defaults to false and is ignored elsewhere.

Before release, submit a real test contact message, confirm it in admin, and test a real chat exchange. Check retry with blocked scripts and rejected verification. Automated tests simulate Google responses and do not establish real scores or key compatibility. The chatbot keeps its 15-minute session limit while requiring a fresh v3 token for every message.

Verification failures log a bounded reason without secrets, response tokens, contact contents, or raw provider responses. In Development, the contact form additionally displays a local diagnostic (for example, a rejected score or missing browser token); production keeps the generic message. Script failures also show a sanitized local diagnostic. Restart and refresh before reproducing a failure after code changes. Do not lower the score threshold merely to hide a configuration failure.

References: https://developers.google.com/recaptcha/docs/v3 and https://developers.google.com/recaptcha/docs/verify
