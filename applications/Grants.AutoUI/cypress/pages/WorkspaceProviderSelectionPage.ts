import { AppSelectors } from '../selectors/registry';

export class WorkspaceProviderSelectionPage {
  private readonly numberWords: Record<string, string> = {
    '1': 'One',
    '2': 'Two',
    '3': 'Three',
    '4': 'Four',
    '5': 'Five',
    '6': 'Six',
    '7': 'Seven',
    '8': 'Eight',
    '9': 'Nine',
  };

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

  private normalizeOption(text: string): string {
    return text.toLowerCase().replace(/[^a-z0-9]/g, '');
  }

  private getProviderAliases(providerName: string): string[] {
    const aliases = new Set<string>([providerName.trim()]);
    const match = providerName.trim().match(/^program\s*(\d+)$/i);

    if (match) {
      const digit = match[1];
      aliases.add(`PROGRAM${digit}`);
      aliases.add(`Program ${digit}`);

      const word = this.numberWords[digit];
      if (word) {
        aliases.add(`Program ${word}`);
      }
    }

    return Array.from(aliases);
  }

  selectProvider(providerName: string): void {
    this.providerSelect.should("be.visible").then(($select) => {
      const aliases = this.getProviderAliases(providerName);
      const normalizedAliases = aliases.map((alias) => this.normalizeOption(alias));
      const availableOptions = Array.from($select.find('option'))
        .map((option) => option.textContent?.trim() ?? '')
        .filter((text) => text.length > 0 && !text.startsWith('-- '));

      const matchedOption = availableOptions.find((option) => {
        const normalizedOption = this.normalizeOption(option);
        return normalizedAliases.includes(normalizedOption);
      });

      expect(
        matchedOption,
        `provider option matching "${providerName}" in [${availableOptions.join(', ')}]`,
      ).to.exist;

      cy.wrap($select).select(matchedOption as string);
    });
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
