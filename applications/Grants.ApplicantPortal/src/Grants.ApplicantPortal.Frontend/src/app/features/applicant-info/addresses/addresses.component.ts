import { Component, OnInit, OnDestroy, Input } from '@angular/core';
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

interface Address {
  id: string;
  type: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  province: string;
  postalCode: string;
  country?: string;
  isPrimary?: boolean;
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
export class AddressesComponent implements OnInit, OnDestroy {
  @Input() profileId!: string;
  @Input() pluginId!: string;
  @Input() provider!: string;

  applicantInfo: ApplicantInfo | null = null;
  addresses: Address[] = [];
  primaryAddress: Address | null = null;

  isLoading = true;
  isHydratingAddresses = false;
  error: string | null = null;

  // Datatable configuration
  addressesTableConfig: DatatableConfig = {
    tableId: 'addresses-table',
    defaultSortField: 'lastUpdated',
    enableSortPersistence: true,
    columns: [
      { key: 'type', label: 'Type', sortable: true, type: 'badge', cssClass: 'type-column' },
      { key: 'fullAddress', label: 'Address', sortable: true, cssClass: 'address-column' },
      { key: 'city', label: 'City', sortable: true, cssClass: 'city-column' },
      { key: 'province', label: 'Province', sortable: true, cssClass: 'province-column' },
      { key: 'postalCode', label: 'Postal Code', sortable: true, cssClass: 'postal-code-column' },
      { key: 'isPrimary', label: 'Primary', sortable: true, type: 'boolean', cssClass: 'primary-column' }
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

  // Subjects for cleanup
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly applicantService: ApplicantService,
    private readonly applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    if (this.profileId && this.pluginId && this.provider) {
      this.loadData();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.loadApplicantInfo();
    this.loadAddresses();
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
    this.isHydratingAddresses = true;
    this.error = null;

    this.applicantInfoService
      .getAddressesInfo(
        this.profileId,
        this.pluginId,
        this.provider
      )
      .pipe(take(1), takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isHydratingAddresses = false;
          // Parse the result based on the actual API response structure
          this.addresses = Array.isArray(result.jsonData) 
            ? result.jsonData 
            : JSON.parse(result.jsonData)?.data?.addresses || [];
          
          this.primaryAddress = this.addresses.find(a => a.isPrimary) || this.addresses[0] || null;
          console.log('Addresses data loaded:', this.addresses);
          this.isLoading = false;
        },
        error: (error) => {
          this.isHydratingAddresses = false;
          this.error = 'Failed to load addresses data';
          this.isLoading = false;
          console.error('Error loading addresses:', error);
        },
      });
  }

  // Event handlers
  onAddressClick(address: Address): void {
    console.log('Clicked address:', address);
    // TODO: Navigate to address detail view
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

  onSetAsPrimary(address: Address): void {
    console.log('Setting as primary address...', address);
    
    this.applicantInfoService.setAddressAsPrimary(
      address.id,
      this.profileId,
      this.pluginId,
      this.provider
    ).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        console.log('Address set as primary successfully:', response);
        // Refresh the addresses list to update the display
        this.loadAddresses();
      },
      error: (error) => {
        console.error('Failed to set address as primary:', error);
        // Could add a toast notification here
        this.error = 'Failed to set address as primary. Please try again.';
      }
    });
  }

  onAddressSort(event: DatatableSortEvent): void {
    console.log('Addresses sorted by:', event.column, event.direction);
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }

  // Helper method to format addresses for datatable
  getAddressesForTable(): any[] {
    return this.addresses.map(address => ({
      ...address,
      fullAddress: this.formatFullAddress(address)
    }));
  }

  private formatFullAddress(address: Address): string {
    const parts = [address.addressLine1];
    if (address.addressLine2) {
      parts.push(address.addressLine2);
    }
    return parts.join(', ');
  }

  // Helper method to format primary address display
  getPrimaryFullAddress(): string {
    if (!this.primaryAddress) return '';
    return this.formatFullAddress(this.primaryAddress);
  }
}