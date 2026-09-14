/**
 * Utility functions for Israel Local Time (Asia/Jerusalem) formatting.
 * Ensures all timestamps across the Angular client are deterministically
 * converted and displayed in Israel standard/daylight time (IST/IDT) in 24-hour format
 * rather than UTC or 12-hour AM/PM.
 */

export const ISRAEL_TIMEZONE = 'Asia/Jerusalem';

export interface FormatIsraelTimeOptions {
  includeDate?: boolean;
  includeSeconds?: boolean;
  includeTimezoneBadge?: boolean;
}

/**
 * Formats an ISO date string, Date object, or timestamp to Israel Local Time (24h).
 * Always converts UTC/other timestamps into Asia/Jerusalem (IST/IDT) time.
 *
 * @param dateInput - ISO string, Date instance, timestamp number, or null/undefined
 * @param options - Custom formatting options
 * @returns Formatted Israel local time string, e.g. "14/09/2026, 15:00:00 (Israel Time)"
 */
export function formatIsraelDateTime(
  dateInput: string | Date | number | null | undefined,
  options: FormatIsraelTimeOptions = {}
): string {
  if (dateInput === null || dateInput === undefined || dateInput === '') {
    return '';
  }

  try {
    let normalizedInput: string | Date | number = dateInput;
    if (typeof dateInput === 'string') {
      const trimmed = dateInput.trim();
      // If ISO format without timezone designator (e.g. "2026-09-14T16:04:00"), treat as UTC
      if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?$/.test(trimmed)) {
        normalizedInput = `${trimmed}Z`;
      } else {
        normalizedInput = trimmed;
      }
    }

    const d = typeof normalizedInput === 'string' || typeof normalizedInput === 'number'
      ? new Date(normalizedInput)
      : normalizedInput;

    if (isNaN(d.getTime())) {
      return String(dateInput);
    }

    const {
      includeDate = true,
      includeSeconds = true,
      includeTimezoneBadge = true
    } = options;

    const formatter = new Intl.DateTimeFormat('en-GB', {
      timeZone: ISRAEL_TIMEZONE,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      second: '2-digit',
      hour12: false
    });

    const parts = formatter.formatToParts(d);
    const partMap: Record<string, string> = {};
    for (const part of parts) {
      partMap[part.type] = part.value;
    }

    const day = partMap['day'] || '01';
    const month = partMap['month'] || '01';
    const year = partMap['year'] || '1970';
    const hour = partMap['hour'] || '00';
    const minute = partMap['minute'] || '00';
    const second = partMap['second'] || '00';

    let timeStr = `${hour}:${minute}`;
    if (includeSeconds) {
      timeStr += `:${second}`;
    }

    let result = includeDate ? `${day}/${month}/${year}, ${timeStr}` : timeStr;

    if (includeTimezoneBadge) {
      result += ' (Israel Time)';
    }

    return result;
  } catch {
    return String(dateInput);
  }
}
