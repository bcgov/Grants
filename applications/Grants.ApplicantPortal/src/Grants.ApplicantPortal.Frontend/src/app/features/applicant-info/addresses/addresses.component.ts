import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';

import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ToastService } from '../../../shared/services/toast.service';
import { ApplicantInfo } from '../../../shared/models/applicant.interface';
import {
  Address,
  AddressMutationRequest,
  AddressMutationResponse,
  AddressTypeOption,
  PrimaryAddressIdsByType
} from '../../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import {
  DatatableConfig,
  DatatableActionEvent,
  DatatableSortEvent
} from '../../../shared/components/datatable/datatable.models';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

export interface AddressDisplay {
  id: string;
  addressType: string;
  street: string;
  street2: string;
  unit: string;
  city: string;
  province: string;
  postalCode: string;
  country: string;
  isPrimary: boolean;
  isEditable: boolean;
  disabledTooltip: string;
  referenceNo: string;
  fullAddress: string;
  /** Per-row label for the "set as primary" action, e.g. "Set as primary Mailing address". */
  setPrimaryLabel: string;
}

/** One rendered primary-address block — one per known address type. */
export interface PrimaryAddressSlot {
  /** Raw type key as it arrives from the API (e.g. "Physical"). */
  typeKey: string;
  /** Lowercase, dash-separated key used to build unique element ids. */
  idKey: string;
  /** Human readable type label. */
  label: string;
  /** Heading text for the block, e.g. "PRIMARY MAILING ADDRESS". */
  heading: string;
  /** Primary address for this type, or null when the applicant has none. */
  address: AddressDisplay | null;
}

/**
 * Type pre-selected in the add-address form. Purely a form default — no primary
 * address logic keys off this value.
 */
const DEFAULT_ADDRESS_TYPE = 'Physical';

@Component({
  selector: 'app-addresses',
  standalone: true,
  imports: [
    FormsModule,
    DatatableComponent,
    TooltipDirective,
  ],
  templateUrl: './addresses.component.html',
  styleUrls: ['./addresses.component.scss'],
})
export class AddressesComponent implements OnInit, OnDestroy, OnChanges {
  @ViewChild('addressForm') addressForm!: NgForm;
  @Input() pluginId!: string;
  @Input() provider!: string;
  @Input() key!: string;
  @Input() hasMultipleOrgs: boolean = false;
  @Input() isSingleOrg: boolean = false;
  @Input() applicantId: string | null = null;

  private readonly destroy$ = new Subject<void>();

  applicantInfo: ApplicantInfo | null = null;
  addresses: AddressDisplay[] = [];
  /** Primary address per address type, keyed by the lowercase type key. */
  primaryAddressesByType = new Map<string, AddressDisplay>();
  /** One slot per known address type — drives the primary address blocks. */
  primaryAddressSlots: PrimaryAddressSlot[] = [];

  // Modal properties
  showAddAddressModal = false;
  isSavingAddress = false;
  saveAddressError: string | null = null;
  formSubmitted = false;

  // Delete confirmation properties
  showDeleteConfirmModal = false;
  isDeletingAddress = false;
  deleteAddressError: string | null = null;
  addressToDelete: AddressDisplay | null = null;

  // Edit mode properties
  isEditMode = false;
  editingAddressId: string | null = null;

  newAddress: Partial<AddressDisplay> = {
    addressType: DEFAULT_ADDRESS_TYPE,
    street: '',
    street2: '',
    unit: '',
    city: '',
    province: '',
    postalCode: '',
    country: '',
    isPrimary: false,
    isEditable: true
  };

  isLoading = true;
  isHydratingAddresses = false;
  error: string | null = null;
  addressTypes: AddressTypeOption[] = [];

