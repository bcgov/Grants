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
    this.workspaceDropdown.then(($dropdown) => {
      if ($dropdown.attr("aria-expanded") !== "true") {
        cy.wrap($dropdown).click();
      }
    });

    this.workspaceDropdown.should("have.attr", "aria-expanded", "true");
  }

  closeWorkspaceDropdown(): void {
    this.workspaceDropdown.then(($dropdown) => {
      if ($dropdown.attr("aria-expanded") === "true") {
        cy.wrap($dropdown).click();
      }
    });

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

  verifyWorkspaceSelection(): void {
    this.workspaceDropdown
      .should("be.visible")
      .invoke("attr", "aria-label")
      .should((label) => {
        expect(label, 'workspace aria-label').to.be.a('string');
        expect(label, 'workspace aria-label').to.include('Current workspace: ');

        const displayText = String(label).replace('Current workspace: ', '').trim();
        expect(displayText, 'selected workspace display text').to.not.equal('');
        expect(displayText, 'selected workspace display text').to.not.equal('No Workspace');

        if (displayText.includes(' > ')) {
          const [workspace, provider] = displayText.split(' > ').map((s) => s.trim());
          expect(workspace, 'workspace part').to.not.equal('');
          expect(provider, 'provider part').to.not.equal('');
        }
      });
  }

  verifyWorkspaceDropdownMenu(): void {
    this.workspaceDropdownMenu.should("be.visible").then(($menu) => {
      const headers = $menu.find(AppSelectors.Nav.dropdownHeader);
      expect(headers.length, 'workspace dropdown headers').to.be.greaterThan(0);
      expect(headers.first().text().trim(), 'workspace header text').to.not.equal('');

      if (headers.length > 1) {
        this.providersHeader.should("contain.text", "Providers");
      }

      if ($menu.find(AppSelectors.Nav.providerItemActive).length > 0) {
        this.activeProviderItem.should("be.visible");
      }

      if ($menu.find(AppSelectors.Nav.changeWorkspaceButton).length > 0) {
        this.changeWorkspaceButton
          .should("be.visible")
          .and("contain.text", "Change Workspace");
      }
    });
  }

  clickPayments(): void {
    this.paymentsLink.click();
  }
}

export const navMenuPage = new NavMenuPage();
