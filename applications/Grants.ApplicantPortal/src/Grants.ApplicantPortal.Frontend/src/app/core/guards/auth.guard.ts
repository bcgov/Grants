import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { CanActivateFn } from '@angular/router';
import { map, take } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';

export const authGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  return authService.isAuthenticated$.pipe(
    take(1),
    map((isAuthenticated) => {
      console.log('Auth Guard - isAuthenticated:', isAuthenticated);
      console.log('Auth Guard - current route:', state.url);
      
      if (isAuthenticated) {
        console.log('Auth Guard - User authenticated, allowing access');
        return true;
      } else {
        console.log('Auth Guard - User not authenticated, redirecting to login');
        router.navigate(['/login']);
        return false;
      }
    })
  );
};
