import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Subject } from 'rxjs';
import { take, takeUntil } from 'rxjs/operators';

import { ApplicantService } from '../../../core/services/applicant.service';
import { ApplicantInfoService } from '../../../core/services/applicant-info.service';
import { ApplicantInfo } from '../../../shared/models/applicant.interface';
import { ContactInfo, Contact } from '../../../shared/models/applicant-info.interface';
import { DatatableComponent } from '../../../shared/components/datatable/datatable.component';
import { 
  DatatableConfig,
  DatatableActionEvent,
  DatatableRowClickEvent,
  DatatableSortEvent
} from '../../../shared/components/datatable/datatable.models';

interface ContactDisplay {
  id: string;
  type: string;
  name: string;
  email: string;
  phone: string;
  title: string;
  isPrimary: boolean;
  isActive: boolean;
  lastUpdated?: string;
  allowEdit?: boolean;
}

@Component({
  selector: 'app-contacts',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DatatableComponent,
  ],
  templateUrl: './contacts.component.html',
  styleUrls: ['./contacts.component.scss'],
})
export class ContactsComponent implements OnInit, OnDestroy, OnChanges {
  @Input() pluginId!: string;
  @Input() provider!: string;
  @Input() key!: string;

  private readonly destroy$ = new Subject<void>();

  applicantInfo: ApplicantInfo | null = null;
  contacts: ContactDisplay[] = [];
  primaryContact: ContactDisplay | null = null;

  // Modal properties
  showAddContactModal = false;
  isSavingContact = false;
  saveContactError: string | null = null;
  emailValidationError: string | null = null;
  nameValidationError: string | null = null;
  
  // Delete confirmation properties
  showDeleteConfirmModal = false;
  isDeletingContact = false;
  deleteContactError: string | null = null;
  contactToDelete: ContactDisplay | null = null;
  
  // Edit mode properties
  isEditMode = false;
  editingContactId: string | null = null;
  
  newContact: Partial<ContactDisplay> = {
    name: '',
    email: '',
    phone: '',
    title: '',
    type: 'General',
    isPrimary: false,
    isActive: true,
    allowEdit: true
  };

  isLoading = true;
  isHydratingContacts = false;
  error: string | null = null;

  // Datatable configuration
  contactsTableConfig: DatatableConfig = {
    tableId: 'contacts-table',
    defaultSortField: 'lastUpdated',
    enableSortPersistence: true,
    columns: [
      { key: 'name', label: 'Name', sortable: true, cssClass: 'name-column' },
      { key: 'email', label: 'Email', sortable: true, type: 'email', cssClass: 'email-column' },
      { key: 'phone', label: 'Phone', sortable: true, type: 'phone', cssClass: 'phone-column' },
      { key: 'title', label: 'Title', sortable: true, cssClass: 'title-column' },
      { key: 'type', label: 'Type', sortable: true, cssClass: 'type-column' },
      { key: 'isPrimary', label: 'Primary', sortable: true, type: 'boolean', cssClass: 'primary-column' }
    ],
    actionsType: 'dropdown',
    actionItems: [
      { label: 'Set as primary', icon: 'fa-phone', action: 'setAsPrimary' },
      { label: 'Edit', icon: 'fa-pencil-alt', action: 'edit' },
      { label: 'Delete', icon: 'fa-trash', action: 'delete', cssClass: 'text-danger' }
    ],
    actionsVisibilityField: 'allowEdit',

    noDataMessage: 'No contacts found. Click "Add" to create your first contact.',
    loadingMessage: 'Loading your contacts...'
  };


  constructor(
    private readonly applicantService: ApplicantService,
    private readonly applicantInfoService: ApplicantInfoService
  ) {}

  ngOnInit(): void {
    console.log('ContactsComponent ngOnInit - inputs:', {
      pluginId: this.pluginId,
      provider: this.provider,
      hasPluginId: !!this.pluginId,
      hasProvider: !!this.provider
    });
    
    if (this.pluginId && this.provider) {
      console.log('ContactsComponent - Calling loadContacts()');
      this.loadContacts();
    } else {
      console.log('ContactsComponent - Missing pluginId or provider, not loading contacts');
    }
    this.loadApplicantInfo();
  }

