import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApplicantService } from '../../../core/services/applicant.service';
import {
  ApplicantInfo,
  OrganizationInfo,
  Submission,
} from '../../../shared/models/applicant.model';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import { Subject, Observable, of } from 'rxjs';
import { SubmissionsComponent } from './submissions.component';

export interface OrgSearchResult {
  id: string;
  name: string;
  registeredNumber: string;
  status: string;
  type: string;
}

@Component({
  selector: 'app-applicant-info',
  standalone: true,
  imports: [CommonModule, FormsModule, SubmissionsComponent],
  templateUrl: './applicant-info.component.html',
  styleUrls: ['./applicant-info.component.scss'],
})
export class ApplicantInfoComponent implements OnInit {
  applicantInfo: ApplicantInfo | null = null;
  organizationInfo: OrganizationInfo | null = null;
  submissions: Submission[] = [];

  // Search functionality
  searchTerm: string = '';
  searchResults: OrgSearchResult[] = [];
  showDropdown: boolean = false;
  isSearching: boolean = false;
  private readonly searchSubject = new Subject<string>();

  selectedFiscalMonth: string = '';
  selectedFiscalDay: string = '';

  daysArray: number[] = Array.from({ length: 31 }, (_, i) => i + 1);

  // Month options array (optional - for dynamic rendering)
  monthOptions = [
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

  constructor(private readonly applicantService: ApplicantService) {}

  ngOnInit(): void {
    this.loadApplicantInfo();
    this.setupOrganizationSearch();
  }

  private loadApplicantInfo(): void {
    this.applicantService.getApplicantInfo().subscribe((data) => {
      console.log('Applicant Info:', data);
      this.applicantInfo = data;
    });

    this.applicantService.getOrganizationInfo().subscribe((data) => {
      console.log('Organization Info:', data);
      this.organizationInfo = data;

      this.selectedFiscalMonth = data?.fiscalYearEndMonth || '';
      this.selectedFiscalDay = data?.fiscalYearEndDay || '';
    });
  }

  private setupOrganizationSearch(): void {
    this.searchSubject
      .pipe(
        debounceTime(300),
        distinctUntilChanged(),
        switchMap((term) => this.searchOrganizations(term))
      )
      .subscribe((results) => {
        this.searchResults = results;
        this.showDropdown = this.searchTerm.length >= 3;
        this.isSearching = false;
      });
  }

  private searchOrganizations(term: string): Observable<OrgSearchResult[]> {
    if (term.length < 3) {
      return of([]);
    }

    // Mock search results - replace with actual API call
    return of(this.getMockSearchResults(term));
  }

  private getMockSearchResults(term: string): OrgSearchResult[] {
    // Mock data - replace with actual API call
    const mockOrgs: OrgSearchResult[] = [
      {
        id: '1',
        name: 'ABC Technology Corp',
        registeredNumber: 'BC1234567',
        status: 'Active',
        type: 'Corporation',
      },
      {
        id: '2',
        name: 'ABC Solutions Ltd',
        registeredNumber: 'BC2345678',
        status: 'Active',
        type: 'Limited Company',
      },
      {
        id: '3',
        name: 'Advanced Business Consulting',
        registeredNumber: 'BC3456789',
        status: 'Active',
        type: 'Partnership',
      },
      {
        id: '4',
        name: 'Alpha Beta Communications',
        registeredNumber: 'BC4567890',
        status: 'Inactive',
        type: 'Corporation',
      },
    ];

    return mockOrgs.filter((org) =>
      org.name.toLowerCase().includes(term.toLowerCase())
    );
  }

  onSearchInput(event: Event): void {
    const target = event.target as HTMLInputElement;
    this.searchTerm = target.value;

    if (this.searchTerm.length >= 3) {
      this.isSearching = true;
      this.showDropdown = true;
      this.searchSubject.next(this.searchTerm);
    } else {
      this.searchResults = [];
      this.showDropdown = false;
      this.isSearching = false;
    }
  }

  onSearchBlur(): void {
    setTimeout(() => {
      this.showDropdown = false;
    }, 200);
  }

  onSearchFocus(): void {
    if (this.searchTerm.length >= 3) {
      this.showDropdown = true;
    }
  }

  onSearchResultSelect(result: OrgSearchResult): void {
    this.organizationInfo = {
      ...this.organizationInfo,
      orgName: result.name,
      orgRegisteredNumber: result.registeredNumber,
      orgStatus: result.status,
      orgType: result.type,
      orgSize: this.organizationInfo?.orgSize || '',
      nonRegOrgName: this.organizationInfo?.nonRegOrgName || '',
      fiscalYearEndMonth: this.organizationInfo?.fiscalYearEndMonth,
      fiscalYearEndDay: this.organizationInfo?.fiscalYearEndDay,
    };

    // Update search term and hide dropdown
    this.searchTerm = result.name;
    this.showDropdown = false;
  }

  onFiscalMonthChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedFiscalMonth = target.value;
    if (this.organizationInfo) {
      this.organizationInfo.fiscalYearEndMonth = this.selectedFiscalMonth;
    }
    console.log('Selected Fiscal Month:', this.selectedFiscalMonth);
  }

  onFiscalDayChange(event: Event): void {
    const target = event.target as HTMLSelectElement;
    this.selectedFiscalDay = target.value;
    if (this.organizationInfo) {
      this.organizationInfo.fiscalYearEndDay = this.selectedFiscalDay;
    }
    console.log('Selected Fiscal Day:', this.selectedFiscalDay);
  }

  saveOrganization(): void {
    console.log('Save organization clicked');
  }

  // addContact(): void {
  //   console.log('Add contact clicked');
  // }

  // addAddress(): void {
  //   console.log('Add address clicked');
  // }
}
