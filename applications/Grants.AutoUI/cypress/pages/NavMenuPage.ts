import { AppSelectors } from '../selectors/registry';

class NavMenuPage {
  get navMenu() {
    return cy.get(AppSelectors.Nav.menu);
  }

  get applicantInfoLink() {
    return cy.get(AppSelectors.Nav.applicantInfoLink);
  }

  get paymentsLink() {
    return cy.get(AppSelectors.Nav.paymentsLink);
  }

  get workspaceDropdown() {
    return cy.get(AppSelectors.Nav.workspaceDropdown);
  }

  get workspaceDropdownMenu() {
    return cy.get(AppSelectors.Nav.workspaceDropdownMenu);
  }

  get workspaceDropdownHeader() {
    return this.workspaceDropdownMenu.find(AppSelectors.Nav.dropdownHeader).first();
  }

  get providersHeader() {
    return this.workspaceDropdownMenu.find(AppSelectors.Nav.dropdownHeader).eq(1);
  }

  get activeProviderItem() {
    return this.workspaceDropdownMenu.find(AppSelectors.Nav.providerItemActive);
  }

  providerItem(providerId: string) {
    return cy.get(AppSelectors.Nav.providerItem(providerId));
  }

  get changeWorkspaceButton() {
    return cy.get(AppSelectors.Nav.changeWorkspaceButton);
  }

  get userDropdownButton() {
    return cy.get(AppSelectors.Nav.userDropdownButton);
  }

  get userDropdownMenu() {
    return cy.get(AppSelectors.Nav.userDropdownMenu);
  }

  get logoutButton() {
    return cy.get(AppSelectors.Nav.logoutButton);
  }

  openUserDropdown(): void {
    this.userDropdownButton.click();
  }

  openWorkspaceDropdown(): void {
    this.workspaceDropdown
      .should("have.attr", "aria-expanded", "false")
      .click();
    this.workspaceDropdown.should("have.attr", "aria-expanded", "true");
  }

  closeWorkspaceDropdown(): void {
    this.workspaceDropdown.should("have.attr", "aria-expanded", "true").click();
    this.workspaceDropdown.should("have.attr", "aria-expanded", "false");
  }

  verifyPrimaryNavItems(): void {
    this.applicantInfoLink
      .should("be.visible")
      .and("have.attr", "href", "/app/applicant-info");
    this.paymentsLink
      .should("be.visible")
      .and("have.attr", "href", "/app/payments");
  }

  verifyWorkspaceSelection(workspaceName: string, providerName: string): void {
    this.workspaceDropdown
      .should("be.visible")
      .invoke("attr", "aria-label")
      .should("include", workspaceName)
      .and("include", providerName);
  }

  verifyWorkspaceDropdownMenu(
    workspaceName: string,
    providerName: string,
  ): void {
    this.workspaceDropdownMenu.should("be.visible");
    this.workspaceDropdownHeader.should("contain.text", workspaceName);
    this.providersHeader.should("contain.text", "Providers");
    this.activeProviderItem
      .should("be.visible")
      .and("contain.text", providerName);
    this.changeWorkspaceButton
      .should("be.visible")
      .and("contain.text", "Change Workspace");
  }

  clickPayments(): void {
    this.paymentsLink.click();
  }
}

export const navMenuPage = new NavMenuPage();
