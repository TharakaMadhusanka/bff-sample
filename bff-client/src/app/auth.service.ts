import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';

export interface UserProfile {
  name: string;
  email: string;
  roles: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private bffUrl = 'https://localhost:7086';
  
  // Track user state safely across components
  private userSubject = new BehaviorSubject<UserProfile | null>(null);
  public user$: Observable<UserProfile | null> = this.userSubject.asObservable();

  constructor(private http: HttpClient, private router: Router) {}

  checkAuthAndInitialize(): void {
    debugger;
    // withCredentials tells the browser to automatically include the BFF cookies
    this.http.get<UserProfile>(`${this.bffUrl}/user`, { withCredentials: true })
      .subscribe({
        next: (userProfile) => {
          console.log(userProfile);
          // User has a valid cookie! Save user details to show in the UI
          this.userSubject.next(userProfile);
        },
        error: (err) => {
          if (err.status === 401) {
            // No token/cookie available. Trigger full window redirect to log in.
            window.location.href = `${this.bffUrl}/login`;
          } else {
            console.error('An unexpected authentication error occurred:', err);
          }
        }
      });
  }
}
