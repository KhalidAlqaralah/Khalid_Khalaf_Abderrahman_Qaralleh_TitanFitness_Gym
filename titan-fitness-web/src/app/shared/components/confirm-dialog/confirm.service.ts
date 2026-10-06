import { Injectable } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { Observable, map } from 'rxjs';
import { ConfirmData, ConfirmDialogComponent } from './confirm-dialog';

@Injectable({ providedIn: 'root' })
export class ConfirmService {
  constructor(private dialog: MatDialog) {}

  ask(data: ConfirmData): Observable<boolean> {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmData, boolean>(ConfirmDialogComponent, { data, width: '420px', panelClass: 'tf-dialog' })
      .afterClosed()
      .pipe(map(result => result === true));
  }

  discardChanges(): Observable<boolean> {
    return this.ask({
      title: 'Discard changes?',
      message: 'You have changed some fields. If you leave now, those changes are lost.',
      confirmText: 'Discard',
      cancelText: 'Keep editing',
      danger: true
    });
  }
}
