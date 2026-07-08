import { AuthUser } from '@frontend/models';

export interface JwtPayload {
  sub: string;
  tenantId: string;
  role?: string | string[];
  exp: number;
}

function base64UrlDecode(segment: string): string {
  const base64 = segment.replace(/-/g, '+').replace(/_/g, '/');
  const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
  return atob(padded);
}

export function decodeJwt(token: string): JwtPayload | null {
  const parts = token.split('.');
  if (parts.length !== 3) {
    return null;
  }
  try {
    return JSON.parse(base64UrlDecode(parts[1])) as JwtPayload;
  } catch {
    return null;
  }
}

export function isTokenExpired(payload: JwtPayload): boolean {
  return payload.exp * 1000 <= Date.now();
}

export function toAuthUser(payload: JwtPayload): AuthUser {
  const roles = Array.isArray(payload.role)
    ? payload.role
    : payload.role
      ? [payload.role]
      : [];
  return { id: payload.sub, tenantId: payload.tenantId, roles };
}
