import { Pipe, PipeTransform } from '@angular/core';
import { formatIsraelDateTime, FormatIsraelTimeOptions } from '../utils/date.utils';

/**
 * Angular pipe for converting any timestamp/date to Israel Local Time (Asia/Jerusalem).
 *
 * Usage:
 *   {{ item.executedAt | israelDate }}
 *   {{ item.executedAt | israelDate:{ includeTimezoneBadge: false } }}
 */
@Pipe({
  name: 'israelDate',
  standalone: true
})
export class IsraelDatePipe implements PipeTransform {
  transform(
    value: string | Date | number | null | undefined,
    options?: FormatIsraelTimeOptions
  ): string {
    return formatIsraelDateTime(value, options);
  }
}
