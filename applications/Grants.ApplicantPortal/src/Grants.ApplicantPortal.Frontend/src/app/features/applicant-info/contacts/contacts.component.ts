import { Component, OnInit, OnDestroy, Input, OnChanges, SimpleChanges } from '@angular/core';
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

interface Contact {
  id: string; // GUID
  contactId: string;
  type: string;
  firstName: string;
  lastName: string;
  name: string;
  email: string;
  phone: string;
  title: string;
  extension?: string;
  department?: string;
  isPrimary: boolean;
  isActive: boolean;
  preferredContact: boolean;
  lastUpdated: string;
  allowEdit: boolean;
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
  @Input() profileId!: string;
  @Input() pluginId!: string;
  @Input() provider!: string;

  applicantInfo: ApplicantInfo | null = null;
  contacts: Contact[] = [];
  primaryContact: Contact | null = null;

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
  contactToDelete: Contact | null = null;
  
  // Edit mode properties
  isEditMode = false;
  editingContactId: string | null = null;
  
  newContact: Partial<Contact> = {
    firstName: '',
    lastName: '',
    name: '',
    email: '',
    phone: '',
    title: '',
    extension: '',
    department: '',
    type: 'General',
    isPrimary: false,
    isActive: true,
    preferredContact: false,
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

  ngOnChanges(changes: SimpleChanges): void {
    // Reload data when pluginId changes (workspace switch)
    if (changes['pluginId'] && !changes['pluginId'].firstChange) {
      console.log('ContactsComponent - Plugin ID changed, reloading data');
      this.loadData();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private loadData(): void {
    this.loadApplicantInfo();
    this.loadContacts();
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
    this.isHydratingContacts = true;
    this.error = null;

    this.applicantInfoService
      .getContactsInfo(
        this.profileId,
        this.pluginId,
        this.provider
      )
      .pipe(take(1), takeUntil(this.destroy$))
      .subscribe({
        next: (result) => {
          this.isHydratingContacts = false;
          // Parse the result based on the actual API response structure
          this.contacts = Array.isArray(result.jsonData) 
            ? result.jsonData 
            : JSON.parse(result.jsonData)?.data?.contacts || [];
          
          this.primaryContact = this.contacts.find(c => c.isPrimary) || this.contacts[0] || null;
          console.log('Contacts data loaded:', this.contacts);
          this.isLoading = false;
        },
        error: (error) => {
          this.isHydratingContacts = false;
          this.error = 'Failed to load contacts data';
          this.isLoading = false;
          console.error('Error loading contacts:', error);
        },
      });
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
          this.profileId,
          this.pluginId,
          this.provider,
          contactPayload
        )
      : this.applicantInfoService.createContact(
          this.profileId,
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
        // Refresh the contacts list
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
        firstName: '',
        lastName: '',
        name: '',
        email: '',
        phone: '',
        title: '',
        extension: '',
        department: '',
        type: 'General',
        isPrimary: false,
        isActive: true,
        preferredContact: false,
        allowEdit: true
      };
    }
  }

  getContactsForTable(): any[] {
    return this.contacts.map(contact => {
      const tableRow = { ...contact };      
      return tableRow;
    });
  }

  isValidContact(contact: Partial<Contact>): boolean {
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

  onEditContact(contact: Contact): void {
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
      extension: contact.extension,
      department: contact.department,
      isActive: contact.isActive,
      preferredContact: contact.preferredContact,
      allowEdit: contact.allowEdit
    };
    
    this.showAddContactModal = true;
  }

  onDeleteContact(contact: Contact): void {
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

    this.applicantInfoService.deleteContact(
      this.contactToDelete.id,
      this.profileId,
      this.pluginId,
      this.provider
    ).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        console.log('Contact deleted successfully:', response);
        this.isDeletingContact = false;
        this.showDeleteConfirmModal = false;
        this.contactToDelete = null;
        // Refresh the contacts list
        this.loadContacts();
      },
      error: (error) => {
        console.error('Failed to delete contact:', error);
        this.isDeletingContact = false;
        this.deleteContactError = error?.error?.message || 'Failed to delete contact. Please try again.';
      }
    });
  }

  onCancelDeleteContact(): void {
    this.showDeleteConfirmModal = false;
    this.isDeletingContact = false;
    this.deleteContactError = null;
    this.contactToDelete = null;
  }

  onSetAsPrimary(contact: Contact): void {
    console.log('Setting as primary contact...', contact);
    
    this.applicantInfoService.setContactAsPrimary(
      contact.id,
      this.profileId,
      this.pluginId,
      this.provider
    ).pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (response) => {
        console.log('Contact set as primary successfully:', response);
        // Refresh the contacts list to update the display
        this.loadContacts();
      },
      error: (error) => {
        console.error('Failed to set contact as primary:', error);
        // Could add a toast notification here
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
    const contact = event.row as Contact;
    
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