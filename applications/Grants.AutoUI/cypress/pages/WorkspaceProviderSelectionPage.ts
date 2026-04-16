export class WorkspaceProviderSelectionPage {
  // Workspace screen
  get workspaceSelect() {
    return cy.get('[data-cy="workspace-select"]');
  }

  get workspaceContinueButton() {
    return cy.get('[data-cy="workspace-continue-btn"]');
  }

  // Provider screen
  get providerLabel() {
    return cy.get('label[for="provider-select"]');
  }

  get providerSelect() {
    return cy.get('[data-cy="provider-select"]');
  }

  get providerContinueButton() {
    return cy.get('[data-cy="provider-continue-btn"]');
  }

  // Actions
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