  // Datatable configuration
  addressesTableConfig: DatatableConfig = {
    tableId: 'addresses-table',
    defaultSortField: 'addressType',
    enableSortPersistence: true,
    columns: [
      { key: 'addressType', label: 'Type', sortable: true, cssClass: 'type-column' },
      { key: 'fullAddress', label: 'Address', sortable: true, cssClass: 'address-column' },
      { key: 'city', label: 'City', sortable: true, cssClass: 'city-column' },
      { key: 'province', label: 'Province', sortable: true, cssClass: 'province-column' },
      { key: 'postalCode', label: 'Postal Code', sortable: true, cssClass: 'postal-code-column' }
    ],
    actionsType: 'dropdown',
    actionItems: [
      { label: 'Set as primary', labelField: 'setPrimaryLabel', icon: 'fa-home', action: 'setAsPrimary' },
      { label: 'Edit', icon: 'fa-pencil-alt', action: 'edit' },
      { label: 'Delete', icon: 'fa-trash', action: 'delete', cssClass: 'text-danger' }
    ],    
    disabledActionsField: 'isEditable',
    disabledActionsTooltip: 'This address is linked to a submission. Contact the grant program administrator to update it',
    disabledActionsTooltipField: 'disabledTooltip',
    noDataMessage: 'No addresses found. Click "Add" to create your first address.',
    loadingMessage: 'Loading your addresses...'
  };

  constructor(
    private readonly applicantService: ApplicantService,
    private readonly applicantInfoService: ApplicantInfoService,
    private readonly toastService: ToastService
  ) {}

  ngOnInit(): void {
    if (this.pluginId && this.provider) {
      this.loadAddresses();
      this.loadAddressTypes();
    }
    this.loadApplicantInfo();
  }

