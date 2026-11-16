import { TestBed } from '@angular/core/testing';
import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ToastService } from '../../../shared/services/toast.service';
import { ContactsComponent } from './contacts.component';

describe('ContactsComponent table rows', () => {
  let component: ContactsComponent;
  const blankContact: ContactsComponent['contacts'][number] = {
    id: 'blank',
    contactType: 'Applicant',
    name: '',
    email: '',
    workPhoneNumber: '',
    workPhoneExtension: '',
    mobilePhoneNumber: '',
    homePhoneNumber: '',
    title: '',
    role: 'General',
    isPrimary: false,
    isEditable: true,
    disabledTooltip: '',
    applicationId: null
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ContactsComponent],
      providers: [
        { provide: ApplicantService, useValue: {} },
        { provide: ApplicantInfoService, useValue: {} },
        { provide: ToastService, useValue: {} }
      ]
    }).overrideComponent(ContactsComponent, {
      set: { template: '', imports: [] }
    });
    component = TestBed.createComponent(ContactsComponent).componentInstance;
  });

  it('creates', () => {
    expect(component).toBeTruthy();
  });

  it('returns no rows when there are no contacts', () => {
    expect(component.getContactsForTable()).toEqual([]);
  });

  it('hides contacts with empty or whitespace-only visible fields', () => {
    component.contacts = [
      blankContact,
      { ...blankContact, id: 'whitespace', name: ' ', email: '\t', title: '\n', workPhoneNumber: '  ' }
    ];

    expect(component.getContactsForTable()).toEqual([]);
    expect(component.contacts.length).toBe(2);
  });

  for (const field of ['name', 'email', 'title', 'workPhoneNumber'] as const) {
    it(`keeps a contact with only ${field} populated`, () => {
      const contact = { ...blankContact, [field]: ' value ' };
      component.contacts = [blankContact, contact];

      expect(component.getContactsForTable()).toEqual([contact]);
    });
  }

  it('does not use hidden fields to display an otherwise blank row', () => {
    component.contacts = [{ ...blankContact, mobilePhoneNumber: '5551234567', workPhoneExtension: '123' }];

    expect(component.getContactsForTable()).toEqual([]);
  });
});