import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class StorageCleanupService {

  constructor() {}

  /**
   * Initialize storage monitoring and cleanup
   */
  initialize(): void {
    this.monitorStorageSize();
    this.schedulePeriodicCleanup();
  }

  /**
   * Monitor storage size and trigger cleanup if it gets too large
   */
  private monitorStorageSize(): void {
    try {
      const totalSize = this.calculateStorageSize();
      console.log(`Current storage size: ${this.formatBytes(totalSize)}`);
      
      // If storage is over 8MB, trigger cleanup
      if (totalSize > 8 * 1024 * 1024) {
        console.warn('Storage size is large, triggering cleanup');
        this.performMaintenance();
      }
    } catch (error) {
      console.warn('Error monitoring storage size:', error);
    }
  }

  /**
   * Calculate total storage size
   */
  private calculateStorageSize(): number {
    let total = 0;
    
    try {
      // Calculate localStorage size
      for (let key in localStorage) {
        if (localStorage.hasOwnProperty(key)) {
          total += localStorage[key].length + key.length;
        }
      }
      
      // Calculate sessionStorage size
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

  /**
   * Schedule periodic cleanup
   */
  private schedulePeriodicCleanup(): void {
    // Clean up every hour
    setInterval(() => {
      this.performMaintenance();
    }, 60 * 60 * 1000);
  }

  /**
   * Perform storage maintenance
   */
  performMaintenance(): void {
    console.log('Performing storage maintenance...');
    
    try {
      this.cleanupOldTokens();
      this.cleanupDuplicateEntries();
      this.cleanupLargeEntries();
      
      const finalSize = this.calculateStorageSize();
      console.log(`Storage size after cleanup: ${this.formatBytes(finalSize)}`);
    } catch (error) {
      console.error('Error during storage maintenance:', error);
    }
  }

  /**
   * Clean up expired or old auth tokens
   */
  private cleanupOldTokens(): void {
    try {
      const keysToCheck = Object.keys(localStorage).concat(Object.keys(sessionStorage));
      const authKeyPatterns = [
        /^.*oidc.*$/i,
        /^.*auth.*$/i,
        /^.*token.*$/i,
        /^.*angular-auth.*$/i,
        /^.*Bearer.*$/i
      ];

      keysToCheck.forEach(key => {
        if (authKeyPatterns.some(pattern => pattern.test(key))) {
          try {
            const value = localStorage.getItem(key) || sessionStorage.getItem(key);
            if (value && this.isTokenExpired(value)) {
              localStorage.removeItem(key);
              sessionStorage.removeItem(key);
              console.log(`Removed expired token: ${key}`);
            }
          } catch (error) {
            // If we can't parse the token, it's probably corrupted - remove it
            localStorage.removeItem(key);
            sessionStorage.removeItem(key);
            console.log(`Removed corrupted token: ${key}`);
          }
        }
      });
    } catch (error) {
      console.warn('Error cleaning up old tokens:', error);
    }
  }

  /**
   * Remove duplicate storage entries
   */
  private cleanupDuplicateEntries(): void {
    try {
      const localKeys = Object.keys(localStorage);
      const sessionKeys = Object.keys(sessionStorage);
      
      // Find keys that exist in both storages
      const duplicateKeys = localKeys.filter(key => sessionKeys.includes(key));
      
      duplicateKeys.forEach(key => {
        const localValue = localStorage.getItem(key);
        const sessionValue = sessionStorage.getItem(key);
        
        // Keep the one in localStorage, remove from sessionStorage
        if (localValue === sessionValue) {
          sessionStorage.removeItem(key);
          console.log(`Removed duplicate from sessionStorage: ${key}`);
        }
      });
    } catch (error) {
      console.warn('Error cleaning up duplicates:', error);
    }
  }

  /**
   * Clean up entries that are too large
   */
  private cleanupLargeEntries(): void {
    try {
      const maxEntrySize = 100 * 1024; // 100KB per entry
      
      [localStorage, sessionStorage].forEach(storage => {
        Object.keys(storage).forEach(key => {
          try {
            const value = storage.getItem(key);
            if (value && value.length > maxEntrySize) {
              storage.removeItem(key);
              console.log(`Removed oversized entry: ${key} (${this.formatBytes(value.length)})`);
            }
          } catch (error) {
            console.warn(`Error checking size of ${key}:`, error);
          }
        });
      });
    } catch (error) {
      console.warn('Error cleaning up large entries:', error);
    }
  }

  /**
   * Check if a token is expired
   */
  private isTokenExpired(tokenString: string): boolean {
    try {
      // Try to parse as JWT
      if (tokenString.includes('.')) {
        const payload = this.parseJwtPayload(tokenString);
        if (payload && payload.exp) {
          const now = Math.floor(Date.now() / 1000);
          return payload.exp < now;
        }
      }
      
      // Try to parse as JSON with exp field
      const parsed = JSON.parse(tokenString);
      if (parsed && parsed.exp) {
        const now = Math.floor(Date.now() / 1000);
        return parsed.exp < now;
      }
      
      return false;
    } catch {
      return false;
    }
  }

  /**
   * Parse JWT payload
   */
  private parseJwtPayload(token: string): any {
    try {
      const parts = token.split('.');
      if (parts.length !== 3) return null;
      
      const payload = parts[1];
      const decoded = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }

  /**
   * Format bytes to human readable format
   */
  private formatBytes(bytes: number): string {
    if (bytes === 0) return '0 Bytes';
    
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  /**
   * Emergency total cleanup - use only when HTTP 431 occurs
   */
  emergencyCleanup(): void {
    console.log('Performing emergency total cleanup');
    
    try {
      localStorage.clear();
      sessionStorage.clear();
      
      // Clear any service worker caches if available
      if ('caches' in window) {
        caches.keys().then(names => {
          names.forEach(name => {
            caches.delete(name);
          });
        });
      }
      
      console.log('Emergency cleanup completed');
    } catch (error) {
      console.error('Error during emergency cleanup:', error);
    }
  }
}