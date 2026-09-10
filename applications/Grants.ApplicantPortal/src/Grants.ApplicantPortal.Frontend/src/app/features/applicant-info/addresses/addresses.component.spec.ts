import { ComponentFixture, TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { of } from 'rxjs';

import { AddressesComponent, AddressDisplay } from './addresses.component';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ApplicantService } from '../../../core/services/applicant.service';
import { ToastService } from '../../../shared/services/toast.service';
import {
  Address,
  AddressMutationResponse,
  AddressTypesResponse,
} from '../../../shared/models/applicant-info.interface';

// ── helpers ──────────────────────────────────────────────────────────────────

function makeAddress(
  id: string,
  addressType: string,
  isPrimary: boolean,
  overrides: Partial<Address> = {}
): Partial<Address> {
  return {
    id,
    addressType,
    street: `${id} Main St`,
    street2: '',
    unit: '',
    city: 'Victoria',
    province: 'BC',
    postalCode: 'V1A 1A1',
    country: 'Canada',
    isPrimary,
    isEditable: true,
    referenceNo: '',
    ...overrides,
  };
}

const ADDRESS_TYPES: AddressTypesResponse = {
  types: [
    { key: 'Physical', label: 'Physical' },
    { key: 'Mailing', label: 'Mailing' },
  ],
};

