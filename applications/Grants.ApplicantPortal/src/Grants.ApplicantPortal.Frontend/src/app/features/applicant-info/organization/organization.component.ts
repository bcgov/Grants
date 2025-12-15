import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import {
  OrganizationData,
  OrgSearchResult,
} from '../../../shared/models/applicant-info.interface';
import { LoadingOverlayComponent } from '../../../shared/components/loading-overlay/loading-overlay.component';

@Component({
  selector: 'app-organization-info',
  standalone: true,
  imports: [CommonModule, FormsModule, LoadingOverlayComponent],
  templateUrl: './organization.component.html',
  styleUrls: ['./organization.component.scss'],
})
export class OrganizationInfoComponent {
  @Input() organizationInfo: OrganizationData | null = null;
  @Input() isLoading = false;
  @Input() isSaving = false;
  @Output() saveOrganization = new EventEmitter<OrganizationData>();
  @Output() searchResultSelected = new EventEmitter<OrgSearchResult>();

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

  constructor() {
    this.setupSearch();
  }

  ngOnInit(): void {
    this.updateFiscalFieldsFromOrganizationInfo();
  }

  ngOnChanges(): void {
    this.updateFiscalFieldsFromOrganizationInfo();
  }

  private updateFiscalFieldsFromOrganizationInfo(): void {
    if (this.organizationInfo) {
      this.selectedFiscalMonth = this.organizationInfo.fiscalMonth || '';
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
    this.searchTerm = result.orgName;
    this.showDropdown = false;
    this.searchResultSelected.emit(result);
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
    
    this.isEditMode = false;
    this.saveOrganization.emit(updatedOrgInfo);
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