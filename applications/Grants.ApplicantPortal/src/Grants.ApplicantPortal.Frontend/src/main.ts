import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

// Preventive cleanup to avoid HTTP 431 errors on startup
function preventiveCleanup() {
  try {
    const storageSize = calculateStorageSize();
    console.log(`Startup storage check: ${formatBytes(storageSize)}`);
    
    // If storage is over 5MB, perform cleanup
    if (storageSize > 5 * 1024 * 1024) {
      console.warn('Large storage detected on startup, performing cleanup');
      
      // Clean up potentially problematic auth-related entries
      const authKeys = Object.keys(localStorage).filter(key => 
        key.toLowerCase().includes('oidc') || 
        key.toLowerCase().includes('auth') || 
        key.toLowerCase().includes('token')
      );
      
      authKeys.forEach(key => {
        const value = localStorage.getItem(key);
        if (value && value.length > 10000) { // Remove large auth entries
          localStorage.removeItem(key);
          console.log(`Removed large auth entry: ${key}`);
        }
      });
    }
  } catch (error) {
    console.warn('Error during preventive cleanup:', error);
  }
}

function calculateStorageSize(): number {
  let total = 0;
  try {
    for (let key in localStorage) {
      if (localStorage.hasOwnProperty(key)) {
        total += localStorage[key].length + key.length;
      }
    }
    for (let key in sessionStorage) {
      if (sessionStorage.hasOwnProperty(key)) {
        total += sessionStorage[key].length + key.length;
      }
    }
  } catch (error) {
    console.warn('Error calculating storage size:', error);
  }
  return total;
}

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 Bytes';
  const k = 1024;
  const sizes = ['Bytes', 'KB', 'MB', 'GB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
}

// Run preventive cleanup before bootstrapping
preventiveCleanup();

bootstrapApplication(AppComponent, appConfig)
  .catch((err) => console.error(err));
