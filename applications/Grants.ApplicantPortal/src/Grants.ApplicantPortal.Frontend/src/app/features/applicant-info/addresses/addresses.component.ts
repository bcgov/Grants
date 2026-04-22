import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';

import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ToastService } from '../../../shared/services/toast.service';
import { ApplicantInfo } from '../../../shared/models/applicant.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { 
  DatatableConfig,
  DatatableActionEvent,
  DatatableRowClickEvent,
  DatatableSortEvent
} from '../../../shared/components/datatable/datatable.models';
import { TooltipDirective } from '../../../shared/directives/tooltip.directive';

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
  disabledTooltip: string;
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
  primaryAddress: AddressDisplay | null = null;

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
    addressType: 'Physical',
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
  addressTypes: { key: string; label: string }[] = [];

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
      { label: 'Set as primary', icon: 'fa-home', action: 'setAsPrimary' },
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
      next: (response: any) => {
        this.addressTypes = response?.types ?? [];
      },
      error: (error) => {
        console.error('Failed to load address types:', error);
        // Fallback to hardcoded types if API not available
        this.addressTypes = [
          { key: 'Physical', label: 'Physical' },
          { key: 'Mailing', label: 'Mailing' }
        ];
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
          this.primaryAddress = this.addresses.find(addr => addr.isPrimary) || null;
          this.updateActionsVisibility();
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
        disabledTooltip: this.getDisabledTooltip(addr),
        referenceNo: addr.referenceNo ?? '',
        fullAddress: addressParts
      };
    });
  }

  private getDisabledTooltip(address: any): string {
    if (address.isEditable) {
      return '';
    }
    if (this.hasMultipleOrgs) {
      return 'Multiple organization records found — please contact support to consolidate before editing addresses';
    }
    return 'This address is linked to a submission. Contact the grant program administrator to update it';
  }

  /**
   * Uses the server-returned primaryAddressId to set the primary flag
   * on all addresses and update the primaryAddress reference.
   */
  private applyPrimaryFromResponse(primaryAddressId: string | null | undefined): void {
    if (primaryAddressId == null) {
      return;
    }

    const normalizedPrimaryId = primaryAddressId.toLowerCase();

    this.addresses = this.addresses.map(a => ({
      ...a,
      isPrimary: a.id.toLowerCase() === normalizedPrimaryId
    }));
    this.primaryAddress = this.addresses.find(a => a.isPrimary) ?? null;
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

    const payload: any = {
      ...(this.isEditMode && this.editingAddressId ? { addressId: this.editingAddressId } : {}),
      applicantId: this.applicantId,
      addressType: this.newAddress.addressType ?? 'Physical',
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

        const responseId = response?.addressId ?? response?.id;

        if (!this.isEditMode && !responseId) {
          this.saveAddressError = 'Address was saved but the server did not return a valid address ID. Please refresh and try again.';
          return;
        }

        const addressId = this.isEditMode ? this.editingAddressId! : responseId;
        const street = payload.street;
        const street2 = payload.street2;
        const unit = payload.unit;

        const effectiveIsEditable = response?.isEditable ?? true;

        const savedAddress: AddressDisplay = {
          id: addressId,
          addressType: payload.addressType,
          street,
          street2,
          unit,
          city: payload.city,
          province: payload.province,
          postalCode: payload.postalCode,
          country: payload.country,
          isPrimary: addressId.toLowerCase() === response?.primaryAddressId?.toLowerCase(),
          isEditable: effectiveIsEditable,
          disabledTooltip: this.getDisabledTooltip({ ...response, isEditable: effectiveIsEditable }),
          referenceNo: response?.referenceNo ?? '',
          fullAddress: [street, street2, unit].filter(Boolean).join(', ')
        };

        if (this.isEditMode && this.editingAddressId) {
          this.addresses = this.addresses.map(a =>
            a.id === this.editingAddressId ? savedAddress : a
          );
        } else {
          this.addresses = [...this.addresses, savedAddress];
        }

        this.applyPrimaryFromResponse(response?.primaryAddressId);

        const addressLabel = [payload.street, payload.city].filter(Boolean).join(', ');
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
        addressType: 'Physical',
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

  // Datatable event handlers
  onAddressRowClick(event: DatatableRowClickEvent): void {
    // TODO: Navigate to address detail view
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
      next: (response: any) => {
        const deletedId = this.addressToDelete!.id;
        this.isDeletingAddress = false;
        this.showDeleteConfirmModal = false;
        this.addressToDelete = null;

        this.addresses = this.addresses.filter(a => a.id !== deletedId);

        this.applyPrimaryFromResponse(response?.primaryAddressId);

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
      next: (response: any) => {
        this.applyPrimaryFromResponse(response?.primaryAddressId);
        const addressLabel = [address.street, address.city].filter(Boolean).join(', ');
        this.toastService.success(`"${addressLabel}" has been set as the primary address.`);
      },
      error: (error: any) => {
        console.error('Failed to set address as primary:', error);
      }
    });
  }

  onAddressSort(event: DatatableSortEvent): void {
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }

  private formatFullAddress(address: AddressDisplay): string {
    const parts = [address.street, address.street2, address.unit].filter(Boolean);
    return parts.join(', ') || address.fullAddress || '';
  }

  getPrimaryFullAddress(): string {
    if (!this.primaryAddress) return '';
    return this.formatFullAddress(this.primaryAddress);
  }
}