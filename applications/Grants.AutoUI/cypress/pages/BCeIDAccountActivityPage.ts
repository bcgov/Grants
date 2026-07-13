import { ExternalSelectors } from '../selectors/external-registry';

export class BCeIDAccountActivityPage {
  private readonly activityHeading =
    'To complete login with your BCeID, review your BCeID account activity.';

  private readonly checkboxLabel =
    'Do not show me BCeID account activity when I log in';

  get pageText() {
    return cy.get(ExternalSelectors.BCeIDAccountActivity.pageText);
  }

  get doNotShowCheckbox() {
    return cy.get(ExternalSelectors.BCeIDAccountActivity.doNotShowCheckbox);
  }

  get continueButton() {
    return cy
      .contains(
        ExternalSelectors.BCeIDAccountActivity.continueButton,
        'Continue',
      )
      .first();
  }

  verifyPageLoaded(): void {
    this.pageText.should('contain.text', this.activityHeading);
    cy.contains(this.checkboxLabel).should('be.visible');
    this.continueButton.should('be.visible');
  }

  skipFutureActivityPrompt(): void {
    cy.get('body').then(($body) => {
      if ($body.find(ExternalSelectors.BCeIDAccountActivity.doNotShowCheckbox).length > 0) {
        this.doNotShowCheckbox.first().check({ force: true });
      }
    });
  }

  waitForRedirectToApp(): void {
    cy.location('pathname', { timeout: 60000 }).should((pathname) => {
      expect(
        pathname === '/workspace-selector' || pathname.startsWith('/app/'),
        `expected redirect into portal, got ${pathname}`,
      ).to.be.true;
    });
  }

  continueToPortal(): void {
    this.continue();
    this.waitForRedirectToApp();
  }

  continue(): void {
    this.continueButton.click();
  }
}

export const bceidAccountActivityPage = new BCeIDAccountActivityPage();
