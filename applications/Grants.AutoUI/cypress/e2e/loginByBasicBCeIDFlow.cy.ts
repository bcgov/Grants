import { loginPage } from "../pages/LoginPage";
import { authenticatorPage } from "../pages/AuthenticatorPage";
import { bceidLoginPage } from "../pages/BCeIDLoginPage";
import { bceidAccountActivityPage } from "../pages/BCeIDAccountActivityPage";
import { workspaceProviderSelectionPage } from "../pages/WorkspaceProviderSelectionPage";
import { landingPage } from "../pages/LandingPage";
import { navMenuPage } from "../pages/NavMenuPage";
import { paymentsPage } from "../pages/PaymentsPage";
import { AppSelectors } from "../selectors/registry";

// testIsolation: false — browser state is preserved between tests so the flow
// navigates forward once without resetting between each step.
describe(
  "Login by Basic BCeID - Full Flow",
  { testIsolation: false },
  () => {
    const getRequiredEnv = (key: string): string => {
      const value = Cypress.env(key);
      if (!value) {
        throw new Error(
          `Missing required Cypress env variable "${key}". ` +
            "Provide it via --env, CI config, or a local cypress.env.json.",
        );
      }
      return String(value);
    };

    const username = () => getRequiredEnv("test2username");
    const password = () => getRequiredEnv("test2password");
    const currentEnv = String(
      Cypress.env("ENV") || Cypress.env("environment") || "",
    ).toLowerCase();
    const shouldValidatePayments = currentEnv !== "prod";
    const skipPaymentsInProd = (context: Mocha.Context): void => {
      if (!shouldValidatePayments) {
        context.skip();
      }
    };
    const workspaceName = () => getRequiredEnv("workspaceName");
    const providerName = () => getRequiredEnv("providerName");
    const bceidActivityHeading =
      "To complete login with your BCeID, review your BCeID account activity.";

    const waitForPostLoginDestination = () => {
      cy.get("body", { timeout: 60000 }).should(($body) => {
        const pathname = $body[0].ownerDocument.location.pathname;
        const bodyText = $body.text();

        expect(
          bodyText.includes(bceidActivityHeading) ||
            pathname === "/workspace-selector" ||
            pathname.startsWith("/app/"),
          `expected post-login destination, got ${pathname}`,
        ).to.be.true;
      });
    };

    const waitForWorkspaceSelectionOrApp = () => {
      cy.get("body", { timeout: 60000 }).should(($body) => {
        const pathname = $body[0].ownerDocument.location.pathname;
        const hasWorkspaceSelect =
          $body.find(AppSelectors.Workspace.workspaceSelect).length > 0;
        const hasProviderSelect =
          $body.find(AppSelectors.Workspace.providerSelect).length > 0;

        expect(
          pathname.startsWith("/app/") ||
            (pathname === "/workspace-selector" &&
              (hasWorkspaceSelect || hasProviderSelect)),
          `expected app route or workspace selection controls, got ${pathname}`,
        ).to.be.true;
      });
    };

    before(() => {
      cy.clearCookies();
      cy.clearLocalStorage();
      loginPage.visit();
      cy.window().then((win) => {
        win.sessionStorage.clear();
      });
    });

    context("Step 1: Landing Page", () => {
      it("displays the login card and button", () => {
        loginPage.loginCard.should("be.visible");
        loginPage.loginButton.should("be.visible").and("contain.text", "Login");
      });

      it("clicks the login button", () => {
        loginPage.clickLogin();
      });
    });

    context("Step 2: Keycloak Authenticator", () => {
      it("displays the authenticator page with Basic BCeID option", () => {
        authenticatorPage.verifyPageLoaded();
        authenticatorPage.bceidLink.should("be.visible").within(() => {
          cy.get(".kc-social-provider-name").should(
            "contain.text",
            "Basic or Business BCeID",
          );
        });
      });

      it("clicks Basic BCeID", () => {
        authenticatorPage.clickBCeID();
      });
    });

    context("Step 3: Basic BCeID Login", () => {
      it("displays the Basic BCeID login page", () => {
        bceidLoginPage.verifyPageLoaded();
        bceidLoginPage.userIdInput.should("be.visible");
        bceidLoginPage.passwordInput.should("be.visible");
        bceidLoginPage.continueButton.should("be.visible");
      });

      it("enters username and password then clicks Continue", () => {
        bceidLoginPage.enterUserId(username());
        bceidLoginPage.enterPassword(password());
        bceidLoginPage.clickContinue();
      });
    });

    context("Step 4: Basic BCeID Account Activity", () => {
      it("acknowledges account activity review if presented", () => {
        waitForPostLoginDestination();

        cy.get("body").then(($body) => {
          if ($body.text().includes(bceidActivityHeading)) {
            bceidAccountActivityPage.verifyPageLoaded();
            bceidAccountActivityPage.skipFutureActivityPrompt();
            bceidAccountActivityPage.continueToPortal();
          }
        });
      });
    });

    context("Step 5: Workspace and Provider Selection", () => {
      it("selects workspace and continues", () => {
        waitForWorkspaceSelectionOrApp();

        cy.get("body").then(($body) => {
          const pathname = $body[0].ownerDocument.location.pathname;

          if (pathname.startsWith("/app/")) {
            return;
          }

          if ($body.find(AppSelectors.Workspace.workspaceSelect).length > 0) {
            workspaceProviderSelectionPage.verifyWorkspaceScreenLoaded();
            workspaceProviderSelectionPage.selectWorkspace(workspaceName());
            workspaceProviderSelectionPage.continueFromWorkspace();
          }
        });
      });

      it("waits for provider screen, selects provider, and continues", () => {
        waitForWorkspaceSelectionOrApp();

        cy.get("body").then(($body) => {
          const pathname = $body[0].ownerDocument.location.pathname;

          if (pathname.startsWith("/app/")) {
            return;
          }

          if ($body.find(AppSelectors.Workspace.providerSelect).length > 0) {
            workspaceProviderSelectionPage.verifyProviderScreenLoaded();
            workspaceProviderSelectionPage.selectProvider(providerName());
            workspaceProviderSelectionPage.continueFromProvider();
            cy.location("pathname", { timeout: 60000 }).should("include", "/app/");
          }
        });
      });
    });

    context("Step 6: Portal Landing Page", () => {
      it("displays all four dashboard cards", () => {
        landingPage.verifyPageLoaded();
      });

      it("shows the Organization Information card with org table", () => {
        landingPage.orgInfoCard
          .find("h3")
          .should("contain.text", "Organization Information");
        landingPage.verifyOrganizationContentLoaded();
      });

      it("shows the Submissions card with submissions table", () => {
        landingPage.submissionsCard
          .find("h3")
          .should("contain.text", "Submissions");
        landingPage.submissionsTable.should("be.visible");
      });

      it("shows the Contact Information card with Add button and primary contact", () => {
        landingPage.contactsCard
          .find("h3")
          .should("contain.text", "Contact Information");
        landingPage.contactsCard.then(($card) => {
          const addButton = $card.find(AppSelectors.Landing.addContactButton);

          if (addButton.length > 0 && addButton.is(":visible")) {
            landingPage.addContactButton
              .should("be.visible")
              .and("contain.text", "Add");
          }
        });
        landingPage.verifyContactsContentLoaded();
      });

      it("shows the Address Information card with primary address", () => {
        landingPage.addressesCard
          .find("h3")
          .should("contain.text", "Address Information");
        landingPage.verifyAddressesContentLoaded();
      });
    });

    context("Step 7: Navigation Menu and Workspace Indicator", () => {
      it("shows the Applicant Info and Payments nav links", () => {
        navMenuPage.verifyPrimaryNavItems();
      });

      it("shows the workspace dropdown button with selected workspace and provider", () => {
        navMenuPage.verifyWorkspaceSelection();
      });

      it("opens the workspace dropdown and validates menu contents", () => {
        navMenuPage.openWorkspaceDropdown();
        navMenuPage.verifyWorkspaceDropdownMenu();
        navMenuPage.closeWorkspaceDropdown();
      });

      it("clicks the Payments nav link", function (this: Mocha.Context) {
        skipPaymentsInProd(this);

        navMenuPage.clickPayments();
      });
    });

    context("Step 8: Payments Page", () => {
      it("navigates to the Payments page", function (this: Mocha.Context) {
        skipPaymentsInProd(this);

        paymentsPage.verifyPageLoaded();
      });

      it("displays the payments search input", function (this: Mocha.Context) {
        skipPaymentsInProd(this);

        paymentsPage.searchInput.should("be.visible");
      });

      it("displays payment rows in the table", function (this: Mocha.Context) {
        skipPaymentsInProd(this);

        paymentsPage.tableRows.should("have.length.greaterThan", 0);
      });

      it("shows the core payments table columns", function (this: Mocha.Context) {
        skipPaymentsInProd(this);

        paymentsPage.verifyCoreColumns();
      });
    });

    context("Step 9: User Header Dropdown and Logout", () => {
      it("displays the user avatar dropdown button in the header", () => {
        navMenuPage.userDropdownButton.should("be.visible");
      });

      it("opens the user dropdown and validates the logout option", () => {
        navMenuPage.openUserDropdown();
        navMenuPage.userDropdownMenu.should("be.visible");
        navMenuPage.logoutButton
          .should("be.visible")
          .and("have.attr", "href", "#");
      });
    });
  },
);
