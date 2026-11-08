import { Component, input, output } from '@angular/core';
import { statuses, TaskStatus } from '../api/models';
@Component({
  selector: 'app-status-control',
  template: `<select
      [attr.aria-label]="'Change status for ' + title()"
      [value]="status()"
      [disabled]="busy()"
      (change)="change($event)"
    >
      @for (option of statuses; track option) {
        <option [value]="option" [disabled]="status() === 'Pending' && option === 'Done'">
          {{ option === 'InProgress' ? 'In progress' : option }}
        </option>
      }
    </select>
    @if (busy()) {
      <small role="status">Saving…</small>
    }`,
})
export class StatusControl {
  readonly status = input.required<TaskStatus>();
  readonly title = input.required<string>();
  readonly busy = input(false);
  readonly requested = output<TaskStatus>();
  readonly statuses = statuses;
  change(event: Event) {
    const select = event.target as HTMLSelectElement;
    const next = select.value as TaskStatus;
    select.value = this.status();
    if (this.busy() || (this.status() === 'Pending' && next === 'Done')) return;
    this.requested.emit(next);
  }
}
