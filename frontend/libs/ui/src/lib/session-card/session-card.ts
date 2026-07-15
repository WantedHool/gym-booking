import { DatePipe } from '@angular/common';
import { Component, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { ScheduleSession } from '@frontend/models';
import { CUSTOMER_DATE_FORMATS } from '../date-formats';

@Component({
  selector: 'lib-session-card',
  imports: [DatePipe, MatButtonModule, MatIconModule],
  templateUrl: './session-card.html',
})
export class SessionCard {
  readonly session = input.required<ScheduleSession>();
  readonly dateFormat = input(CUSTOMER_DATE_FORMATS.session);
  readonly waitlistPosition = input<number | null>(null);
  // false → κρύβει «Κράτηση» & «Λίστα αναμονής» (χωρίς ενεργή συνδρομή δεν έχει νόημα).
  // Τα «Ακύρωση»/«Έξοδος» μένουν πάντα ορατά ώστε ο χρήστης να αναιρεί δεσμεύσεις.
  readonly canBook = input(true);

  readonly bookSession = output<ScheduleSession>();
  readonly cancelSession = output<ScheduleSession>();
  readonly joinWaitlist = output<ScheduleSession>();
  readonly leaveWaitlist = output<ScheduleSession>();

  protected isFull(): boolean {
    const s = this.session();
    return s.bookedCount >= s.capacity;
  }
}