  ngOnChanges(changes: SimpleChanges): void {
    const pluginIdChanged = changes['pluginId'];
    const providerChanged = changes['provider'];
    
    if ((pluginIdChanged && !pluginIdChanged.firstChange) || (providerChanged && !providerChanged.firstChange)) {
      if (this.pluginId && this.provider) {
        this.loadAddresses();
      }
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Helper method to get safe data for datatable
  getAddressesForTable(): AddressDisplay[] {
    return this.addresses && Array.isArray(this.addresses) ? this.addresses : [];
  }

  private updateActionsVisibility(): void {
    if (this.hasMultipleOrgs) {
      this.addressesTableConfig = { ...this.addressesTableConfig, actionsType: 'none' };
    } else {
      this.addressesTableConfig = { ...this.addressesTableConfig, actionsType: 'dropdown' };
    }
  }

  private loadApplicantInfo(): void {
    this.applicantService
      .getApplicantInfo()
      .pipe(takeUntil(this.destroy$))
      .subscribe((data) => {
        this.applicantInfo = data;
      });
  }

  private loadAddressTypes(): void {
    this.applicantInfoService.getAddressTypes(this.pluginId).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        this.addressTypes = response?.types ?? [];
        this.refreshPrimaryAddresses();
      },
      error: (error) => {
        console.error('Failed to load address types:', error);
        // Fallback to hardcoded types if API not available
        this.addressTypes = [
          { key: 'Physical', label: 'Physical' },
          { key: 'Mailing', label: 'Mailing' }
        ];
        this.refreshPrimaryAddresses();
      }
    });
  }

  private loadAddresses(): void {
    this.isLoading = true;
    this.error = null;

    this.applicantInfoService
      .getAddressesInfo(
        this.pluginId,
        this.provider
      )
      .pipe(take(1), takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isLoading = false;
          this.addresses = this.processAddressesData(result.addressesData || []);
          this.refreshPrimaryAddresses();
          this.updateActionsVisibility();
        },
        error: (error) => {
          this.isLoading = false;
          this.error = 'Failed to load addresses data';
          console.error('Error loading addresses:', error);
        },
      });
  }

  private processAddressesData(addresses: Partial<Address>[]): AddressDisplay[] {
    return addresses.map(addr => {
      const street = addr.street ?? '';
      const street2 = addr.street2 ?? '';
      const unit = addr.unit ?? '';

      const addressParts = AddressesComponent.joinAddressParts(street, street2, unit);
      const addressType = addr.addressType ?? 'Unknown';

      return {
        id: addr.id ?? '00000000-0000-0000-0000-000000000000',
        addressType,
        street,
        street2,
        unit,
        city: addr.city ?? '',
        province: addr.province ?? '',
        postalCode: addr.postalCode ?? '',
        country: addr.country ?? '',
        isPrimary: addr.isPrimary ?? false,
        isEditable: addr.isEditable ?? false,
        disabledTooltip: this.getDisabledTooltip(addr),
        referenceNo: addr.referenceNo ?? '',
        fullAddress: addressParts,
        setPrimaryLabel: this.buildSetPrimaryLabel(addressType)
      };
    });
  }

  private getDisabledTooltip(address: { isEditable?: boolean }): string {
    if (address.isEditable) {
      return '';
    }
    if (this.hasMultipleOrgs) {
      return 'Multiple organization records found — please contact support to consolidate before editing addresses';
    }
    return 'This address is linked to a submission. Contact the grant program administrator to update it';
  }

  /**
   * Applies the server-returned per-type primary map to the loaded addresses.
   *
   * For every type present in the map the matching address becomes primary and
   * every other address *of that same type* is cleared. Addresses whose type is
   * absent from the map are left untouched, so setting a primary for one type
   * never wipes the primary of another.
   */
  private applyPrimaryFromResponse(primaryAddressIdsByType: PrimaryAddressIdsByType | null | undefined): void {
    const primaryIdByType = this.normalizePrimaryMap(primaryAddressIdsByType);

    if (primaryIdByType.size > 0) {
      this.addresses = this.addresses.map(address => {
        const primaryIdForType = primaryIdByType.get(this.normalizeTypeKey(address.addressType));

        // Type not covered by the response — leave this address exactly as it is.
        if (primaryIdForType === undefined) {
          return address;
        }

        return { ...address, isPrimary: address.id.toLowerCase() === primaryIdForType };
      });
    }

    this.refreshPrimaryAddresses();
  }

  /** Lowercases both type keys and ids so comparisons are case-insensitive. */
  private normalizePrimaryMap(
    primaryAddressIdsByType: PrimaryAddressIdsByType | null | undefined
  ): Map<string, string> {
    const normalized = new Map<string, string>();

    for (const [typeKey, addressId] of Object.entries(primaryAddressIdsByType ?? {})) {
      if (typeKey && addressId) {
        normalized.set(this.normalizeTypeKey(typeKey), addressId.toLowerCase());
      }
    }

    return normalized;
  }

  private normalizeTypeKey(typeKey: string): string {
    return (typeKey ?? '').trim().toLowerCase();
  }

  /** Recomputes the per-type primary map, the rendered slots and per-row labels. */
  private refreshPrimaryAddresses(): void {
    // Relabel first so the slots below reference the current row objects.
    this.refreshSetPrimaryLabels();

    const byType = new Map<string, AddressDisplay>();

    for (const address of this.addresses) {
      if (address.isPrimary) {
        byType.set(this.normalizeTypeKey(address.addressType), address);
      }
    }

    this.primaryAddressesByType = byType;
    this.primaryAddressSlots = this.buildPrimaryAddressSlots();
  }

  /**
   * Builds one slot per known address type: every type configured for the
   * workspace plus any type already present on the applicant's addresses.
   */
  private buildPrimaryAddressSlots(): PrimaryAddressSlot[] {
    const slots: PrimaryAddressSlot[] = [];
    const seen = new Set<string>();
    const usedIdKeys = new Set<string>();
    const typeKeys = [
      ...this.addressTypes.map(type => type.key),
      ...this.addresses.map(address => address.addressType)
    ];

    for (const typeKey of typeKeys) {
      const normalized = this.normalizeTypeKey(typeKey);

      if (!normalized || seen.has(normalized)) {
        continue;
      }
      seen.add(normalized);

      const label = this.getTypeLabel(typeKey);

      slots.push({
        typeKey,
        idKey: this.buildIdKey(normalized, usedIdKeys),
        label,
        heading: `Primary ${label} Address`.toUpperCase(),
        address: this.primaryAddressesByType.get(normalized) ?? null
      });
    }

    return slots;
  }

  /**
   * Slugifies a normalized type key for use in element ids, data-cy values and the @for track
   * key. Slugging is lossy — "Home Office" and "Home-Office" both reduce to "home-office" — so
   * a counter is appended on collision. Duplicate track keys are a runtime error in Angular,
   * and duplicate ids would break the label/control pairing.
   *
   * NOTE: the Cypress selector registry mirrors this transformation in
   * `Landing.primaryAddressBlock` (applications/Grants.AutoUI/cypress/selectors/registry.ts).
   * Changing the slug rule here without updating that factory silently breaks E2E selectors —
   * `npm run validate:selectors` cannot catch it, because dynamic factory entries are skipped.
   */
  private buildIdKey(normalizedTypeKey: string, usedIdKeys: Set<string>): string {
    // Split on runs of non-alphanumerics rather than replace-then-trim: dropping the empty
    // leading and trailing segments removes the need for an anchored trim expression, which
    // backtracks super-linearly on dash-heavy input.
    const slug = normalizedTypeKey.split(/[^a-z0-9]+/).filter(Boolean).join('-') || 'unknown';

    let idKey = slug;
    let suffix = 2;

    while (usedIdKeys.has(idKey)) {
      idKey = `${slug}-${suffix++}`;
    }

    usedIdKeys.add(idKey);
    return idKey;
  }

  /** Keeps the per-row action labels in sync with the loaded address types. */
  private refreshSetPrimaryLabels(): void {
    let changed = false;

    const relabelled = this.addresses.map(address => {
      const setPrimaryLabel = this.buildSetPrimaryLabel(address.addressType);

      if (setPrimaryLabel === address.setPrimaryLabel) {
        return address;
      }

      changed = true;
      return { ...address, setPrimaryLabel };
    });

    if (changed) {
      this.addresses = relabelled;
    }
  }

  private buildSetPrimaryLabel(addressType: string): string {
    const label = this.getTypeLabel(addressType);
    return label ? `Set as primary ${label} address` : 'Set as primary';
  }

  /** Resolves the display label for a type key, matched case-insensitively. */
  private getTypeLabel(typeKey: string): string {
    const normalized = this.normalizeTypeKey(typeKey);

    if (!normalized) {
      return '';
    }

    const match = this.addressTypes.find(type => this.normalizeTypeKey(type.key) === normalized);
    return match?.label || typeKey;
  }

  /** Label for the "set as primary" checkbox, driven by the selected type. */
  get primaryCheckboxLabel(): string {
    const label = this.getTypeLabel(this.newAddress.addressType ?? '');
    return label ? `Set as Primary ${label} Address` : 'Set as Primary Address';
  }

  // Event handlers
  onAddAddress(): void {
    this.isEditMode = false;
    this.editingAddressId = null;
    this.formSubmitted = false;
    this.resetNewAddressForm();
    this.showAddAddressModal = true;
  }

  onSaveNewAddress(): void {
    this.formSubmitted = true;

    if (!this.applicantId) {
      this.saveAddressError = 'Unable to save address: applicant information is missing. Please refresh and try again.';
      return;
    }

    if (this.addressForm?.invalid || !this.isValidAddress()) {
      return;
    }

    this.isSavingAddress = true;
    this.saveAddressError = null;

    const payload: AddressMutationRequest = {
      ...(this.isEditMode && this.editingAddressId ? { addressId: this.editingAddressId } : {}),
      applicantId: this.applicantId,
      addressType: this.newAddress.addressType ?? DEFAULT_ADDRESS_TYPE,
      street: this.newAddress.street ?? '',
      street2: this.newAddress.street2 ?? '',
      unit: this.newAddress.unit ?? '',
      city: this.newAddress.city ?? '',
      province: this.newAddress.province ?? '',
      postalCode: this.newAddress.postalCode ?? '',
      country: this.newAddress.country ?? '',
      isPrimary: this.newAddress.isPrimary ?? false
    };

    const apiCall = this.isEditMode
      ? this.applicantInfoService.updateAddress(
          this.editingAddressId!,
          this.pluginId,
          this.provider,
          payload
        )
      : this.applicantInfoService.createAddress(
          this.pluginId,
          this.provider,
          payload
        );

    apiCall.pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        this.isSavingAddress = false;
        this.showAddAddressModal = false;

        const responseId = response?.addressId;

        if (!this.isEditMode && !responseId) {
          this.saveAddressError = 'Address was saved but the server did not return a valid address ID. Please refresh and try again.';
          return;
        }

        const addressId = (this.isEditMode ? this.editingAddressId : responseId) ?? '';
        const street = payload.street;
        const street2 = payload.street2 ?? '';
        const unit = payload.unit ?? '';

        const savedAddress: AddressDisplay = {
          id: addressId,
          addressType: payload.addressType,
          street,
          street2,
          unit,
          city: payload.city,
          province: payload.province,
          postalCode: payload.postalCode,
          country: payload.country ?? '',
          // applyPrimaryFromResponse below re-derives this from the per-type map, so the
          // requested value only stands when the server resolved no primary for this type.
          isPrimary: payload.isPrimary,
          // The mutation endpoints return only the address id and the per-type primary map.
          // A row the applicant just saved is editable by definition, so there is no disabled
          // tooltip, and no reference number exists until the external system assigns one.
          isEditable: true,
          disabledTooltip: '',
          referenceNo: '',
          fullAddress: AddressesComponent.joinAddressParts(street, street2, unit),
          setPrimaryLabel: this.buildSetPrimaryLabel(payload.addressType)
        };

        if (this.isEditMode && this.editingAddressId) {
          this.addresses = this.addresses.map(a =>
            a.id === this.editingAddressId ? savedAddress : a
          );
        } else {
          this.addresses = [...this.addresses, savedAddress];
        }

        this.applyPrimaryFromResponse(response?.primaryAddressIdsByType);

        const addressLabel = AddressesComponent.buildAddressLabel(payload);
        this.toastService.success(
          this.isEditMode
            ? `Address "${addressLabel}" has been updated.`
            : `Address "${addressLabel}" has been added.`
        );

        this.resetNewAddressForm();
        this.formSubmitted = false;
      },
      error: (error) => {
        console.error(`Failed to ${this.isEditMode ? 'update' : 'create'} address:`, error);
        this.isSavingAddress = false;
        this.saveAddressError = error?.error?.message || `Failed to ${this.isEditMode ? 'update' : 'create'} address. Please try again.`;
      }
    });
  }

  onCancelAddAddress(): void {
    this.showAddAddressModal = false;
    this.isSavingAddress = false;
    this.saveAddressError = null;
    this.isEditMode = false;
    this.editingAddressId = null;
    this.formSubmitted = false;
    this.resetNewAddressForm();
  }

  private resetNewAddressForm(): void {
    this.saveAddressError = null;

    if (!this.isEditMode) {
      this.newAddress = {
        addressType: DEFAULT_ADDRESS_TYPE,
        street: '',
        street2: '',
        unit: '',
        city: '',
        province: '',
        postalCode: '',
        country: '',
        isPrimary: false,
        isEditable: true
      };
    }
  }

  isValidAddress(): boolean {
    return !!(this.newAddress.addressType && this.newAddress.addressType.trim().length > 0
      && this.newAddress.street && this.newAddress.street.trim().length > 0
      && this.newAddress.city && this.newAddress.city.trim().length > 0
      && this.newAddress.province && this.newAddress.province.trim().length > 0
      && this.newAddress.postalCode && this.newAddress.postalCode.trim().length > 0);
  }

  onAddressAction(event: DatatableActionEvent): void {
    const address = event.row as AddressDisplay;

    if ((event.action === 'edit' || event.action === 'setAsPrimary' || event.action === 'delete') && !address.isEditable) {
      return;
    }

    switch (event.action) {
      case 'setAsPrimary':
        this.onSetAsPrimary(address);
        break;
      case 'edit':
        this.onEditAddress(address);
        break;
      case 'delete':
        this.onDeleteAddress(address);
        break;
      default:
        break;
    }
  }

  onEditAddress(address: AddressDisplay): void {
    this.isEditMode = true;
    this.editingAddressId = address.id;
    this.formSubmitted = false;
    this.resetNewAddressForm();

    this.newAddress = {
      addressType: address.addressType,
      street: address.street,
      street2: address.street2,
      unit: address.unit,
      city: address.city,
      province: address.province,
      postalCode: address.postalCode,
      country: address.country,
      isPrimary: address.isPrimary,
      isEditable: address.isEditable
    };

    this.showAddAddressModal = true;
  }

  onDeleteAddress(address: AddressDisplay): void {
    this.addressToDelete = address;
    this.deleteAddressError = null;
    this.showDeleteConfirmModal = true;
  }

  onConfirmDeleteAddress(): void {
    if (!this.addressToDelete) {
      return;
    }

    if (!this.applicantId) {
      this.deleteAddressError = 'Unable to delete address: applicant information is missing. Please refresh and try again.';
      return;
    }

    this.isDeletingAddress = true;
    this.deleteAddressError = null;

    this.applicantInfoService.deleteAddress(
      this.addressToDelete.id,
      this.pluginId,
      this.provider,
      this.applicantId
    ).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response: AddressMutationResponse) => {
        const deletedId = this.addressToDelete!.id;
        this.isDeletingAddress = false;
        this.showDeleteConfirmModal = false;
        this.addressToDelete = null;

        this.addresses = this.addresses.filter(a => a.id !== deletedId);

        this.applyPrimaryFromResponse(response?.primaryAddressIdsByType);

        this.toastService.success('Address has been deleted.');
      },
      error: (error) => {
        console.error('Failed to delete address:', error);
        this.isDeletingAddress = false;
        this.deleteAddressError = error?.error?.message ?? 'Failed to delete address. Please try again.';
      }
    });
  }

  onCancelDeleteAddress(): void {
    this.showDeleteConfirmModal = false;
    this.isDeletingAddress = false;
    this.deleteAddressError = null;
    this.addressToDelete = null;
  }

  onSetAsPrimary(address: AddressDisplay): void {
    if (!this.applicantId) {
      console.error('Cannot set primary address: applicantId is missing.');
      return;
    }

    this.applicantInfoService.setAddressAsPrimary(
      address.id,
      this.pluginId,
      this.provider
    )
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (response: AddressMutationResponse) => {
        this.applyPrimaryFromResponse(response?.primaryAddressIdsByType);
        const addressLabel = AddressesComponent.buildAddressLabel(address);
        const typeLabel = this.getTypeLabel(address.addressType);
        this.toastService.success(
          typeLabel
            ? `"${addressLabel}" has been set as the primary ${typeLabel} address.`
            : `"${addressLabel}" has been set as the primary address.`
        );
      },
      error: (error: unknown) => {
        console.error('Failed to set address as primary:', error);
      }
    });
  }

  onAddressSort(event: DatatableSortEvent): void {
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }

  /** Joins the street-line parts of an address, skipping the blank ones. */
  private static joinAddressParts(...parts: (string | undefined)[]): string {
    return parts.filter(Boolean).join(', ');
  }

  /** Short "street, city" label used in save and delete confirmation toasts. */
  private static buildAddressLabel(address: { street?: string; city?: string }): string {
    return AddressesComponent.joinAddressParts(address.street, address.city);
  }
}