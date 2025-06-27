describe('Landing Page Test', () => {
  it('visits example.com', () => {
    cy.visit('https://grants.gov.bc.ca/')
    cy.contains('Enterprise Grant Management System')
  })
})