  ngOnChanges(changes: SimpleChanges): void {
    console.log('ContactsComponent ngOnChanges called:', {
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
        console.log('ContactsComponent - Input changed, reloading data:', {
          pluginIdChanged: !!pluginIdChanged,
          providerChanged: !!providerChanged,
          oldPluginId: pluginIdChanged?.previousValue,
          newPluginId: pluginIdChanged?.currentValue,
          oldProvider: providerChanged?.previousValue,
          newProvider: providerChanged?.currentValue
        });
        this.loadContacts();
      }
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  // Helper method to get safe data for datatable
  getContactsForTable(): ContactDisplay[] {
    return this.contacts && Array.isArray(this.contacts) ? this.contacts : [];
  }

  private loadApplicantInfo(): void {
    this.applicantService
      .getApplicantInfo()
      .pipe(takeUntil(this.destroy$))
      .subscribe((data) => {
        this.applicantInfo = data;
      });
  }

  private loadContacts(): void {
    console.log('ContactsComponent loadContacts() called with:', {
      pluginId: this.pluginId,
      provider: this.provider
    });
    
    this.isLoading = true;
    this.error = null;

    console.log('Making API call to getContactsInfo...');
    this.applicantInfoService
      .getContactsInfo(
        this.pluginId,
        this.provider
      )
      .pipe(take(1), takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isLoading = false;
          this.contacts = this.processContactsData(result.contactsData || []);
          this.primaryContact = this.contacts.find(contact => contact.isPrimary) || null;
          console.log('Contacts data loaded:', this.contacts);
        },
        error: (error) => {
          this.isLoading = false;
          this.error = 'Failed to load contacts data';
          console.error('Error loading contacts:', error);
        },
      });
  }

  private processContactsData(contacts: any[]): ContactDisplay[] {
    return contacts.map(contact => ({
      id: contact.id || `contact-${Math.random()}`,
      type: contact.type || 'Unknown',
      name: contact.name || '',
      email: contact.email || '',
      phone: contact.phone || '',
      title: contact.title || '',
      isPrimary: contact.isPrimary || false,
      isActive: contact.isActive !== false,
      lastUpdated: contact.lastUpdated,
      allowEdit: contact.allowEdit !== false
    }));
  }

  // Event handlers
  onAddContact(): void {
    this.isEditMode = false;
    this.editingContactId = null;
    this.resetNewContactForm();
    this.showAddContactModal = true;
  }

