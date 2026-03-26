import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, OnChanges, SimpleChanges, ChangeDetectorRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, NgForm } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { Subject, Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, takeUntil, finalize, map, catchError } from 'rxjs/operators';
import {
  OrganizationData,
  OrgSearchResult,
  OrgbookResponse,
  OrgbookOrganization,
} from '../../../shared/models/applicant-info.interface';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ToastService } from '../../../shared/services/toast.service';
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
  @ViewChild('orgForm') orgForm!: NgForm;
  @Input() pluginId: string = '';
  @Input() provider: string = '';
  @Output() organizationLoaded = new EventEmitter<{ orgNumber: string; orgName: string } | null>();
  @Output() multipleOrganizationsDetected = new EventEmitter<boolean>();
  
  // Internal state
  organizationInfo: OrganizationData | null = null;
  orgbookResponse: OrgbookResponse | null = null; // Keep to store the response
  isLoading = false;
  isSaving = false;

  // Multiple organizations handling
  multipleOrganizations: OrgbookOrganization[] = [];
  showMultipleOrgsTable = false;
  showSingleOrgForm = false;
  orgbookDataTableConfig!: DatatableConfig;

  // Search properties
  searchTerm = '';
  searchResults: OrgSearchResult[] = [];
  showDropdown = false;
  isSearching = false;

  // Form properties
  selectedFiscalMonth = '';
  selectedFiscalDay = '';

  // Form submitted flag for showing validation
  formSubmitted = false;

  // Edit mode properties
  isEditMode = false;
  private backupOrganizationInfo: OrganizationData | null = null;
  private backupFiscalMonth = '';
  private backupFiscalDay = '';
  private backupSearchTerm = '';

  // Constants
  readonly daysArray = Array.from({ length: 31 }, (_, i) => i + 1);
  private readonly monthAbbreviations = ['', 'Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
  private readonly orgbookBaseApi = 'https://orgbook.gov.bc.ca/api';
  private readonly searchSubject = new Subject<string>();
  private readonly destroy$ = new Subject<void>();

  constructor(
    private readonly cdr: ChangeDetectorRef,
    private readonly http: HttpClient,
    private readonly applicantInfoService: ApplicantInfoService,
    private readonly toastService: ToastService
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
      this.showSingleOrgForm = false;
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
      this.showSingleOrgForm = false;
      this.multipleOrganizations = organizations;
      this.organizationInfo = null;
      this.isLoading = false;
      this.multipleOrganizationsDetected.emit(true);
      this.organizationLoaded.emit(null);
    } else if (organizations.length === 1) {
      this.showMultipleOrgsTable = false;
      this.showSingleOrgForm = true;
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
      this.multipleOrganizationsDetected.emit(false);
      this.organizationLoaded.emit({
        orgNumber: this.organizationInfo?.orgNumber ?? '',
        orgName: this.organizationInfo?.orgName ?? ''
      });
    } else {
      this.showMultipleOrgsTable = false;
      this.showSingleOrgForm = false;
      this.multipleOrganizations = [];
      this.organizationInfo = null;
      this.isLoading = false;
      this.multipleOrganizationsDetected.emit(false);
      this.organizationLoaded.emit(null);
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
      if (this.organizationInfo.fiscalYearEndMonth != null) {
        this.selectedFiscalMonth = this.monthAbbreviations[this.organizationInfo.fiscalYearEndMonth] ?? '';
      } else {
        this.selectedFiscalMonth = this.organizationInfo.fiscalMonth ?? '';
      }
      this.selectedFiscalDay =
        this.organizationInfo.fiscalYearEndDay != null
          ? String(this.organizationInfo.fiscalYearEndDay)
          : (this.organizationInfo.fiscalDay != null
              ? String(this.organizationInfo.fiscalDay)
              : '');
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
    const url = `${this.orgbookBaseApi}/v3/search/autocomplete?q=${encodeURIComponent(term)}&revoked=false&inactive=`;
    return this.http.get<any>(url).pipe(
      map((response) => this.mapAutocompleteResults(response)),
      catchError(() => of([]))
    );
  }

  private mapAutocompleteResults(response: any): OrgSearchResult[] {
    if (!response?.results) return [];
    const seen = new Set<string>();
    return response.results
      .filter((r: any) => {
        const id = r.topic_source_id;
        if (!id || seen.has(id)) return false;
        seen.add(id);
        return true;
      })
      .map((r: any) => ({
        id: r.topic_source_id ?? '',
        orgName: r.value ?? '',
        orgNumber: r.topic_source_id ?? '',
        orgStatus: '',
        organizationType: r.topic_type ?? '',
      }));
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
    this.showDropdown = false;
    this.searchTerm = result.orgName;
    this.fetchOrgDetails(result.orgNumber);
  }

  private fetchOrgDetails(orgBookId: string): void {
    if (!orgBookId) return;
    const queryParams = `q=${encodeURIComponent(orgBookId)}&inactive=any&latest=any&revoked=any&ordering=-score`;
    const url = `${this.orgbookBaseApi}/v4/search/topic?${queryParams}`;
    this.isLoading = true;
    this.http.get<any>(url).pipe(
      takeUntil(this.destroy$),
      finalize(() => {
        this.isLoading = false;
        this.cdr.detectChanges();
      })
    ).subscribe({
      next: (response) => {
        const firstResult = response?.results?.[0];
        if (firstResult) {
          this.populateFromOrgbookTopic(firstResult);
        }
      },
      error: (err) => {
        console.error('Failed to fetch OrgBook details:', err);
      }
    });
  }

  private populateFromOrgbookTopic(topic: any): void {
    const orgName = topic.names?.[0]?.text ?? '';
    const orgNumber = topic.source_id ?? '';
    const inactive = topic.inactive ?? false;
    const entityType = topic.attributes?.find((a: any) => a.type === 'entity_type')?.value ?? '';

    const base = this.organizationInfo ?? {} as OrganizationData;
    this.organizationInfo = {
      ...base,
      orgName,
      orgNumber,
      orgStatus: inactive ? 'Inactive' : 'Active',
      organizationType: entityType,
      legalName: orgName,
    };
    this.searchTerm = orgName;
    this.cdr.detectChanges();
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
    this.backupSearchTerm = this.searchTerm;
  }

  onSave(): void {
    if (this.isSaving || !this.organizationInfo) return;

    this.formSubmitted = true;
    if (this.orgForm?.invalid) return;
    
    // Update organization info with fiscal year data — send null when the placeholder option is selected
    const updatedOrgInfo: OrganizationData = {
      ...this.organizationInfo,
      fiscalMonth: this.selectedFiscalMonth || null,
      fiscalDay: this.selectedFiscalDay ? Number(this.selectedFiscalDay) : null,
      fiscalYearEndMonth: this.selectedFiscalMonth ? this.monthAbbreviations.indexOf(this.selectedFiscalMonth) : null,
      fiscalYearEndDay: this.selectedFiscalDay ? parseInt(this.selectedFiscalDay) : null
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
          this.searchTerm = '';
          this.resetSearch();
          this.formSubmitted = false;
          this.toastService.success('Organization information saved successfully.');
        },
        error: (error) => {
          console.error('Failed to save organization:', error);
          const message = this.extractErrorMessage(error);
          this.toastService.error(message);
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
    this.searchTerm = this.backupSearchTerm;
    this.resetSearch();
    this.formSubmitted = false;
  }

  private resetSearch(): void {
    this.searchResults = [];
    this.showDropdown = false;
    this.isSearching = false;
  }

  private extractErrorMessage(error: any): string {
    const body = error?.error;
    if (body?.errors) {
      const messages = Object.values(body.errors)
        .flat()
        .filter((m): m is string => typeof m === 'string');
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }
    if (body?.message) {
      return body.message;
    }
    return 'An unexpected error occurred while saving. Please try again.';
  }
}