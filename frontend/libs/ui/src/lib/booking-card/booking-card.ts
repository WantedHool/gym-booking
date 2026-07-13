import { DatePipe } from '@angular/common';
import { Component, input } from '@angular/core';
import { Booking } from '@frontend/models';
import { CUSTOMER_DATE_FORMATS } from '../date-formats';
import { StatusBadge } from '../status-badge/status-badge';

@Component({
  selector: 'lib-booking-card',
  imports: [DatePipe, StatusBadge],
  templateUrl: './booking-card.html',
})
export class BookingCard {
  readonly booking = input.required<Booking>();
  readonly dateFormat = input<string>(CUSTOMER_DATE_FORMATS.session);
  readonly showStatus = input(false);
}
