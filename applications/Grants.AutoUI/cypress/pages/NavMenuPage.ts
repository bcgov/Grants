class NavMenuPage {
  get navMenu() {
    return cy.get("nav.nav-menu");
  }

  get applicantInfoLink() {
    return cy.get("#nav-applicant-info");
  }

  get paymentsLink() {
    return cy.get("#nav-payments");
  }

  get workspaceDropdown() {
    return cy.get("#workspaceDropdown");
  }

  get workspaceDropdownMenu() {
    return cy.get("ul[aria-labelledby='workspaceDropdown']");
  }

  get workspaceDropdownHeader() {
    return this.workspaceDropdownMenu.find(".dropdown-header").first();
  }

  get providersHeader() {
    return this.workspaceDropdownMenu.find(".dropdown-header").eq(1);
  }

  get activeProviderItem() {
    return this.workspaceDropdownMenu.find(".provider-item.active");
  }

  providerItem(providerId: string) {
    return this.workspaceDropdownMenu.find(`#provider-item-${providerId}`);
  }

  get changeWorkspaceButton() {
    return cy.get("#change-workspace-btn");
  }

  get userDropdownButton() {
    return cy.get("#headerUserDropdown");
  }

  get userDropdownMenu() {
    return cy.get("ul[aria-labelledby='headerUserDropdown']");
  }

  get logoutButton() {
    return cy.get("#headerUserDropdown-logout");
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
      .and("have.id", `provider-item-${providerName}`);
    this.changeWorkspaceButton
      .should("be.visible")
      .and("have.id", "change-workspace-btn");
  }

  clickPayments(): void {
    this.paymentsLink.click();
  }
}

export const navMenuPage = new NavMenuPage();
