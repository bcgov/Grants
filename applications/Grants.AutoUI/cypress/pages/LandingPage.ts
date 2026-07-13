import { AppSelectors } from '../selectors/registry';

class LandingPage {
  // ── Cards ─────────────────────────────────────────────────────────────────
  get orgInfoCard() {
    return cy.get(AppSelectors.Landing.orgInfoCard);
  }

  get submissionsCard() {
    return cy.get(AppSelectors.Landing.submissionsCard);
  }

  get contactsCard() {
    return cy.get(AppSelectors.Landing.contactsCard);
  }

  get addressesCard() {
    return cy.get(AppSelectors.Landing.addressesCard);
  }

  // ── Organization Information ───────────────────────────────────────────────
  get orgTable() {
    return cy.get(AppSelectors.Landing.orgTable);
  }

  get orgNameField() {
    return cy.get(AppSelectors.Landing.orgNameField);
  }

  get regNumberField() {
    return cy.get(AppSelectors.Landing.regNumberField);
  }

  // ── Submissions ───────────────────────────────────────────────────────────
  get submissionsTable() {
    return cy.get(AppSelectors.Landing.submissionsTable);
  }

  // ── Contacts ─────────────────────────────────────────────────────────────
  get addContactButton() {
    return cy.get(AppSelectors.Landing.addContactButton);
  }

  get primaryContactInfo() {
    return cy.get(AppSelectors.Landing.primaryContactInfo);
  }

  get noPrimaryContact() {
    return cy.get(AppSelectors.Landing.noPrimaryContact);
  }

  // ── Addresses ────────────────────────────────────────────────────────────
  get primaryAddressInfo() {
    return cy.get(AppSelectors.Landing.primaryAddressInfo);
  }

  get noAddressesMessage() {
    return cy.get(AppSelectors.Landing.noAddressesMessage);
  }

  verifyPageLoaded(): void {
    // Use path assertion to avoid failures caused by baseUrl formatting differences.
    cy.url().should("include", "/app/");
    this.orgInfoCard.should("be.visible");
    this.submissionsCard.should("be.visible");
    this.contactsCard.should("be.visible");
    this.addressesCard.should("be.visible");
  }

  verifyOrganizationContentLoaded(): void {
    this.orgInfoCard
      .should("be.visible")
      .should(($card) => {
        const hasOrgTable =
          $card.find(AppSelectors.Landing.orgTableInner).length > 0;
        const hasFallbackFields =
          $card.find(AppSelectors.Landing.orgNameField).length > 0 &&
          $card.find(AppSelectors.Landing.regNumberField).length > 0;

        expect(
          hasOrgTable || hasFallbackFields,
          "organization content should render as table or fallback fields",
        ).to.be.true;
      })
      .then(($card) => {
        const hasOrgTable =
          $card.find(AppSelectors.Landing.orgTableInner).length > 0;

        if (hasOrgTable) {
          this.orgTable.should("be.visible");
          return;
        }

        this.orgNameField.should("be.visible");
        this.regNumberField.should("be.visible");
      });
  }

  verifyContactsContentLoaded(): void {
    this.contactsCard
      .should("be.visible")
      .should(($card) => {
        const hasPrimaryContact =
          $card.find(AppSelectors.Landing.primaryContactInfo).length > 0;
        const hasEmptyState =
          $card.find(AppSelectors.Landing.noPrimaryContact).length > 0;

        expect(
          hasPrimaryContact || hasEmptyState,
          "contact content should render as primary contact or empty state",
        ).to.be.true;
      })
      .then(($card) => {
        const hasPrimaryContact =
          $card.find(AppSelectors.Landing.primaryContactInfo).length > 0;

        if (hasPrimaryContact) {
          this.primaryContactInfo.should("be.visible");
        } else {
          this.noPrimaryContact.should("be.visible");
        }
      });
  }

  verifyAddressesContentLoaded(): void {
    this.addressesCard
      .should("be.visible")
      .should(($card) => {
        const hasPrimaryAddress =
          $card.find(AppSelectors.Landing.primaryAddressInfo).length > 0;
        const hasEmptyState =
          $card.find(AppSelectors.Landing.noAddressesMessage).length > 0;

        expect(
          hasPrimaryAddress || hasEmptyState,
          "address content should render as primary address or empty state",
        ).to.be.true;
      })
      .then(($card) => {
        const hasPrimaryAddress =
          $card.find(AppSelectors.Landing.primaryAddressInfo).length > 0;

        if (hasPrimaryAddress) {
          this.primaryAddressInfo.should("be.visible");
        } else {
          this.noAddressesMessage.should("be.visible");
        }
      });
  }
}

export const landingPage = new LandingPage();
