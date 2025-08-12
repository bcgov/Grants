import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Observable, of } from 'rxjs';
import {
  debounceTime,
  distinctUntilChanged,
  switchMap,
  takeUntil,
} from 'rxjs/operators';

import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ApplicantInfo } from '../../../shared/models/applicant.interface';
import { SubmissionsComponent } from './submissions.component';
import {
  OrganizationData,
  OrgSearchResult,
} from '../../../shared/models/applicant-info.interface';
import {
  Key,
  PluginId,
  ProfileId,
  Provider,
} from '../../../shared/models/applicantion-info.enums';

@Component({
  selector: 'app-applicant-info',
  standalone: true,
  imports: [CommonModule, FormsModule, SubmissionsComponent],
  templateUrl: './applicant-info.component.html',
  styleUrls: ['./applicant-info.component.scss'],
})
export class ApplicantInfoComponent implements OnInit, OnDestroy {
  // Profile properties
  profileId = ProfileId.DEFAULT;
  pluginId = PluginId.DEMO;
  provider = Provider.DEMO;
  keyOrgInfo = Key.ORGINFO;
  keySubmissions = Key.SUBMISSIONS;

  // Data properties
  applicantInfo: ApplicantInfo | null = null;
  organizationInfo: OrganizationData | null = null;

  // Search properties
  searchTerm = '';
  searchResults: OrgSearchResult[] = [];
  showDropdown = false;
  isSearching = false;

  // Form properties
  selectedFiscalMonth = '';
  selectedFiscalDay = '';

  isLoading = true;
  isHydratingOrgInfo = false;

  // Constants
  readonly monthOptions = [
    { value: 'Jan', label: 'January' },
    { value: 'Feb', label: 'February' },
    { value: 'Mar', label: 'March' },
    { value: 'Apr', label: 'April' },
    { value: 'May', label: 'May' },
    { value: 'Jun', label: 'June' },
    { value: 'Jul', label: 'July' },
    { value: 'Aug', label: 'August' },
    { value: 'Sep', label: 'September' },
    { value: 'Oct', label: 'October' },
    { value: 'Nov', label: 'November' },
    { value: 'Dec', label: 'December' },
  ];

  readonly daysArray = Array.from({ length: 31 }, (_, i) => i + 1);

  // Subjects for cleanup and search
  private readonly destroy$ = new Subject<void>();
  private readonly searchSubject = new Subject<string>();

  constructor(
    private readonly applicantService: ApplicantService,
    private readonly applicantInfoService: ApplicantInfoService
  ) {
    this.setupSearch();
  }

  ngOnInit(): void {
    this.loadData();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.loadApplicantInfo();
    this.loadOrganizationInfo();
  }

  private loadApplicantInfo(): void {
    this.applicantService
      .getApplicantInfo()
      .pipe(takeUntil(this.destroy$))
      .subscribe((data) => (this.applicantInfo = data));
  }

  private loadOrganizationInfo(): void {
    this.isHydratingOrgInfo = true;

    this.applicantInfoService
      .hydrateAndGetOrganizationInfo(
        ProfileId.DEFAULT,
        PluginId.DEMO,
        Provider.DEMO,
        Key.ORGINFO,
        {}
      )
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isHydratingOrgInfo = false;
          this.organizationInfo = result.organizationData;
          console.log('Organization data loaded:', this.organizationInfo);

          this.selectedFiscalMonth = this.organizationInfo?.fiscalMonth || '';
          this.selectedFiscalDay =
            this.organizationInfo?.fiscalDay != null
              ? String(this.organizationInfo?.fiscalDay)
              : '';
          this.isLoading = false;
        },
        error: (error) => {
          this.isHydratingOrgInfo = false;
          console.error('Hydration failed:', error);
        },
      });
  }

  // === Search Methods ===
  private setupSearch(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => this.performSearch(term)),
        takeUntil(this.destroy$)
      )
      .subscribe((results) => {
        this.searchResults = results;
        this.showDropdown = this.searchTerm.length >= 3;
        this.isSearching = false;
      });
  }

  private performSearch(term: string): Observable<OrgSearchResult[]> {
    if (term.length < 3) return of([]);

    // Replace with actual API call
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

  // === Event Handlers ===
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
    this.updateOrganizationFromSearch(result);
    this.searchTerm = result.orgName;
    this.showDropdown = false;
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

  saveOrganization(): void {
    console.log('Saving organization...', this.organizationInfo);
    // Implement save logic
  }

  // === Helper Methods ===
  private resetSearch(): void {
    this.searchResults = [];
    this.showDropdown = false;
    this.isSearching = false;
  }

  private updateOrganizationFromSearch(result: OrgSearchResult): void {
    if (!this.organizationInfo) return;

    this.organizationInfo = {
      ...this.organizationInfo,
      orgName: result.orgName,
      orgNumber: result.orgNumber,
      orgStatus: result.orgStatus,
      organizationType: result.organizationType,
    };
  }
}
