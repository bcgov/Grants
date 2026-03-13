import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';

import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ApplicantInfo } from '../../../shared/models/applicant.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { 
  DatatableConfig,
  DatatableActionEvent,
  DatatableRowClickEvent,
  DatatableSortEvent
} from '../../../shared/components/datatable/datatable.models';

interface AddressDisplay {
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
  referenceNo: string;
  fullAddress: string;
}

@Component({
  selector: 'app-addresses',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatatableComponent,
  ],
  templateUrl: './addresses.component.html',
  styleUrls: ['./addresses.component.scss'],
})
export class AddressesComponent implements OnInit, OnDestroy, OnChanges {
  @Input() pluginId!: string;
  @Input() provider!: string;
  @Input() key!: string;

  private readonly destroy$ = new Subject<void>();

  applicantInfo: ApplicantInfo | null = null;
  addresses: AddressDisplay[] = [];
  primaryAddress: AddressDisplay | null = null;

  isLoading = true;
  isHydratingAddresses = false;
  error: string | null = null;

  // Edit modal state
  showEditAddressModal = false;
  isSavingAddress = false;
  saveAddressError: string | null = null;
  editingAddressId: string | null = null;
  editAddress: Partial<AddressDisplay> = {};

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
      { key: 'postalCode', label: 'Postal Code', sortable: true, cssClass: 'postal-code-column' },
      { key: 'isPrimary', label: 'Primary', sortable: true, type: 'boolean', cssClass: 'primary-column', booleanFalseBlank: true }
    ],
    actionsType: 'dropdown',
    actionItems: [
      { label: 'Set as primary', icon: 'fa-home', action: 'setAsPrimary' },
      { label: 'Edit', icon: 'fa-pencil-alt', action: 'edit' },
    ],    
    disabledActionsField: 'isEditable',
    disabledActionsTooltip: 'This address is linked to a submission. Contact the grant program administrator to update it',
    noDataMessage: 'No addresses found.',
    loadingMessage: 'Loading your addresses...'
  };

  constructor(
    private readonly applicantService: ApplicantService,
    private readonly applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    console.log('AddressesComponent ngOnInit - inputs:', {
      pluginId: this.pluginId,
      provider: this.provider,
      hasPluginId: !!this.pluginId,
      hasProvider: !!this.provider
    });
    
    if (this.pluginId && this.provider) {
      console.log('AddressesComponent - Calling loadAddresses()');
      this.loadAddresses();
    } else {
      console.log('AddressesComponent - Missing pluginId or provider, not loading addresses');
    }
    this.loadApplicantInfo();
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('AddressesComponent ngOnChanges called:', {
      changes,
      currentInputs: {
        pluginId: this.pluginId,
        provider: this.provider
      }
    });
    
    // Reload data when pluginId or provider changes
    const pluginIdChanged = changes['pluginId'];
    const providerChanged = changes['provider'];
    
    if ((pluginIdChanged && !pluginIdChanged.firstChange) || (providerChanged && !providerChanged.firstChange)) {
      if (this.pluginId && this.provider) {
        console.log('AddressesComponent - Input changed, reloading data:', {
          pluginIdChanged: !!pluginIdChanged,
          providerChanged: !!providerChanged,
          oldPluginId: pluginIdChanged?.previousValue,
          newPluginId: pluginIdChanged?.currentValue,
          oldProvider: providerChanged?.previousValue,
          newProvider: providerChanged?.currentValue
        });
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

  private loadApplicantInfo(): void {
    this.applicantService
      .getApplicantInfo()
      .pipe(takeUntil(this.destroy$))
      .subscribe((data) => {
        this.applicantInfo = data;
      });
  }

  private loadAddresses(): void {
    console.log('AddressesComponent loadAddresses() called with:', {
      pluginId: this.pluginId,
      provider: this.provider
    });
    
    this.isLoading = true;
    this.error = null;

    console.log('Making API call to getAddressesInfo...');
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
          this.primaryAddress = this.addresses.find(addr => addr.isPrimary) || null;
          console.log('Addresses data loaded:', this.addresses);
        },
        error: (error) => {
          this.isLoading = false;
          this.error = 'Failed to load addresses data';
          console.error('Error loading addresses:', error);
        },
      });
  }

  private processAddressesData(addresses: any[]): AddressDisplay[] {
    return addresses.map(addr => {
      const street = addr.street ?? '';
      const street2 = addr.street2 ?? '';
      const unit = addr.unit ?? '';

      const addressParts = [street, street2, unit].filter(Boolean).join(', ');

      return {
        id: addr.id ?? '00000000-0000-0000-0000-000000000000',
        addressType: addr.addressType ?? 'Unknown',
        street,
        street2,
        unit,
        city: addr.city ?? '',
        province: addr.province ?? '',
        postalCode: addr.postalCode ?? '',
        country: addr.country ?? '',
        isPrimary: addr.isPrimary ?? false,
        isEditable: addr.isEditable ?? false,
        referenceNo: addr.referenceNo ?? '',
        fullAddress: addressParts
      };
    });
  }

  // Datatable event handlers
  onAddressRowClick(event: DatatableRowClickEvent): void {
    console.log('Clicked address:', event.row);
    // TODO: Navigate to address detail view
  }

  onAddressAction(event: DatatableActionEvent): void {
    const address = event.row as AddressDisplay;

    if ((event.action === 'edit' || event.action === 'setAsPrimary') && !address.isEditable) {
      console.log('Action not allowed: Address cannot be edited');
      return;
    }

    switch (event.action) {
      case 'setAsPrimary':
        this.onSetAsPrimary(address);
        break;
      case 'edit':
        this.onEditAddress(address);
        break;
      default:
        console.log('Unknown action:', event.action);
    }
  }

  onEditAddress(address: AddressDisplay): void {
    console.log('Editing address...', address);
    this.editingAddressId = address.id;
    this.saveAddressError = null;
    this.editAddress = {
      addressType: address.addressType,
      street: address.street,
      street2: address.street2,
      unit: address.unit,
      city: address.city,
      province: address.province,
      postalCode: address.postalCode,
      country: address.country,
      isPrimary: address.isPrimary,
    };
    this.showEditAddressModal = true;
  }

  onSaveAddress(): void {
    if (!this.editingAddressId || !this.isValidAddress()) {
      return;
    }

    this.isSavingAddress = true;
    this.saveAddressError = null;

    const payload = {
      addressType: this.editAddress.addressType ?? '',
      street: this.editAddress.street ?? '',
      city: this.editAddress.city ?? '',
      province: this.editAddress.province ?? '',
      postalCode: this.editAddress.postalCode ?? '',
      isPrimary: this.editAddress.isPrimary ?? false,
      street2: this.editAddress.street2 ?? '',
      unit: this.editAddress.unit ?? '',
      country: this.editAddress.country ?? '',
    };

    this.applicantInfoService.updateAddress(
      this.editingAddressId,
      this.pluginId,
      this.provider,
      payload
    ).pipe(takeUntil(this.destroy$)).subscribe({
      next: (response) => {
        console.log('Address updated successfully:', response);
        this.isSavingAddress = false;
        this.showEditAddressModal = false;

        const primaryAddressId = response?.primaryAddressId ?? null;

        // Update local state
        this.addresses = this.addresses.map(addr => {
          const isEdited = addr.id === this.editingAddressId;
          const street = isEdited ? (this.editAddress.street ?? '') : addr.street;
          const street2 = isEdited ? (this.editAddress.street2 ?? '') : addr.street2;
          const unit = isEdited ? (this.editAddress.unit ?? '') : addr.unit;
          return {
            ...addr,
            ...(isEdited ? this.editAddress : {}),
            isPrimary: primaryAddressId !== null ? addr.id === primaryAddressId : false,
            fullAddress: isEdited ? [street, street2, unit].filter(Boolean).join(', ') : addr.fullAddress,
          } as AddressDisplay;
        });
        this.primaryAddress = this.addresses.find(a => a.isPrimary) ?? null;
        this.editingAddressId = null;
      },
      error: (error) => {
        console.error('Failed to update address:', error);
        this.isSavingAddress = false;
        this.saveAddressError = error?.error?.message ?? 'Failed to update address. Please try again.';
      },
    });
  }

  onCancelEditAddress(): void {
    this.showEditAddressModal = false;
    this.isSavingAddress = false;
    this.saveAddressError = null;
    this.editingAddressId = null;
    this.editAddress = {};
  }

  isValidAddress(): boolean {
    return !!(this.editAddress.addressType && this.editAddress.addressType.trim().length > 0
      && this.editAddress.street && this.editAddress.street.trim().length > 0
      && this.editAddress.city && this.editAddress.city.trim().length > 0
      && this.editAddress.province && this.editAddress.province.trim().length > 0
      && this.editAddress.postalCode && this.editAddress.postalCode.trim().length > 0);
  }

  onSetAsPrimary(address: AddressDisplay): void {
    console.log('Setting as primary address...', address);

    this.applicantInfoService.setAddressAsPrimary(
      address.id,
      this.pluginId,
      this.provider
    )
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (response) => {
        console.log('Address set as primary successfully:', response);
        
        const primaryAddressId = response?.primaryAddressId ?? address.id;

        // Update local state using primaryAddressId from response
        this.addresses = this.addresses.map(addr => ({
          ...addr,
          isPrimary: addr.id === primaryAddressId
        }));
        
        this.primaryAddress = this.addresses.find(a => a.isPrimary) ?? null;
        console.log('Primary address updated locally:', this.primaryAddress);
      },
      error: (error) => {
        console.error('Failed to set address as primary:', error);
      }
    });
  }

  onAddressSort(event: DatatableSortEvent): void {
    console.log('Addresses sorted by:', event.column, event.direction);
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }

  private formatFullAddress(address: AddressDisplay): string {
    const parts = [address.street, address.street2, address.unit].filter(Boolean);
    return parts.join(', ') || address.fullAddress || '';
  }

  // Helper method to format primary address display
  getPrimaryFullAddress(): string {
    if (!this.primaryAddress) return '';
    return this.formatFullAddress(this.primaryAddress);
  }
}