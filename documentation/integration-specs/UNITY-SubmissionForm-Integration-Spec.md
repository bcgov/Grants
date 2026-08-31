# UNITY Application — Submission Form (PDF Source Data) Integration Specification

> **Purpose:** This document describes exactly how the real UNITY application must extend its existing `/api/app/applicant-profiles/profile` REST endpoint to support submission PDF view/download in the Grants Applicant Portal (ticket AB#34070). Hand this to the team/agent working on the UNITY codebase.
>
> **Status:** The Portal side is fully implemented and shipped behind this contract. The DEMO plugin (mock data) proves the pipeline end-to-end. The UNITY plugin is wired up and calling this endpoint shape already — it will simply start working once UNITY implements the extension described below. No Portal-side changes should be needed once this contract is met.

---

## 1. What this is for

The Submissions grid now has a "view/download PDF" action per submission. When clicked, the Portal renders the submission as a PDF **entirely client-side**, using [form.io](https://formio.github.io/formio.js/) to render the form definition (**schema**) populated with the applicant's answers (**data**), then rasterizing that render to a PDF in the browser.

UNITY's job is only to supply that `{ schema, data }` pair for a single submission, on request. UNITY does not generate any PDF, image, or file — just the same schema+data shape CHEFS already produces for a submission export.

---

## 2. Endpoint

**No new endpoint is required.** This reuses the existing profile-data endpoint UNITY already implements for CONTACTINFO, ADDRESSINFO, ORGINFO, SUBMISSIONINFO, and PAYMENTINFO:

```
GET /api/app/applicant-profiles/profile
```

The Portal distinguishes this request from the others via the `Key` query parameter (see below) and one additional parameter, `SubmissionId`, that only this key uses.

### Request

| Query Parameter | Required | Description |
|---|---|---|
| `TenantId` | Yes | Same as existing calls — the Portal's `Provider` value (e.g. `PROGRAM1`). |
| `Key` | Yes | **`SUBMISSIONFORMDATA`** — the new key value that identifies this request type. |
| `ProfileId` | Yes | Same as existing calls — the Portal's internal profile GUID. |
| `Subject` | Yes | Same as existing calls — the OIDC subject, normalized per the existing convention (see [Unity-Integration.md](../architecture/Unity-Integration.md#subject-identifier-format)). |
| `SubmissionId` | Yes | **New.** GUID identifying the single submission whose form is being requested. This is one of the `id` values UNITY already returns in the `SUBMISSIONINFO` submissions list for this profile/tenant. |

Example request:

```http
GET /api/app/applicant-profiles/profile?TenantId=PROGRAM1&Key=SUBMISSIONFORMDATA&ProfileId=3fa85f64-5717-4562-b3fc-2c963f66afa6&Subject=SMZFRRLA7J5HW6Z7WZVYZDRTQ6DJ6FBR&SubmissionId=a2345e67-890a-bc12-34de-f5678901ab23
Host: unity.example.com
X-Api-Key: <api-key>
Accept: application/json
```

Auth, headers, timeout, and retry behavior are identical to every other call the Portal already makes against this endpoint — see [Unity-Integration.md § Authentication](../architecture/Unity-Integration.md#authentication). Current Portal-side config for reference: 45s timeout, 2 retry attempts, circuit breaker enabled.

---

## 3. Response

### Envelope (same convention as every other `Key`)

```json
{
  "data": {
    "dataType": "SUBMISSIONFORMDATA",
    "schema": { ... },
    "data": { ... }
  }
}
```

The Portal strips `dataType` and forwards everything else under `data` straight through to the frontend, so **`schema` and `data` are the only two fields that matter** — but they must be siblings at this exact nesting level (inside the envelope's outer `data`, alongside `dataType`).

### `schema` — the form.io form definition

The same form definition/version schema CHEFS already has for this submission's form — i.e. `{ "type": "form", "display": "form", "components": [...] }`. This is form-level, not submission-level: two submissions to the same form/program should return the same `schema`.

### `data` — the submission's answers, in form.io submission shape

> **IMPORTANT — nesting:** This must be a full form.io **submission object**, i.e. it must itself have a `data` key holding the actual field values: `{ "data": { "<componentKey>": <value>, ... } }`. **Do not** flatten this to just the field values directly under the outer `data` — the Portal passes this object straight into form.io's `setSubmission()`, which expects the standard submission shape.

If your CHEFS integration already gives you the full submission export shape (`{ "data": {...}, "state": "...", "metadata": {...} }` — see the sample below), you can pass that object through as-is; the Portal only reads `.data` off of it, so extra fields like `state`/`metadata` are harmless and don't need to be stripped.

### Full example

Using a form matching what the Portal team was given as a reference sample (a BC Gov "Strategic Forestry Envelope" form):

```json
{
  "data": {
    "dataType": "SUBMISSIONFORMDATA",
    "schema": {
      "type": "form",
      "display": "form",
      "components": [
        {
          "key": "tabs",
          "type": "tabs",
          "components": [
            {
              "key": "applicantInformation",
              "label": "3. APPLICANT INFORMATION",
              "components": [
                {
                  "key": "_ApplicantName",
                  "type": "textfield",
                  "label": "Applicant Name",
                  "input": true
                }
              ]
            }
          ]
        }
      ]
    },
    "data": {
      "data": {
        "_ApplicantName": "VelangTest2",
        "_OrganizationName": "VelangTest",
        "_projectTitle": "Maple Ridge Community Resource Development Initiative",
        "_fundingRequest": 500000
      },
      "state": "submitted"
    }
  }
}
```

This is exactly the schema+data pair the Portal team was originally given as the reference sample during design — see the CHEFS export shape it's modeled on: `submission.data` at [chefs-test.apps.silver.devops.gov.bc.ca](https://chefs-test.apps.silver.devops.gov.bc.ca) exports for a real submission.

---

## 4. Error handling

| Scenario | Expected UNITY behavior |
|---|---|
| Submission exists and belongs to the caller | `200 OK` with the envelope above. |
| `SubmissionId` doesn't exist, or doesn't belong to `ProfileId`/`Subject` | **Do not** return `200` with empty/null `schema`/`data`. Return `404` (not found) or `403` (forbidden). |
| Submission's form type doesn't support PDF generation / no schema available | `4xx` (e.g. `404` or `422`) — the Portal surfaces this to the applicant as "PDF unavailable for this submission." |
| Transient failure | Standard `5xx` — the Portal will retry (2 attempts) per its existing resilience config, matching all other calls to this endpoint. |

**Defense in depth:** The Portal already performs its own local check — before calling this endpoint, it confirms the requested `SubmissionId` is present in the caller's own cached `SUBMISSIONINFO` list for that profile/tenant, and rejects the request locally (`403`) if not. UNITY should still independently validate `SubmissionId` ownership rather than relying solely on the Portal's check, since PII/financial data is at stake.

---

## 5. Caching (informational — no action needed on UNITY's side)

The Portal caches this response **on-demand only** — i.e. only when an applicant actually clicks to view/download a PDF, never pre-fetched or eagerly seeded like `SUBMISSIONINFO`/`CONTACTINFO`/etc. The cache is scoped per `ProfileId` + `PluginId` + `Provider` + `Key` + `SubmissionId`, with the same TTL as every other cached key (currently 30 min absolute / 10 min sliding, configurable). This just means UNITY should expect at most one call per submission per cache window, not a call on every page load.

---

## 6. Where this lives in the Portal codebase (for cross-reference)

| Concern | File |
|---|---|
| Outbound call to this endpoint | `Grants.ApplicantPortal.API.Plugins/Unity/Unity.SubmissionForm.cs` |
| Key mapping (`SUBMISSIONFORM` → `SUBMISSIONFORMDATA`) | `Grants.ApplicantPortal.API.Plugins/Unity/Unity.Profile.cs` (`MapToUnityKey`) |
| Equivalent DEMO mock (for reference on expected shape) | `Grants.ApplicantPortal.API.Plugins/Demo/Data/SubmissionFormData.cs` |
| Portal's own public endpoint (frontend → Portal) | `Grants.ApplicantPortal.API.Web/Submissions/RetrieveSubmissionForm.cs` — `GET /Submissions/{PluginId}/{Provider}/{SubmissionId}/Form` |
| Frontend consumer of `{ schema, data }` | `Grants.ApplicantPortal.Frontend/src/app/core/services/submission-pdf.service.ts` |

---

## 7. Testing checklist

- [ ] `GET .../profile?...&Key=SUBMISSIONFORMDATA&SubmissionId=<valid-id>` returns `200` with the envelope shape in §3.
- [ ] `schema` is a valid form.io form definition (renders in form.io without errors).
- [ ] `data` is `{ "data": { ...fields matching the schema's component keys... } }` — **not** flattened.
- [ ] A `SubmissionId` belonging to a different applicant returns `403`/`404`, not `200`.
- [ ] An invalid/unknown `SubmissionId` returns `404`.
- [ ] Response time is consistent with the Portal's other profile-data calls (typically < 100ms per [Unity-Integration.md § Performance](../architecture/Unity-Integration.md#performance)).

---

## Related Documentation

- [Unity-Integration.md](../architecture/Unity-Integration.md) — general Unity REST/RabbitMQ integration overview, auth, subject format
- [Client-Side-Submission-PDF-Generation.md](../architecture/Client-Side-Submission-PDF-Generation.md) — how the Portal frontend consumes this data
- [API-Endpoints.md](../auto/API-Endpoints.md) — Portal's own endpoint reference, including `RetrieveSubmissionForm`
