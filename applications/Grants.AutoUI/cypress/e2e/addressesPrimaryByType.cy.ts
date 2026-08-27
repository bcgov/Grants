/**
 * Spec stub: Addresses — Primary Address Per Type (AB#34140)
 *
 * Introduced by: the applicant-address invariant changing from "at most one
 * primary address globally" to "at most one primary WITHIN EACH address type
 * group". An applicant can now hold a Primary Physical AND a Primary Mailing
 * at the same time. An address always has exactly one type; `isPrimary` is
 * still a boolean, only the scope of exclusivity changed.
 *
 * Affected components
 *   addresses.component.html — the single "PRIMARY ADDRESS" banner became one
 *                              block per address type, rendered inside the
 *                              preserved data-cy="primary-address-info"
 *                              container. Inner blocks are derived from the
 *                              type key, so a configured "Business" type would
 *                              yield primary-address-business.
 *   addresses.component.ts   — primaryAddressesByType map; applyPrimaryFromResponse
 *                              clears isPrimary only within the responding type
 *                              and leaves absent types untouched.
 *   datatable.component.ts   — new optional labelField gives the row action a
 *                              per-row label ("Set as primary Mailing address").
 *
 * Backend contract note
 *   The four address mutation endpoints no longer return a scalar
 *   `primaryAddressId`. They return `primaryAddressIdsByType`, a map of
 *   address type -> primary address id, always present (`{}` when nothing
 *   resolves). A type group with no explicitly flagged primary INFERS one
 *   (most recently created), so any type with at least one address will
 *   always show a primary.
 *
 * Requirements before implementing
 *   1. A test account whose address data includes:
 *      - at least one Physical AND one Mailing address (to see two primaries)
 *      - two or more addresses of the SAME type (to exercise exclusivity)
 *      - a type with zero addresses (to see the per-type empty state)
 *   2. Confirm the plugin/provider combination that surfaces these in the
 *      target environment. Demo data mirrors this in AddressesData.cs, but
 *      AutoUI targets deployed environments, not localhost.
 *
 * TODO: Implement these scenarios. Assign to QA before merging to production.
 */

import { AppSelectors } from '../selectors/registry';

describe('Addresses — Primary Address Per Type', { testIsolation: false }, () => {
  before(() => {
    // TODO: authenticate and navigate to the applicant profile addresses card
    //       (mirror the login flow from loginByBCSCFlow.cy.ts, then use
    //       LandingPage.addressesCard).
  });

  // ── Two primaries render simultaneously ───────────────────────────────────

  it.skip('shows a Physical and a Mailing primary at the same time', () => {
    // TODO: with at least one address of each type, assert BOTH per-type blocks
    //       render inside the preserved container. This is the core regression
    //       guard for AB#34140 — before this change only one could ever show.
    cy.get(AppSelectors.Landing.primaryAddressInfo).should('be.visible');
    cy.get(AppSelectors.Landing.primaryAddressBlock('physical')).should('be.visible');
    cy.get(AppSelectors.Landing.primaryAddressBlock('mailing')).should('be.visible');
  });

  // ── Exclusivity is scoped to the type ─────────────────────────────────────

  it.skip('setting a Mailing primary leaves the Physical primary untouched', () => {
    // TODO: capture the Physical block's address text, then use the row action
    //       ("Set as primary Mailing address") on a different Mailing address.
    //       Assert the Mailing block changed AND the Physical block is
    //       byte-identical to what it was. This is the invariant most likely
    //       to regress if any cache-patch path drops its type scoping.
  });

  it.skip('setting a primary within one type demotes only that type', () => {
    // TODO: with two or more Mailing addresses, set the non-primary one as
    //       primary and assert exactly ONE Mailing address is flagged primary
    //       afterwards — and that the Physical count is still exactly one.
  });

  // ── Per-type empty state ──────────────────────────────────────────────────

  it.skip('shows the per-type empty state for a type with no addresses', () => {
    // TODO: for an applicant with only Physical addresses, assert the Mailing
    //       block still renders and shows its muted empty state, rather than
    //       the block being omitted.
  });

  it.skip('shows the single no-addresses message when the applicant has none', () => {
    // TODO: with zero addresses in total the original single empty state must
    //       render INSTEAD of two empty per-type blocks.
    cy.get(AppSelectors.Landing.noAddressesMessage).should('be.visible');
    cy.get(AppSelectors.Landing.primaryAddressInfo).should('not.exist');
  });

  // ── Type-aware labels ─────────────────────────────────────────────────────

  it.skip('labels the row action with the address type of that row', () => {
    // TODO: assert a Mailing row's dropdown action reads "Set as primary
    //       Mailing address" and a Physical row's reads "...Physical address".
    //       Without a Primary column in the portal table, this label is the
    //       only cue telling the user which slot they are filling.
  });

  it.skip('labels the modal checkbox with the currently selected type', () => {
    // TODO: open the add-address modal, switch AppSelectors.Addresses.typeSelect
    //       between Physical and Mailing, and assert the label next to
    //       AppSelectors.Addresses.isPrimaryToggle follows the selection.
  });
});
