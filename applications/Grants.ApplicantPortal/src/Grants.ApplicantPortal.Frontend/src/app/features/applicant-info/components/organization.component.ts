import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject, Observable, of } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs/operators';
import {
  OrganizationData,
  OrgSearchResult,
} from '../../../shared/models/applicant-info.interface';

@Component({
  selector: 'app-organization-info',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './organization.component.html',
  styleUrls: ['./organization.component.scss'],
})
export class OrganizationInfoComponent {
  @Input() organizationInfo: OrganizationData | null = null;
  @Input() isLoading = false;
  @Output() saveOrganization = new EventEmitter<void>();
  @Output() searchResultSelected = new EventEmitter<OrgSearchResult>();

  // Search properties
  searchTerm = '';
  searchResults: OrgSearchResult[] = [];
  showDropdown = false;
  isSearching = false;

  // Form properties
  selectedFiscalMonth = '';
  selectedFiscalDay = '';

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

  onSave(): void {
    this.saveOrganization.emit();
  }

  private resetSearch(): void {
    this.searchResults = [];
    this.showDropdown = false;
    this.isSearching = false;
  }
}
