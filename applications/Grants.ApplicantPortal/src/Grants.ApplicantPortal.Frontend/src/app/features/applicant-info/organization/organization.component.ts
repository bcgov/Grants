import { Component, Input, OnInit, OnDestroy, OnChanges, SimpleChanges, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject, Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, takeUntil, finalize } from 'rxjs/operators';
import {
  OrganizationData,
  OrgSearchResult,
  OrgbookResponse,
  OrgbookOrganization,
} from '../../../shared/models/applicant-info.interface';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { LoadingOverlayComponent } from '../../../shared/components/loading-overlay/loading-overlay.component';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { DatatableConfig } from '../../../shared/components/datatable/datatable.models';

@Component({
  selector: 'app-organization-info',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingOverlayComponent, DatatableComponent],
  templateUrl: './organization.component.html',
  styleUrls: ['./organization.component.scss'],
})
export class OrganizationInfoComponent implements OnInit, OnDestroy, OnChanges {
  @Input() pluginId: string = '';
  @Input() provider: string = '';
  
  // Internal state
  organizationInfo: OrganizationData | null = null;
  orgbookResponse: OrgbookResponse | null = null; // Keep to store the response
  isLoading = false;
  isSaving = false;

  // Multiple organizations handling
  multipleOrganizations: OrgbookOrganization[] = [];
  showMultipleOrgsTable = false;
  orgbookDataTableConfig!: DatatableConfig;

  // Search properties
  searchTerm = '';
  searchResults: OrgSearchResult[] = [];
  showDropdown = false;
  isSearching = false;

  // Form properties
  selectedFiscalMonth = '';
  selectedFiscalDay = '';

  // Edit mode properties
  isEditMode = false;
  private backupOrganizationInfo: OrganizationData | null = null;
  private backupFiscalMonth = '';
  private backupFiscalDay = '';

