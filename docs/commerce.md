# Commerce operations and configuration

## Current flow

Products contain separately priced variants with unique SKUs, attributes, optional inventory counts, and image overrides. Existing products receive one default variant preserving their previous price and detail text. Unknown inventory remains unknown; no package count or weight is inferred.

The SQLite database stores guest carts, immutable order-item snapshots, quotes, payment attempts, and order history. A protected HTTP-only cookie identifies a guest for 30 days. Orders are accessible from that browser; customer accounts, email recovery, and automated order emails are not implemented. Keep the database and data-protection keys backed up together.

1. Customers add variants and submit delivery details at `/checkout`.
2. An administrator reviews requests at `/admin/orders`, confirms availability and shipping, documents applicable tax, and issues the quote. Known inventory is reserved atomically at this step. Unknown inventory must be confirmed operationally before quoting.
3. Customers revisit `/orders` and pay the confirmed amount. US orders use Stripe in USD; Ghana orders use Paystack in GHS with an explicit administrator-approved USD-to-GHS conversion rate. Catalog prices and quote components are USD.
4. Signed webhooks trigger independent provider verification of the reference, amount, and currency before an order becomes paid. The browser return URL never confirms payment.
5. Administrators record a carrier/tracking or collection reference to mark a paid order shipped. This does not book a 3PL shipment.

Unpaid orders without a gateway session can be cancelled, releasing reserved stock. Manual payment recording requires a verified reference and is blocked after gateway checkout starts. Once a gateway attempt exists, cancellation is blocked to avoid releasing inventory while payment can still succeed.

## Maryland and tax setup

The configured US seller state is **MD (Maryland)**. This is a business-location setting, not proof of registration or a universal destination tax rate. `Commerce:UsTaxConfigured` defaults to false.

Before enabling US calculation, configure the actual legal seller, full origin address and applicable registrations in Stripe Tax, assign appropriate product tax codes in product admin, and verify representative destination addresses. Then set `Commerce__UsTaxConfigured=true`. The calculation API uses the delivery address, product classifications, quantities, and confirmed shipping. Review its result before issuing the quote. Missing configuration or codes blocks automated calculation.

Ghana tax needs a reviewed local calculation. Record its basis and the reason for any zero tax in the quote evidence. Do not enter zero merely because a registration or rate is unknown. Cross-border duties and importer arrangements must be resolved in the delivery quote. No legal entity or tax registration is inferred from the Maryland selection.

This implementation calculates quotes; it does not register the business, file returns, remit tax, or create Stripe Tax reporting transactions after payment. Tax reporting and reconciliation remain operational responsibilities.

## Provider configuration

Supported keys are in `.env.example`. Put local values only in ignored `.env`; production host variables take precedence. Keep secrets server-side.

- `Commerce__PublicUrl`: canonical HTTPS storefront URL.
- `Payments__Stripe__SecretKey` and `Payments__Stripe__WebhookSecret`.
- `Payments__Paystack__SecretKey`.
- Stripe webhook: `/payments/stripe/webhook`, subscribing to `checkout.session.completed` and `checkout.session.async_payment_succeeded`.
- Paystack webhook: `/payments/paystack/webhook`, handling `charge.success`.

Use provider test mode first. Verify successful, cancelled, delayed and duplicate payment events against the deployed HTTPS endpoints before enabling live keys. The local automated suite uses fake provider responses and does not constitute provider certification.

## Current operational limits

Only one payment attempt is stored per order. Expired checkout links, uncertain initialization responses and provider reconciliation currently need operator/developer handling; there is no automatic session renewal or gateway cancellation workflow. Do not delete attempts or release inventory until the provider confirms no payment can settle. Refunds, returns, disputes and automated reconciliation are not implemented in admin and must be handled in the provider dashboard and reconciled operationally. Do not enable unattended live sales until these procedures and customer delivery/return policies are ready.

Shipping remains a manually confirmed quote pending 3PL selection. No shipping amount, delivery date, tax registration, product classification, or Ghana exchange rate is invented.

## Validation

Run `dotnet build` and `dotnet run --project tests/CommerceChecks/CommerceChecks.csproj`. The integration checks use temporary SQLite databases and fake gateways, covering fresh/legacy schema initialization, variant validation, persistent carts, quote/stock transactions, guest isolation, changed prices, idempotent checkout and webhooks, tax configuration gates, and fulfillment. The local browser workflow was checked using an isolated database: product → cart → reload → checkout → saved order request.

## Reference documentation

- [Stripe Tax coverage](https://docs.stripe.com/tax/supported-countries)
- [Stripe tax calculations](https://docs.stripe.com/api/tax/calculations/create)
- [Stripe hosted checkout](https://docs.stripe.com/api/checkout/sessions/create)
- [Stripe webhook signatures](https://docs.stripe.com/webhooks/signature)
- [Paystack transactions](https://paystack.com/docs/api/transaction/)
- [Paystack webhooks](https://paystack.com/docs/payments/webhooks/)
