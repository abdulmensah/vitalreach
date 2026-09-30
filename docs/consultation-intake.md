# Consultation intake

## Routes and access

- `/consultation-intake`: adult visitor questionnaires, consent, dated measurements and immediate safety prompts.
- `/admin/consultations`: clinical inbox, full answers, rule evidence/references, review notes and QR download.
- `/admin/consultations/qr.svg`: authenticated SVG download for a reception poster. Encodes only the canonical blank-form URL.
- `/admin/users`: superadmin-only access management. Existing ordinary admin sessions cannot invoke user-management actions after permission is removed.

The one-time `clinical-access-v1` migration grants clinical access to `abdulmensah@gmail.com` and `masaoudaa@gmail.com`, and superadmin access to Abdul. It does not create missing accounts, reactivate disabled accounts, or re-grant revoked roles on restart. New accounts have neither role. A superadmin can grant each role separately. Disabling an account disables all its permissions. A superadmin cannot remove/disable their own account or revoke their own superadmin role.

## Activation

Set `Intake__PublicUrl` to the HTTPS origin visitors can reach. Localhost cannot be used by visitors' phones. Set `Intake__Enabled=true` to open submission; it defaults to false. Submitting also requires at least one active clinical reviewer. Keep this disabled until the responsible clinical lead has reviewed the screening rules and center staff have a procedure for monitoring and escalation. Deploy normally; schema additions are automatic and idempotent. No sensitive source PDFs are published.

Before collecting real health information, establish the applicable privacy notice, retention/deletion process, hosting and key-access controls, and staff responsibilities. This feature does not establish legal compliance or clinical validation. There is no automated retention deletion, outbound notification, diagnosis, or emergency dispatch. The live inbox must be actively reviewed by staff.

## PDF adaptation

All six supplied two-page PDFs are adapted into patient-readable questions. IDs are retained in the catalog for traceability. Shared identity, BP, medicine and notes fields are entered once. Unknown and declined are explicit responses. Clinical exam questions ask only about tests already performed. Visit receipt date and authenticated reviewer identity replace visitor-entered encounter date and “Screened By.” Facility/unit, examination findings, clinical outcome and follow-up are documented in the clinician's assessment note.

The PDF clinical outcomes and unvalidated indicator counts are not automatically assigned to patients. Staff determine the outcome. In particular, demographic items do not independently trigger kidney-disease flags and tolerance/withdrawal are not used to diagnose substance use disorder. The app includes an explicit tramadol/paracetamol combination row, separate from single-agent use.

## Rule set v1 (2026-09-30)

Flags describe reported evidence and the need for evaluation. They do not estimate disease probability. Every submission requires clinician review, including entirely negative answers. Missing answers produce an incomplete flag. Historical measurements are labeled with dates; they are not assertions about current physiology. Values are self-reported and unverified.

- Current chest pain, severe breathing difficulty/confusion, sudden stroke symptoms or immediate self-harm danger: immediate help message before consent or submission. [CDC stroke signs](https://www.cdc.gov/stroke/signs-symptoms/index.html), [NIMH warning signs](https://www.nimh.nih.gov/health/publications/warning-signs-of-suicide).
- Chest pain / breathing difficulty references: [CDC heart attack symptoms](https://www.cdc.gov/heart-disease/about/heart-attack.html).
- Self-harm thoughts in the prior two weeks: immediate clinical safety assessment; distinguish from confirmed danger now. [NIMH](https://www.nimh.nih.gov/health/publications/warning-signs-of-suicide).
- BP: either component at least 180/120 prompts urgent review; current concerning symptoms require emergency help. The boundary is deliberately conservative (`>=` versus AHA's `>`). At least 140 systolic or 90 diastolic prompts review, following the supplied checklist; lower values do not establish normal BP. [AHA severe BP](https://www.heart.org/en/health-topics/high-blood-pressure/understanding-blood-pressure-readings/when-to-call-911-for-high-blood-pressure).
- Glucose: fasting 100–125 mg/dL prompts follow-up, fasting >=126 or any >=200 prompts diagnostic evaluation. HbA1c >=5.7% prompts review, >=6.5% diagnostic evaluation. No self-reported result confirms diabetes. mmol/L glucose is multiplied by 18. [NIDDK testing](https://www.niddk.nih.gov/health-information/diabetes/overview/tests-diagnosis).
- Glucose <70 or >=300 mg/dL prompts prompt clinical assessment; current severe symptoms prompt emergency help. Historical values do not establish an emergency now. [CDC low glucose](https://www.cdc.gov/diabetes/about/low-blood-sugar-hypoglycemia.html), [CDC DKA](https://www.cdc.gov/diabetes/about/diabetic-ketoacidosis.html).
- eGFR <60 or urine ACR >=30 mg/g prompts review and confirmation. A single result cannot establish chronic kidney disease. [NIDDK kidney testing](https://www.niddk.nih.gov/health-information/kidney-disease/chronic-kidney-disease-ckd/tests-diagnosis).
- Reported jaundice prompts urgent assessment; an examination having taken place alone is not jaundice. [NHS jaundice](https://www.nhs.uk/conditions/jaundice/).
- Other reported symptoms, risks, abnormal urine/liver tests and medicines receive descriptive review flags based on the source checklist. Medicines are not labeled as proof of injury. No advice to stop, taper or change doses is generated.

Routine adult numeric interpretation is suppressed when pregnancy/postpartum status is yes or unknown; reported high BP/glucose or low glucose still prompts individual clinical assessment. Under-18 submissions are blocked and directed to assisted assessment. This is not a pediatric or obstetric screening instrument.

## Storage and operational limits

`ConsultationSubmissions` stores encrypted JSON containing patient responses, consent timestamp/version, form version, and generated flag evidence/rule version. Reviewer notes are encrypted separately. Only reference, timestamps, priority, status, concurrency version and audit metadata are stored unencrypted. List queries retrieve metadata only. A separately authorized, audited read decrypts a record. Review writes check authorization and optimistic concurrency. Duplicate public submission IDs are idempotent and cannot read or overwrite prior records.

Encryption uses ASP.NET Data Protection purpose `VitalReach.Consultation.v1`. Persist and protect `DataProtection__Path` across releases and backups; loss of the key ring makes records unreadable. Key files require OS/host encryption and restricted access: encrypting records does not protect against an attacker who can read both database and keys. Audit events record reviewer identity and access action, without patient answers. The latest review note replaces the prior note; this is an intake tool, not a complete longitudinal electronic health record.

Public form state lives in the server circuit until navigation/disconnect; no localStorage persistence or PHI query strings. Submission clears displayed answers. Health routes send `no-store`, `no-referrer` and `noindex` headers. Use private devices or close the session on a shared kiosk; implement a managed kiosk reset policy as needed. No email, analytics events, or marketing integration receives answers.

An in-process hashed-address limiter allows 20 new submissions/hour/address, plus a one-minute circuit limit. Duplicates do not consume the limit. Limits reset on process restart and shared NATs share a bucket. Use reverse-proxy/WAF limits for multi-instance deployments and adjust the policy for busy reception networks. Do not enable sensitive EF logging.

## Verification

`dotnet run --project tests/IntakeChecks --configuration Release`

Tests exercise threshold boundaries and units, incomplete answers, emergency vs historical symptoms, consent and range validation, role authorization/revocation, encrypted persistence, duplicate submission, audited reads/review writes, concurrency, additive schema creation and QR SVG generation. CI runs this suite before publishing. Clinical rules still require clinical validation; automated tests establish implementation behavior only.
