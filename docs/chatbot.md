# AI site-help assistant

The floating assistant on Home, Shop, Health Guide, Wellness Center and Contact uses OpenAI's Responses API. It is absent from admin, checkout and consultation intake. It cannot access private records or execute actions. It receives only a bounded list of published product names/categories/paths and published locations. It is instructed to avoid medical advice and unsupported commercial claims; model behavior still needs human evaluation before release.

Configure locally in the ignored `.env`, or in each server environment file:

```
Chatbot__Enabled=true
Chatbot__ApiKey=server-only-project-api-key
Chatbot__Model=gpt-5-nano
Chatbot__MaxOutputTokens=2048
Chatbot__ReasoningEffort=low
Chatbot__MaxRequestsPerHour=60
```

Use a text model available to your API project. Keep keys out of source control and configure provider-level spending limits. The UI remains unavailable until enabled, a key and model are set, and reCAPTCHA is configured. Restart after configuration changes. Do not use Google sign-in credentials for reCAPTCHA or OpenAI.

Visitors explicitly start the chat after a disclosure and background reCAPTCHA v3 verification. A fresh token is generated and checked server-side for each message, including its action, score and hostname. Only then can messages reach OpenAI. Each circuit is limited to 20 AI attempts, five seconds between requests, 1,000 input characters per message, and a 15-minute verification session. A shared per-process hourly ceiling defaults to 60 calls. These in-memory limits reset on restart; use shared quotas for multiple application instances and provider billing controls for a financial cap.

The request uses `store: false`, no tools, and no provider conversation ID. Up to four recent exchanges are sent as context. The UI keeps up to eight exchanges in transient circuit memory and renders responses as escaped plain text. Ending the chat clears history. Provider security/abuse retention is separate from application-state storage.

Before release: manually verify with real Google keys, test a real AI exchange, confirm empty/missing keys fail closed, review mobile and keyboard interaction, and evaluate medical questions, product/stock claims, emergencies and prompt-injection attempts. Mock tests do not establish real model response quality.

API reference: https://developers.openai.com/api/docs/guides/migrate-to-responses

The output budget includes reasoning and visible text. The default is 2,048 tokens (bounded to 1,024–8,192) with low reasoning effort for gpt-5-nano. Incomplete replies, including truncated text, are not added to chat history. Failed replies retain the visitor's question for retry; there are no automatic paid retries. Empty and refused replies have separate messages. Diagnostics record only failure categories, never conversation content or raw provider responses. If selecting another model, ensure its reasoning settings are compatible.