  // Constants
  readonly daysArray = Array.from({ length: 31 }, (_, i) => i + 1);
  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly cdr: ChangeDetectorRef,
    private readonly http: HttpClient,
    private readonly applicantInfoService: ApplicantInfoService
  ) {
    this.setupSearch();
    this.initializeDataTableConfig();
  }

  ngOnInit(): void {
    this.updateFiscalFieldsFromOrganizationInfo();
    // Data loading will be handled by ngOnChanges when pluginId/provider are set
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('OrganizationComponent ngOnChanges called:', changes);
    
    // If pluginId or provider changed, reload data
    if (changes['pluginId'] || changes['provider']) {
      const hasPluginId = this.pluginId && this.pluginId.trim() !== '';
      const hasProvider = this.provider && this.provider.trim() !== '';
      
      if (hasPluginId && hasProvider) {
        console.log('pluginId or provider changed, reloading data');
        this.loadOrganizationData();
      }
    }
  }

  private loadOrganizationData(): void {
    if (!this.pluginId || !this.provider) {
      console.log('No pluginId or provider, skipping organization data load');
      this.showMultipleOrgsTable = false;
      this.multipleOrganizations = [];
      this.organizationInfo = null;
      return;
    }

    console.log('Loading organization data for pluginId:', this.pluginId, 'provider:', this.provider);
    this.isLoading = true;
    
    this.applicantInfoService.getOrganizationInfo(this.pluginId, this.provider)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          console.log('Organization data received:', result);
          this.handleOrganizationResponse(result);
        },
        error: (error) => {
          console.error('Failed to load organization data:', error);
          this.isLoading = false;
        }
      });
  }

  private handleOrganizationResponse(result: any): void {
    const organizations = result.organizationsData ?? [];
    console.log(`Found ${organizations.length} organizations:`, organizations);
    
    if (organizations.length > 1) {
      this.showMultipleOrgsTable = true;
      this.multipleOrganizations = organizations;
      this.organizationInfo = null;
      this.isLoading = false;
    } else if (organizations.length === 1) {
      this.showMultipleOrgsTable = false;
      this.convertOrgbookToOrganizationData(organizations[0]);

      // Merge saved data on top of the orgbook data
      if (result.organizationData) {
        this.organizationInfo = {
          ...this.organizationInfo,
          ...result.organizationData
        };
        this.updateFiscalFieldsFromOrganizationInfo();
      }

      this.isLoading = false;
    } else {
      this.showMultipleOrgsTable = false;
      this.multipleOrganizations = [];
      this.organizationInfo = null;
      this.isLoading = false;
    }
    
    this.cdr.detectChanges();
  }

  private initializeDataTableConfig(): void {
    this.orgbookDataTableConfig = {
      columns: [
        { key: 'orgName', label: 'Organization Name', sortable: true, type: 'text' },
        { key: 'organizationType', label: 'Type', sortable: true, type: 'text' },
        { key: 'orgNumber', label: 'Registration Number', sortable: true, type: 'text' },
        { key: 'orgStatus', label: 'Status', sortable: true, type: 'badge' },
        { key: 'organizationSize', label: 'Organization Size', sortable: true, type: 'text' },
        { key: 'sector', label: 'Sector', sortable: true, type: 'text' },
        { key: 'subSector', label: 'Sub Sector', sortable: true, type: 'text' }
      ],
      badgeConfig: {
        field: 'orgStatus',
        badgeClassPrefix: 'status-badge',
        badgeClasses: {
          'ACTIVE': 'status-active',
          'INACTIVE': 'status-inactive',
          'SUSPENDED': 'status-suspended'
        },
        fallbackClass: 'status-unknown'
      },
      actionsType: 'none',
      rowClickable: false,
      tableId: 'orgbook-organizations',      
      noDataMessage: 'No organizations found.',
      tableClass: 'orgbook-table'
    };
  }

  private convertOrgbookToOrganizationData(orgbookOrg: OrgbookOrganization): void {
    console.log('Converting orgbook org to OrganizationData:', orgbookOrg);
    
    // Convert OrgbookOrganization to OrganizationData format
    // Handle organizationSize which can be either string or number
    const orgSize = orgbookOrg.organizationSize !== null && orgbookOrg.organizationSize !== undefined 
      ? String(orgbookOrg.organizationSize) 
      : '';
    
    this.organizationInfo = {
      orgName: orgbookOrg.orgName ?? '',
      orgNumber: orgbookOrg.orgNumber ?? '',
      orgStatus: orgbookOrg.orgStatus ?? '',
      organizationType: orgbookOrg.organizationType ?? '',
      nonRegOrgName: orgbookOrg.nonRegOrgName ?? '',
      orgSize: orgSize,
      fiscalMonth: orgbookOrg.fiscalMonth ?? '',
      fiscalDay: orgbookOrg.fiscalDay ?? 0,
      organizationId: orgbookOrg.id,
      // Set default/empty values for required OrganizationData fields not in orgbook
      legalName: orgbookOrg.orgName ?? orgbookOrg.nonRegOrgName ?? '',
      doingBusinessAs: '',
      ein: '',
      founded: 0,
      address: {} as any, // You may need to handle this based on your requirements
      contactInfo: {} as any, // You may need to handle this based on your requirements
      mission: '',
      servicesAreas: [],
      certifications: [],
      allowEdit: true
    };
    
    console.log('Converted organizationInfo:', this.organizationInfo);
    
    this.updateFiscalFieldsFromOrganizationInfo();
  }

  private updateFiscalFieldsFromOrganizationInfo(): void {
    if (this.organizationInfo) {
      this.selectedFiscalMonth = this.organizationInfo.fiscalMonth ?? '';
      this.selectedFiscalDay =
        this.organizationInfo.fiscalDay != null
          ? String(this.organizationInfo.fiscalDay)
          : '';
    }
  }

  private setupSearch(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => this.performSearch(term))
      )
      .subscribe((results) => {
        this.searchResults = results;
        this.showDropdown = this.searchTerm.length >= 3;
        this.isSearching = false;
      });
  }

  private performSearch(term: string): Observable<OrgSearchResult[]> {
    if (term.length < 3) return of([]);
    return of(this.getMockSearchResults(term));
  }

  private getMockSearchResults(term: string): OrgSearchResult[] {
    const mockOrgs: OrgSearchResult[] = [
      {
        id: '1',
        orgName: 'ABC Technology Corp',
        orgNumber: 'BC1234567',
        orgStatus: 'Active',
        organizationType: 'Corporation',
      },
      {
        id: '2',
        orgName: 'ABC Solutions Ltd',
        orgNumber: 'BC2345678',
        orgStatus: 'Active',
        organizationType: 'Limited Company',
      },
      {
        id: '3',
        orgName: 'Advanced Business Consulting',
        orgNumber: 'BC3456789',
        orgStatus: 'Active',
        organizationType: 'Partnership',
      },
      {
        id: '4',
        orgName: 'Alpha Beta Communications',
        orgNumber: 'BC4567890',
        orgStatus: 'Inactive',
        organizationType: 'Corporation',
      },
    ];

    return mockOrgs.filter((org) =>
      org.orgName.toLowerCase().includes(term.toLowerCase())
    );
  }

  // Event Handlers
  onSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm = target.value;

    if (this.searchTerm.length >= 3) {
      this.isSearching = true;
      this.showDropdown = true;
      this.searchSubject.next(this.searchTerm);
    } else {
      this.resetSearch();
    }
  }

  onSearchBlur(): void {
    setTimeout(() => (this.showDropdown = false), 200);
  }

  onSearchFocus(): void {
    if (this.searchTerm.length >= 3) {
      this.showDropdown = true;
    }
  }

  onSearchResultSelect(result: OrgSearchResult): void {
    console.log('Search result selected:', result);
    // For now, just log the selected result
    // This could be used to populate form fields or trigger additional actions
    
    // Hide the dropdown
    this.showDropdown = false;
    this.searchTerm = result.orgName;
  }

  onFiscalMonthChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedFiscalMonth = target.value;
    if (this.organizationInfo) {
      this.organizationInfo.fiscalMonth = this.selectedFiscalMonth;
    }
  }

  onFiscalDayChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedFiscalDay = target.value;
    if (this.organizationInfo) {
      this.organizationInfo.fiscalDay = Number(this.selectedFiscalDay);
    }
  }

  onEdit(): void {
    this.isEditMode = true;
    // Create backup for cancel functionality
    this.backupOrganizationInfo = this.organizationInfo ? { ...this.organizationInfo } : null;
    this.backupFiscalMonth = this.selectedFiscalMonth;
    this.backupFiscalDay = this.selectedFiscalDay;
  }

  onSave(): void {
    if (this.isSaving || !this.organizationInfo) return;
    
    // Update organization info with fiscal year data
    const updatedOrgInfo: OrganizationData = {
      ...this.organizationInfo,
      fiscalYearEndMonth: this.selectedFiscalMonth ? parseInt(this.selectedFiscalMonth) : this.organizationInfo.fiscalYearEndMonth,
      fiscalYearEndDay: this.selectedFiscalDay ? parseInt(this.selectedFiscalDay) : this.organizationInfo.fiscalYearEndDay
    };
    
    console.log('Saving organization...', updatedOrgInfo);
    
    this.isSaving = true;
    
    this.applicantInfoService.saveOrganizationInfo(
      updatedOrgInfo,
      this.pluginId,
      this.provider
    )
      .pipe(
        takeUntil(this.destroy$),
        finalize(() => {
          this.isSaving = false;
        })
      )
      .subscribe({
        next: (response) => {
          console.log('Organization saved successfully:', response);
          this.isEditMode = false;
          this.organizationInfo = updatedOrgInfo; // Update local state
        },
        error: (error) => {
          console.error('Failed to save organization:', error);
          // Could add user-visible error handling here if needed
        }
      });
  }

  onCancel(): void {
    if (this.isSaving) return;
    
    this.isEditMode = false;
    // Restore from backup
    if (this.backupOrganizationInfo) {
      this.organizationInfo = { ...this.backupOrganizationInfo };
    }
    this.selectedFiscalMonth = this.backupFiscalMonth;
    this.selectedFiscalDay = this.backupFiscalDay;
    this.updateFiscalFieldsFromOrganizationInfo();
  }

  private resetSearch(): void {
    this.searchResults = [];
    this.showDropdown = false;
    this.isSearching = false;
  }
}