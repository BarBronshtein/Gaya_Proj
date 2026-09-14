import { formatIsraelDateTime, ISRAEL_TIMEZONE } from './date.utils';

describe('Date Utilities (Israel Time)', () => {
  it('should export ISRAEL_TIMEZONE as Asia/Jerusalem', () => {
    expect(ISRAEL_TIMEZONE).toBe('Asia/Jerusalem');
  });

  it('should return empty string for null, undefined, or empty string input', () => {
    expect(formatIsraelDateTime(null)).toBe('');
    expect(formatIsraelDateTime(undefined)).toBe('');
    expect(formatIsraelDateTime('')).toBe('');
  });

  it('should return input as string if invalid date is passed', () => {
    expect(formatIsraelDateTime('not-a-date')).toBe('not-a-date');
  });

  it('should correctly format UTC timestamp to Israel Daylight Time (UTC+3 in Summer)', () => {
    // 2026-09-14 12:00:00 UTC -> 15:00:00 IDT (+3)
    const result = formatIsraelDateTime('2026-09-14T12:00:00Z');
    expect(result).toBe('14/09/2026, 15:00:00 (Israel Time)');
    expect(result).not.toContain('UTC');
  });

  it('should correctly format UTC timestamp to Israel Standard Time (UTC+2 in Winter)', () => {
    // 2026-01-15 10:30:00 UTC -> 12:30:00 IST (+2)
    const result = formatIsraelDateTime('2026-01-15T10:30:00Z');
    expect(result).toBe('15/01/2026, 12:30:00 (Israel Time)');
    expect(result).not.toContain('UTC');
  });

  it('should handle Date instances', () => {
    const d = new Date('2026-09-14T12:00:00Z');
    const result = formatIsraelDateTime(d);
    expect(result).toContain('15:00:00');
    expect(result).toContain('Israel Time');
  });

  it('should handle numeric millisecond timestamps', () => {
    const d = new Date('2026-09-14T12:00:00Z').getTime();
    const result = formatIsraelDateTime(d);
    expect(result).toContain('15:00:00');
    expect(result).toContain('Israel Time');
  });

  it('should support disabling the timezone badge if requested', () => {
    const result = formatIsraelDateTime('2026-09-14T12:00:00Z', {
      includeTimezoneBadge: false
    });
    expect(result).toBe('14/09/2026, 15:00:00');
  });

  it('should support excluding seconds if requested', () => {
    const result = formatIsraelDateTime('2026-09-14T12:00:00Z', {
      includeSeconds: false,
      includeTimezoneBadge: false
    });
    expect(result).toBe('14/09/2026, 15:00');
  });

  it('should treat ISO strings without timezone specifier as UTC and format to Israel time', () => {
    // 2026-09-14 12:00:00 without Z should be treated as UTC -> 15:00:00 IDT (+3)
    const result = formatIsraelDateTime('2026-09-14T12:00:00');
    expect(result).toBe('14/09/2026, 15:00:00 (Israel Time)');
  });
});

