import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { messageOf } from '../../core/services/api-errors';
import { AuthService } from '../../core/services/auth.service';
import { AutofocusDirective } from '../../shared/directives/autofocus.directive';

/** Sign-in page. After a 401 the interceptor sends the user here with returnUrl, and we go back there. */
@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, AutofocusDirective],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class LoginComponent {
  private readonly fb = inject(FormBuilder);
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly form = this.fb.nonNullable.group({
    userName: ['', Validators.required],
    password: ['', Validators.required]
  });

  readonly busy = signal(false);
  readonly error = signal<string | null>(null);
  readonly expired = signal(this.route.snapshot.queryParamMap.has('expired'));

  readonly demoAccounts = [
    { userName: 'frontdesk', password: 'Desk@123', role: 'Front desk' },
    { userName: 'manager', password: 'Manager@123', role: 'Branch manager' },
    { userName: 'alex', password: 'Member@123', role: 'Member (self-service)' }
  ];

  useAccount(userName: string, password: string): void {
    this.form.setValue({ userName, password });
  }

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.busy.set(true);
    this.error.set(null);
    const { userName, password } = this.form.getRawValue();

    this.auth.login(userName, password).subscribe({
      next: () => {
        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
        const memberUrl = !!returnUrl && (returnUrl === '/member' || returnUrl.startsWith('/member/'));
        const allowed = !!returnUrl && returnUrl.startsWith('/') && (this.auth.role() === 'Member') === memberUrl;
        this.router.navigateByUrl(allowed ? returnUrl! : this.auth.homeUrl());
      },
      error: (err: HttpErrorResponse) => {
        this.busy.set(false);
        this.error.set(err.status === 401 ? messageOf(err, 'Wrong user name or password.') : messageOf(err, "Can't sign in right now."));
      }
    });
  }
}
