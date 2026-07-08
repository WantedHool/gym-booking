export interface AuthUser {
  id: string;
  tenantId: string;
  roles: string[];
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
}

export interface RegisterRequest {
  password: string;
  firstName: string;
  lastName: string;
}