describe('AddressesComponent', () => {
  let component: AddressesComponent;
  let fixture: ComponentFixture<AddressesComponent>;
  let applicantInfoServiceSpy: jasmine.SpyObj<ApplicantInfoService>;
  let toastServiceSpy: jasmine.SpyObj<ToastService>;

  beforeEach(async () => {
    applicantInfoServiceSpy = jasmine.createSpyObj<ApplicantInfoService>(
      'ApplicantInfoService',
      [
        'getAddressesInfo',
        'getAddressTypes',
        'setAddressAsPrimary',
        'createAddress',
        'updateAddress',
        'deleteAddress',
      ]
    );
    applicantInfoServiceSpy.getAddressesInfo.and.returnValue(of({ addressesData: [] }));
    applicantInfoServiceSpy.getAddressTypes.and.returnValue(of(ADDRESS_TYPES));

    toastServiceSpy = jasmine.createSpyObj<ToastService>('ToastService', ['success', 'error']);

    await TestBed.configureTestingModule({
      imports: [AddressesComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ApplicantService,
        { provide: ApplicantInfoService, useValue: applicantInfoServiceSpy },
        { provide: ToastService, useValue: toastServiceSpy },
      ],
    }).compileComponents();

    fixture = TestBed.createComponent(AddressesComponent);
    component = fixture.componentInstance;
    component.pluginId = 'plugin-1';
    component.provider = 'prov-1';
    component.applicantId = 'applicant-1';
    component.isSingleOrg = true;
  });

  /** Boots the component with the given raw address payload. */
  function initWith(addresses: Partial<Address>[]): void {
    applicantInfoServiceSpy.getAddressesInfo.and.returnValue(of({ addressesData: addresses }));
    fixture.detectChanges();
  }

  function query(selector: string): HTMLElement | null {
    return fixture.nativeElement.querySelector(selector) as HTMLElement | null;
  }

  function primaryBlock(idKey: string): HTMLElement | null {
    return query(`[data-cy="primary-address-${idKey}"]`);
  }

  function rowById(id: string): AddressDisplay | undefined {
    return component.addresses.find((address) => address.id === id);
  }

  /**
   * Reads the primary address for a type out of the slots the template iterates, so the
   * assertions go through the same structure the rendered blocks are built from.
   */
  function primaryFor(typeKey: string): AddressDisplay | null {
    return component.primaryAddressSlots
      .find((slot) => slot.typeKey.toLowerCase() === typeKey.toLowerCase())?.address ?? null;
  }

  const PHYSICAL_PRIMARY = makeAddress('p1', 'Physical', true, { street: '1 Physical Way' });
  const PHYSICAL_OTHER = makeAddress('p2', 'Physical', false, { street: '2 Physical Way' });
  const MAILING_PRIMARY = makeAddress('m1', 'Mailing', true, { street: '1 Mailing Way' });
  const MAILING_OTHER = makeAddress('m2', 'Mailing', false, { street: '2 Mailing Way' });

  // ── rendering: one primary block per type ───────────────────────────────────

  describe('primary address blocks', () => {
    it('renders a Physical and a Mailing primary at the same time', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY]);

      const container = query('[data-cy="primary-address-info"]');
      const physical = primaryBlock('physical');
      const mailing = primaryBlock('mailing');

      expect(container).not.toBeNull();
      expect(physical).not.toBeNull();
      expect(mailing).not.toBeNull();
      expect(physical!.textContent).toContain('1 Physical Way');
      expect(mailing!.textContent).toContain('1 Mailing Way');
      expect(physical!.querySelector('.no-primary-address')).toBeNull();
      expect(mailing!.querySelector('.no-primary-address')).toBeNull();
    });

    it('resolves one primary per type into primaryAddressSlots', () => {
      initWith([PHYSICAL_PRIMARY, PHYSICAL_OTHER, MAILING_PRIMARY, MAILING_OTHER]);

      expect(component.primaryAddressSlots).toHaveSize(2);
      expect(primaryFor('Physical')?.id).toBe('p1');
      expect(primaryFor('Mailing')?.id).toBe('m1');
    });

    it('matches an oddly cased type onto its block', () => {
      initWith([makeAddress('p1', 'PHYSICAL', true, { street: '1 Physical Way' })]);

      // Asserted through the DOM rather than a slot lookup, so the case handling under test
      // is the component's own idKey derivation and not the spec helper's comparison.
      const physical = primaryBlock('physical');

      expect(physical).not.toBeNull();
      expect(physical!.textContent).toContain('1 Physical Way');
      expect(physical!.querySelector('.no-primary-address')).toBeNull();
    });

    it('shows the muted empty state for a type with no primary address', () => {
      initWith([PHYSICAL_PRIMARY]);

      const mailing = primaryBlock('mailing');

      expect(mailing).not.toBeNull();
      expect(mailing!.querySelector('.no-primary-address')).not.toBeNull();
      expect(mailing!.textContent).toContain('No primary Mailing address found.');
      expect(primaryBlock('physical')!.querySelector('.no-primary-address')).toBeNull();
    });

    it('keeps the single "No addresses found." message when there are no addresses at all', () => {
      initWith([]);

      const emptyMessage = query('[data-cy="no-addresses-message"]');

      expect(emptyMessage).not.toBeNull();
      expect(emptyMessage!.textContent).toContain('No addresses found.');
      expect(query('[data-cy="primary-address-info"]')).toBeNull();
      expect(primaryBlock('physical')).toBeNull();
      expect(primaryBlock('mailing')).toBeNull();
    });

    it('gives each block unique element ids', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY]);

      const ids = Array.from(
        fixture.nativeElement.querySelectorAll('[id^="primary-"]') as NodeListOf<HTMLElement>
      ).map((element) => element.id);

      expect(new Set(ids).size).toBe(ids.length);
      expect(ids).toContain('primary-full-address-physical');
      expect(ids).toContain('primary-full-address-mailing');
    });

    it('renders the joined street line of the primary address', () => {
      initWith([
        makeAddress('p1', 'Physical', true, { street: '10 Alpha Rd', street2: 'Bldg A', unit: '3' }),
      ]);

      expect(primaryBlock('physical')!.textContent).toContain('10 Alpha Rd, Bldg A, 3');
    });
  });

  // ── applyPrimaryFromResponse ────────────────────────────────────────────────

  describe('applyPrimaryFromResponse', () => {
    function apply(map: Record<string, string> | null | undefined): void {
      component['applyPrimaryFromResponse'](map);
    }

    it('sets the primary for a type and clears only other addresses of that type', () => {
      initWith([PHYSICAL_PRIMARY, PHYSICAL_OTHER, MAILING_PRIMARY, MAILING_OTHER]);

      apply({ Physical: 'p2', Mailing: 'm1' });

      expect(rowById('p1')!.isPrimary).toBeFalse();
      expect(rowById('p2')!.isPrimary).toBeTrue();
      expect(rowById('m1')!.isPrimary).toBeTrue();
      expect(rowById('m2')!.isPrimary).toBeFalse();
    });

    it('leaves addresses of types absent from the map completely untouched', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY, MAILING_OTHER]);

      apply({ Mailing: 'm2' });

      expect(rowById('p1')!.isPrimary).toBeTrue();
      expect(rowById('m1')!.isPrimary).toBeFalse();
      expect(rowById('m2')!.isPrimary).toBeTrue();
      expect(primaryFor('Physical')?.id).toBe('p1');
    });

    it('compares type keys and ids case-insensitively', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY, MAILING_OTHER]);

      apply({ mailing: 'M2' });

      expect(rowById('m2')!.isPrimary).toBeTrue();
      expect(rowById('m1')!.isPrimary).toBeFalse();
      expect(rowById('p1')!.isPrimary).toBeTrue();
    });

    it('does not wipe existing primary flags when the map is empty or missing', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY]);

      const before = component.addresses;

      apply(null);
      apply({});

      expect(rowById('p1')!.isPrimary).toBeTrue();
      expect(rowById('m1')!.isPrimary).toBeTrue();
      expect(component.primaryAddressSlots.filter((slot) => slot.address)).toHaveSize(2);

      // getAddressesForTable() hands this array to the datatable by reference, so a no-op
      // response must not replace it — rebuilding would re-trigger sorting and paging.
      expect(component.addresses).toBe(before);
    });
  });

  // ── set as primary ──────────────────────────────────────────────────────────

  describe('onSetAsPrimary', () => {
    it('promotes a Mailing address without disturbing the Physical primary', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY, MAILING_OTHER]);

      const response: AddressMutationResponse = {
        addressId: 'm2',
        primaryAddressIdsByType: { Physical: 'p1', Mailing: 'm2' },
      };
      applicantInfoServiceSpy.setAddressAsPrimary.and.returnValue(of(response));

      component.onSetAsPrimary(rowById('m2')!);
      fixture.detectChanges();

      expect(applicantInfoServiceSpy.setAddressAsPrimary).toHaveBeenCalledWith(
        'm2',
        'plugin-1',
        'prov-1'
      );
      expect(primaryFor('Physical')?.id).toBe('p1');
      expect(primaryFor('Mailing')?.id).toBe('m2');
      expect(primaryBlock('physical')!.textContent).toContain('1 Physical Way');
      expect(primaryBlock('mailing')!.textContent).toContain('2 Mailing Way');
      expect(toastServiceSpy.success).toHaveBeenCalled();
    });

    it('does not call the API when the applicantId is missing', () => {
      initWith([PHYSICAL_PRIMARY]);
      component.applicantId = null;

      component.onSetAsPrimary(rowById('p1')!);

      expect(applicantInfoServiceSpy.setAddressAsPrimary).not.toHaveBeenCalled();
    });
  });

  // ── save ────────────────────────────────────────────────────────────────────

  describe('onSaveNewAddress', () => {
    /** Fills the add-address form and flushes the ngModel bindings. */
    function fillNewAddressForm(isPrimary: boolean): void {
      component.onAddAddress();
      component.newAddress = {
        addressType: 'Mailing',
        street: '9 Mailing Way',
        street2: '',
        unit: '',
        city: 'Victoria',
        province: 'BC',
        postalCode: 'V1A 1A1',
        country: 'Canada',
        isPrimary,
      };
      fixture.detectChanges();
      tick();
      fixture.detectChanges();
    }

    /**
     * The next two make the requested flag and the server map DISAGREE, in both directions.
     * Agreeing fixtures cannot tell "took the server map" apart from "took what was asked for".
     */
    it('marks the saved row primary when the server resolved it, even though it was not requested', fakeAsync(() => {
      initWith([PHYSICAL_PRIMARY]);

      // The backend infers a primary for a type that has none, so the first Mailing address
      // comes back as the Mailing primary even though the applicant left the box unticked.
      applicantInfoServiceSpy.createAddress.and.returnValue(
        of({ addressId: 'm9', primaryAddressIdsByType: { Physical: 'p1', Mailing: 'm9' } })
      );

      fillNewAddressForm(false);
      component.onSaveNewAddress();
      fixture.detectChanges();

      expect(applicantInfoServiceSpy.createAddress).toHaveBeenCalled();
      expect(rowById('m9')!.isPrimary).toBeTrue();
      expect(rowById('p1')!.isPrimary).toBeTrue();
      expect(primaryFor('Mailing')?.id).toBe('m9');
    }));

    it('clears the saved row primary when the server resolved a different address for that type', fakeAsync(() => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY]);

      // Primary was requested, but the server kept m1 as the Mailing primary — the map wins.
      applicantInfoServiceSpy.createAddress.and.returnValue(
        of({ addressId: 'm9', primaryAddressIdsByType: { Physical: 'p1', Mailing: 'm1' } })
      );

      fillNewAddressForm(true);
      component.onSaveNewAddress();
      fixture.detectChanges();

      expect(rowById('m9')!.isPrimary).toBeFalse();
      expect(primaryFor('Mailing')?.id).toBe('m1');
      expect(rowById('p1')!.isPrimary).toBeTrue();
    }));

    it('keeps the requested flag when the server map omits that type', fakeAsync(() => {
      initWith([PHYSICAL_PRIMARY]);

      applicantInfoServiceSpy.createAddress.and.returnValue(
        of({ addressId: 'm9', primaryAddressIdsByType: { Physical: 'p1' } })
      );

      fillNewAddressForm(true);
      component.onSaveNewAddress();
      fixture.detectChanges();

      // Nothing in the response speaks for Mailing, so the requested value stands.
      expect(rowById('m9')!.isPrimary).toBeTrue();
      expect(rowById('p1')!.isPrimary).toBeTrue();
    }));
  });

  // ── delete ──────────────────────────────────────────────────────────────────

  describe('onConfirmDeleteAddress', () => {
    it('re-applies the per-type primaries returned after a delete', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_PRIMARY, MAILING_OTHER]);

      applicantInfoServiceSpy.deleteAddress.and.returnValue(
        of({ primaryAddressIdsByType: { Physical: 'p1', Mailing: 'm2' } })
      );

      component.onDeleteAddress(rowById('m1')!);
      component.onConfirmDeleteAddress();
      fixture.detectChanges();

      expect(component.addresses).toHaveSize(2);
      expect(primaryFor('Physical')?.id).toBe('p1');
      expect(primaryFor('Mailing')?.id).toBe('m2');
    });
  });

  // ── type-aware labels ───────────────────────────────────────────────────────

  describe('type-aware labels', () => {
    it('builds a per-row "set as primary" label from the row type', () => {
      initWith([PHYSICAL_PRIMARY, MAILING_OTHER]);

      expect(rowById('p1')!.setPrimaryLabel).toBe('Set as primary Physical address');
      expect(rowById('m2')!.setPrimaryLabel).toBe('Set as primary Mailing address');
    });

    it('points the datatable action at the per-row label field', () => {
      initWith([PHYSICAL_PRIMARY]);

      const setPrimaryAction = component.addressesTableConfig.actionItems?.find(
        (item) => item.action === 'setAsPrimary'
      );

      expect(setPrimaryAction?.labelField).toBe('setPrimaryLabel');
    });

    it('derives the checkbox label from the type selected in the form', () => {
      initWith([]);

      component.newAddress.addressType = 'Mailing';
      expect(component.primaryCheckboxLabel).toBe('Set as Primary Mailing Address');

      component.newAddress.addressType = 'Physical';
      expect(component.primaryCheckboxLabel).toBe('Set as Primary Physical Address');

      component.newAddress.addressType = '';
      expect(component.primaryCheckboxLabel).toBe('Set as Primary Address');
    });

    it('renders the type-aware checkbox label in the modal', () => {
      initWith([]);

      component.onAddAddress();
      component.newAddress.addressType = 'Mailing';
      fixture.detectChanges();

      const label = query('label[for="address-is-primary"]');

      expect(query('[data-cy="address-is-primary"]')).not.toBeNull();
      expect(label!.textContent!.trim()).toBe('Set as Primary Mailing Address');
    });
  });
});
