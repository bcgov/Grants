/**
 * Spec stub: Single-Workspace Provider Selection Flow
 *
 * Introduced by: AB#33625 / AB#33530 — when an applicant belongs to exactly
 * one workspace, the portal skips the workspace-selection step, hides the
 * "Back to Workspaces" button on the provider screen, and shows only the
 * provider name (not "WorkspaceName > ProviderName") in the header dropdown.
 *
 * Affected components
 *   workspace-selector.component.html  — provider-back-btn conditionally hidden
 *   header.component.html              — workspace <h6> in dropdown conditionally hidden;
 *                                        displayText is provider name only
 *
 * Requirements before implementing
 *   1. A test account that belongs to exactly one workspace (single-workspace user).
 *      Add its credentials to cypress.env.json as e.g.:
 *        "singleWsUsername": "...",  "singleWsPassword": "..."
 *   2. The provider name for that workspace, e.g.:
 *        "singleWsProviderName": "PROGRAM1"
 *
 * TODO: Implement these scenarios. Assign to QA before merging to production.
 */

import { loginPage } from '../pages/LoginPage';
import { authenticatorPage } from '../pages/AuthenticatorPage';
import { bcServicesCardPage } from '../pages/BCServicesCardPage';
import { termsOfUsePage } from '../pages/TermsOfUsePage';
import { workspaceProviderSelectionPage } from '../pages/WorkspaceProviderSelectionPage';
import { navMenuPage } from '../pages/NavMenuPage';
import { AppSelectors } from '../selectors/registry';

describe('Single-Workspace Provider Selection Flow', { testIsolation: false }, () => {
  const getRequiredEnv = (key: string): string => {
    const value = Cypress.env(key);
    if (!value) {
      throw new Error(
        `Missing required Cypress env variable "${key}". ` +
          'Provide it via --env, CI config, or a local cypress.env.json.',
      );
    }
    return String(value);
  };

  const username     = () => getRequiredEnv('singleWsUsername');
  const password     = () => getRequiredEnv('singleWsPassword');
  const providerName = () => getRequiredEnv('singleWsProviderName');

  before(() => {
    cy.clearCookies();
    cy.clearLocalStorage();
    loginPage.visit();
    cy.window().then((win) => {
      win.sessionStorage.clear();
    });
  });

  // ── Login ────────────────────────────────────────────────────────────────

  it.skip('authenticates the single-workspace test user via BC Services Card', () => {
    // TODO: walk through login → Keycloak → BC Services Card → credentials
    //       (mirror Steps 1-4 from loginByBCSCFlow.cy.ts)
    loginPage.clickLogin();
    authenticatorPage.clickBCServicesCard();
    bcServicesCardPage.clickTestWithUsernamePassword();
    bcServicesCardPage.submitCredentials(username(), password());
  });

  it.skip('accepts Terms of Use if presented, then proceeds', () => {
    // TODO: identical guard to loginByBCSCFlow Step 5
    cy.url().then((url) => {
      if (url.includes('acceptTerms')) {
        termsOfUsePage.verifyPageLoaded();
        termsOfUsePage.acceptAndContinue();
        cy.url().should('not.include', 'acceptTerms');
      }
    });
  });

  // ── Provider screen (workspace-selector component) ───────────────────────

  it.skip('should land directly on the provider selection screen (no workspace step)', () => {
    // TODO: assert that the workspace-select dropdown is NOT present (user was
    //       auto-advanced because there is only one workspace available)
    cy.get(AppSelectors.Workspace.workspaceSelect).should('not.exist');
    workspaceProviderSelectionPage.verifyProviderScreenLoaded();
  });

  it.skip('should NOT display the "Back to Workspaces" button on the provider screen', () => {
    // provider-back-btn is wrapped in @if (!isSingleWorkspace) — must be absent
    cy.get(AppSelectors.Workspace.providerBackBtn).should('not.exist');
  });

  it.skip('should display the single-workspace description text on the provider screen', () => {
    // In single-workspace mode the description reads "Select a grant program to continue."
    // rather than "Select a grant program for WorkspaceName."
    // TODO: assert the description paragraph contains the expected static text
    cy.contains('Select a grant program to continue.').should('be.visible');
  });

  it.skip('should allow the user to select a provider and continue', () => {
    workspaceProviderSelectionPage.selectProvider(providerName());
    workspaceProviderSelectionPage.continueFromProvider();
    cy.url().should('include', '/app/');
  });

  // ── Header dropdown (header component) ──────────────────────────────────

  it.skip('workspace dropdown button should show only the provider name (no workspace prefix)', () => {
    // In single-workspace mode displayText = providerName only.
    // The aria-label pattern is "Current workspace: <providerName>".
    // Assert providerName is present and workspaceName is NOT part of the label.
    navMenuPage.workspaceDropdown
      .should('be.visible')
      .invoke('attr', 'aria-label')
      .should('include', providerName());
    // TODO: also assert the label does NOT include any workspace slug if the
    //       workspace name is known for this test account.
  });

  it.skip('workspace dropdown menu should NOT show the workspace name header', () => {
    // The <h6 class="dropdown-header"> with workspace description is wrapped in
    // @if (!isSingleWorkspace) — it must be absent for single-workspace accounts.
    navMenuPage.openWorkspaceDropdown();
    navMenuPage.workspaceDropdownMenu.should('be.visible');
    // Only the "Providers" header (if >1 provider) should be present; no workspace name h6.
    // TODO: assert .dropdown-header does not contain a workspace name string
    navMenuPage.closeWorkspaceDropdown();
  });

  it.skip('workspace dropdown menu should NOT show the "Change Workspace" button', () => {
    // change-workspace-btn is wrapped in *ngIf="availableWorkspaces.length > 1"
    navMenuPage.openWorkspaceDropdown();
    cy.get(AppSelectors.Nav.changeWorkspaceButton).should('not.exist');
    navMenuPage.closeWorkspaceDropdown();
  });

  // Add further scenarios identified during QA
});
