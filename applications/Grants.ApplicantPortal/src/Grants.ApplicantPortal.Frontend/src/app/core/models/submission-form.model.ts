export interface SubmissionFormResponse {
  schema: Record<string, unknown>;
  /** A full form.io submission object — `{ data: { <componentKey>: value, ... } }` — not a flat field map. */
  data: Record<string, unknown>;
}
