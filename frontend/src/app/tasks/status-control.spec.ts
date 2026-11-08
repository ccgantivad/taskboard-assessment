import { TestBed } from '@angular/core/testing';
import { StatusControl } from './status-control';
describe('Status control', () => {
  it('blocks only Pending to Done and leaves current display unchanged before confirmation', () => {
    const fixture = TestBed.createComponent(StatusControl);
    fixture.componentRef.setInput('status', 'Pending');
    fixture.componentRef.setInput('title', 'Synthetic');
    fixture.detectChanges();
    let emitted = '';
    fixture.componentInstance.requested.subscribe((value) => (emitted = value));
    const select: HTMLSelectElement = fixture.nativeElement.querySelector('select');
    expect(select.querySelector<HTMLOptionElement>('option[value="Done"]')!.disabled).toBe(true);
    select.value = 'InProgress';
    select.dispatchEvent(new Event('change'));
    expect(emitted).toBe('InProgress');
    expect(select.value).toBe('Pending');
    fixture.componentRef.setInput('status', 'Done');
    fixture.detectChanges();
    expect(select.querySelector<HTMLOptionElement>('option[value="Pending"]')!.disabled).toBe(
      false,
    );
    expect(select.querySelector<HTMLOptionElement>('option[value="InProgress"]')!.disabled).toBe(
      false,
    );
  });
});
