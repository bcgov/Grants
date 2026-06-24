import { AppSelectors } from '../selectors/registry';

export class WorkspaceProviderSelectionPage {
  // ── Workspace screen ──────────────────────────────────────────────────────
  get workspaceSelect() {
    return cy.get(AppSelectors.Workspace.workspaceSelect);
  }

  get workspaceContinueButton() {
    return cy.get(AppSelectors.Workspace.workspaceContinueBtn);
  }

  // ── Provider screen ───────────────────────────────────────────────────────
  get providerLabel() {
    return cy.get(AppSelectors.Workspace.providerLabel);
  }

  get providerSelect() {
    return cy.get(AppSelectors.Workspace.providerSelect);
  }

  get providerContinueButton() {
    return cy.get(AppSelectors.Workspace.providerContinueBtn);
  }

  // ── Actions ───────────────────────────────────────────────────────────────
  verifyWorkspaceScreenLoaded(): void {
    this.workspaceSelect.should("be.visible");
    this.workspaceContinueButton.should("be.visible");
  }

  selectWorkspace(workspaceName: string): void {
    this.workspaceSelect.should("be.visible").select(workspaceName);
  }

  continueFromWorkspace(): void {
    this.workspaceContinueButton.should("be.visible").click();
  }

  verifyProviderScreenLoaded(): void {
    this.providerLabel.should("be.visible").and("contain.text", "Provider");
    this.providerSelect.should("be.visible");
    this.providerContinueButton.should("be.visible");
  }

  selectProvider(providerName: string): void {
    this.providerSelect.should("be.visible").select(providerName);
  }

  continueFromProvider(): void {
    this.providerContinueButton.should("be.visible").click();
  }

  completeSelection(workspaceName: string, providerName: string): void {
    this.verifyWorkspaceScreenLoaded();
    this.selectWorkspace(workspaceName);
    this.continueFromWorkspace();
    this.verifyProviderScreenLoaded();
    this.selectProvider(providerName);
    this.continueFromProvider();
  }
}

export const workspaceProviderSelectionPage =
  new WorkspaceProviderSelectionPage();