  onSaveNewContact(): void {
    if (!this.isValidContact(this.newContact)) {
      return;
    }

    this.isSavingContact = true;
    this.saveContactError = null;

    // Prepare the API payload
    const contactPayload = {
      name: this.newContact.name!,
      email: this.newContact.email || '',
      title: this.newContact.title || '',
      type: this.newContact.type!,
      phoneNumber: this.newContact.phone || '',
      isPrimary: this.newContact.isPrimary!
    };

    const apiCall = this.isEditMode
      ? this.applicantInfoService.updateContact(
          this.editingContactId!,
          this.pluginId,
          this.provider,
          contactPayload
        )
      : this.applicantInfoService.createContact(
          this.pluginId,
          this.provider,
          contactPayload
        );

    apiCall.pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        console.log(`Contact ${this.isEditMode ? 'updated' : 'created'} successfully:`, response);
        this.isSavingContact = false;
        this.showAddContactModal = false;
        this.resetNewContactForm();
        // Refresh contacts data to show updated information
        this.loadContacts();
      },
      error: (error) => {
        console.error(`Failed to ${this.isEditMode ? 'update' : 'create'} contact:`, error);
        this.isSavingContact = false;
        this.saveContactError = error?.error?.message || `Failed to ${this.isEditMode ? 'update' : 'create'} contact. Please try again.`;
      }
    });
  }

  onCancelAddContact(): void {
    this.showAddContactModal = false;
    this.isSavingContact = false;
    this.saveContactError = null;
    this.isEditMode = false;
    this.editingContactId = null;
    this.resetNewContactForm();
  }

  private resetNewContactForm(): void {
    this.saveContactError = null;
    this.emailValidationError = null;
    this.nameValidationError = null;
    
    if (!this.isEditMode) {
      this.newContact = {
        name: '',
        email: '',
        phone: '',
        title: '',
        type: 'General',
        isPrimary: false,
        isActive: true,
        allowEdit: true
      };
    }
  }

  isValidContact(contact: Partial<ContactDisplay>): boolean {
    this.validateName();
    this.validateEmail();
    
    return this.nameValidationError === null && this.emailValidationError === null;
  }

  validateName(): void {
    if (!this.newContact.name || this.newContact.name.trim().length === 0) {
      this.nameValidationError = 'Name is required';
    } else {
      this.nameValidationError = null;
    }
  }

  validateEmail(): void {
    if (this.newContact.email && this.newContact.email.trim().length > 0) {
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(this.newContact.email)) {
        this.emailValidationError = 'Please enter a valid email address';
      } else {
        this.emailValidationError = null;
      }
    } else {
      this.emailValidationError = null;
    }
  }

  onNameChange(): void {
    this.validateName();
  }

  onEmailChange(): void {
    this.validateEmail();
  }

  onContactClick(contact: Contact): void {
    console.log('Clicked contact:', contact);
    // TODO: Navigate to contact detail view
  }

  onEditContact(contact: ContactDisplay): void {
    console.log('Editing contact...', contact);
    this.isEditMode = true;
    this.editingContactId = contact.id;
    this.resetNewContactForm();
    
    // Populate form with existing contact data
    this.newContact = {
      name: contact.name,
      email: contact.email,
      title: contact.title,
      type: contact.type,
      phone: contact.phone,
      isPrimary: contact.isPrimary,
      isActive: contact.isActive || true,
      allowEdit: contact.allowEdit || true
    };
    
    this.showAddContactModal = true;
  }

  onDeleteContact(contact: ContactDisplay): void {
    console.log('Preparing to delete contact...', contact);
    this.contactToDelete = contact;
    this.deleteContactError = null;
    this.showDeleteConfirmModal = true;
  }

  onConfirmDeleteContact(): void {
    if (!this.contactToDelete) {
      return;
    }

    this.isDeletingContact = true;
    this.deleteContactError = null;

    // For embedded data, just remove from local display
    this.contacts = this.contacts.filter(c => c.id !== this.contactToDelete!.id);
    console.log('Contact deleted locally:', this.contactToDelete);
    
    this.isDeletingContact = false;
    this.showDeleteConfirmModal = false;
    this.contactToDelete = null;
  }

  onCancelDeleteContact(): void {
    this.showDeleteConfirmModal = false;
    this.isDeletingContact = false;
    this.deleteContactError = null;
    this.contactToDelete = null;
  }

  onSetAsPrimary(contact: ContactDisplay): void {
    console.log('Setting as primary contact...', contact);
    
    // Make API call to set contact as primary
    const contactData = {
      name: contact.name,
      email: contact.email,
      title: contact.title,
      type: contact.type,
      phoneNumber: contact.phone,
      isPrimary: true
    };

    this.applicantInfoService.updateContact(
      contact.id,
      this.pluginId,
      this.provider,
      contactData
    )
    .pipe(takeUntil(this.destroy$))
    .subscribe({
      next: (response: any) => {
        console.log('Contact set as primary successfully:', response);
        
        // Update local state after successful API call
        this.contacts = this.contacts.map(c => ({
          ...c,
          isPrimary: c.id === contact.id
        }));
        
        this.primaryContact = { ...contact, isPrimary: true };
        console.log('Primary contact updated locally:', this.primaryContact);
      },
      error: (error: any) => {
        console.error('Failed to set contact as primary:', error);
        // Optionally show an error message to the user
      }
    });
  }

  closeDropdown(dropdownToggle: any): void {
    dropdownToggle.click();
  }

  getContactTypeClass(type: string): string {
    switch (type) {
      case 'Primary':
        return 'contact-type-primary';
      case 'Secondary':
        return 'contact-type-secondary';
      case 'Emergency':
        return 'contact-type-emergency';
      default:
        return '';
    }
  }

  // Datatable event handlers
  onContactRowClick(event: DatatableRowClickEvent): void {
    console.log('Clicked contact:', event.row);
    // TODO: Navigate to contact detail view
  }

  onContactAction(event: DatatableActionEvent): void {
    const contact = event.row as ContactDisplay;
    
    // Check if the contact can be edited for actions that require it
    if ((event.action === 'edit' || event.action === 'setAsPrimary' || event.action === 'delete') && !contact.allowEdit) {
      console.log('Action not allowed: Contact cannot be edited');
      return;
    }

    switch (event.action) {
      case 'setAsPrimary':
        this.onSetAsPrimary(contact);
        break;
      case 'edit':
        this.onEditContact(contact);
        break;
      case 'delete':
        this.onDeleteContact(contact);
        break;
      default:
        console.log('Unknown action:', event.action);
    }
  }

  onContactSort(event: DatatableSortEvent): void {
    console.log('Contacts sorted by:', event.column, event.direction);
    // The datatable component now handles all sorting internally
    // This event is emitted for any additional logic you might need
  }
}