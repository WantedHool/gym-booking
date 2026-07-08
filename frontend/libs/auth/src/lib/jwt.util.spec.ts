import { decodeJwt, isTokenExpired, toAuthUser } from './jwt.util';

function makeToken(payload: Record<string, unknown>): string {
  const base64url = (value: unknown) =>
    btoa(JSON.stringify(value))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return `${base64url({ alg: 'HS256' })}.${base64url(payload)}.signature`;
}

describe('jwt.util', () => {
  it('decodes a well-formed token', () => {
    const token = makeToken({ sub: 'u1', tenantId: 't1', role: 'Admin', exp: 123 });

    expect(decodeJwt(token)).toEqual({
      sub: 'u1',
      tenantId: 't1',
      role: 'Admin',
      exp: 123,
    });
  });

  it('returns null for a malformed token', () => {
    expect(decodeJwt('not-a-jwt')).toBeNull();
  });

  it('treats a past exp as expired', () => {
    const past = Math.floor(Date.now() / 1000) - 10;
    expect(isTokenExpired({ sub: 'u1', tenantId: 't1', exp: past })).toBe(true);
  });

  it('treats a future exp as not expired', () => {
    const future = Math.floor(Date.now() / 1000) + 3600;
    expect(isTokenExpired({ sub: 'u1', tenantId: 't1', exp: future })).toBe(false);
  });

  it('normalizes a single role string into an array', () => {
    expect(toAuthUser({ sub: 'u1', tenantId: 't1', role: 'Admin', exp: 0 })).toEqual({
      id: 'u1',
      tenantId: 't1',
      roles: ['Admin'],
    });
  });

  it('keeps multiple roles as an array', () => {
    expect(
      toAuthUser({ sub: 'u1', tenantId: 't1', role: ['User', 'Instructor'], exp: 0 }),
    ).toEqual({ id: 'u1', tenantId: 't1', roles: ['User', 'Instructor'] });
  });

  it('defaults to no roles when the claim is missing', () => {
    expect(toAuthUser({ sub: 'u1', tenantId: 't1', exp: 0 })).toEqual({
      id: 'u1',
      tenantId: 't1',
      roles: [],
    });
  });
});
