import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';

import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ApplicantInfo } from '../../../shared/models/applicant.interface';
import { Address } from '../../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { 
  DatatableConfig,
  DatatableActionEvent,
  DatatableRowClickEvent,
  DatatableSortEvent
} from '../../../shared/components/datatable/datatable.models';

interface AddressDisplay {
  id: string;
  addressId?: string;
  type: string;
  addressLine1?: string;
  addressLine2?: string;
  street: string;
  city: string;
  province: string;
  state: string;
  postalCode: string;
  zipCode: string;
  country?: string;
  isPrimary?: boolean;
  isActive?: boolean;
  lastVerified?: string;
  allowEdit?: boolean;
  fullAddress?: string;
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

  // Datatable configuration
  addressesTableConfig: DatatableConfig = {
    tableId: 'addresses-table',
    defaultSortField: 'lastVerified',
    enableSortPersistence: true,
    columns: [
      { key: 'type', label: 'Type', sortable: true, type: 'badge', cssClass: 'type-column' },
      { key: 'addressId', label: 'Address ID', sortable: true, cssClass: 'address-id-column' },
      { key: 'fullAddress', label: 'Address', sortable: true, cssClass: 'address-column' },
      { key: 'city', label: 'City', sortable: true, cssClass: 'city-column' },
      { key: 'province', label: 'Province', sortable: true, cssClass: 'province-column' },
      { key: 'postalCode', label: 'Postal Code', sortable: true, cssClass: 'postal-code-column' },
      { key: 'isPrimary', label: 'Primary', sortable: true, type: 'boolean', cssClass: 'primary-column' },
      { key: 'isActive', label: 'Active', sortable: true, type: 'boolean', cssClass: 'active-column' }
    ],
    actionsType: 'dropdown',
    actionItems: [
      { label: 'Set as primary', icon: 'fa-home', action: 'setAsPrimary' },
    ],
    badgeConfig: {
      field: 'type',
      badgeClassPrefix: 'address-type-badge',
      badgeClasses: {
        'Physical': 'address-type-physical',
        'Mailing': 'address-type-mailing',
        'Primary': 'address-type-primary',
        'Billing': 'address-type-billing',
        'Office': 'address-type-office'
      },
      fallbackClass: 'address-type-other'
    },
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
    return addresses.map(addr => ({
      id: addr.id || `address-${Math.random()}`,
      addressId: addr.addressId,
      type: addr.type || 'Unknown',
      street: addr.street || '',
      city: addr.city || '',
      province: addr.province || addr.state || '',
      state: addr.state || addr.province || '',
      postalCode: addr.postalCode || addr.zipCode || '',
      zipCode: addr.zipCode || addr.postalCode || '',
      country: addr.country || '',
      isPrimary: addr.isPrimary || false,
      isActive: addr.isActive !== false,
      lastVerified: addr.lastVerified,
      allowEdit: addr.allowEdit !== false,
      fullAddress: `${addr.street || ''}, ${addr.city || ''}, ${addr.state || addr.province || ''} ${addr.postalCode || addr.zipCode || ''}`.trim()
    }));
  }

  // Datatable event handlers
  onAddressRowClick(event: DatatableRowClickEvent): void {
    console.log('Clicked address:', event.row);
    // TODO: Navigate to address detail view
  }

  onAddressAction(event: DatatableActionEvent): void {
    switch (event.action) {
      case 'setAsPrimary':
        this.onSetAsPrimary(event.row);
        break;
      default:
        console.log('Unknown action:', event.action);
    }
  }

  onSetAsPrimary(address: AddressDisplay): void {
    console.log('Setting as primary address...', address);
    
    // Make API call to set address as primary
    const addressData = {
      addressLine1: address.addressLine1,
      addressLine2: address.addressLine2,
      city: address.city,
      province: address.province,
      postalCode: address.postalCode,
      country: address.country,
      type: address.type,
      isPrimary: true
    };

    this.applicantInfoService.setAddressAsPrimary(
      address.id,
      this.pluginId,
      this.provider
    )
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (response) => {
        console.log('Address set as primary successfully:', response);
        
        // Update local state after successful API call
        this.addresses = this.addresses.map(addr => ({
          ...addr,
          isPrimary: addr.id === address.id
        }));
        
        this.primaryAddress = { ...address, isPrimary: true };
        console.log('Primary address updated locally:', this.primaryAddress);
      },
      error: (error) => {
        console.error('Failed to set address as primary:', error);
        // Optionally show an error message to the user
      }
    });
  }

  onAddressSort(event: DatatableSortEvent): void {
    console.log('Addresses sorted by:', event.column, event.direction);
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }

  private formatFullAddress(address: AddressDisplay): string {
    const parts = [];
    if (address.addressLine1) parts.push(address.addressLine1);
    if (address.addressLine2) parts.push(address.addressLine2);
    if (parts.length === 0 && address.street) parts.push(address.street);
    return parts.join(', ') || address.fullAddress || '';
  }

  // Helper method to format primary address display
  getPrimaryFullAddress(): string {
    if (!this.primaryAddress) return '';
    return this.formatFullAddress(this.primaryAddress);
  }
}