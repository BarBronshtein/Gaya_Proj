import { IsraelDatePipe } from './israel-date.pipe';

describe('IsraelDatePipe', () => {
  let pipe: IsraelDatePipe;

  beforeEach(() => {
    pipe = new IsraelDatePipe();
  });

  it('should create an instance', () => {
    expect(pipe).toBeTruthy();
  });

  it('should format UTC timestamp to Israel Local Time string', () => {
    const formatted = pipe.transform('2026-09-14T12:00:00Z');
    expect(formatted).toBe('14/09/2026, 15:00:00 (Israel Time)');
    expect(formatted).not.toContain('UTC');
  });

  it('should handle null / empty inputs gracefully', () => {
    expect(pipe.transform(null)).toBe('');
    expect(pipe.transform(undefined)).toBe('');
    expect(pipe.transform('')).toBe('');
  });

  it('should pass options through to formatting utility', () => {
    const formatted = pipe.transform('2026-09-14T12:00:00Z', { includeTimezoneBadge: false });
    expect(formatted).toBe('14/09/2026, 15:00:00');
  });
});
