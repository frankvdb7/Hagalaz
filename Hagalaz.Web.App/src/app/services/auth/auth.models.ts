export interface AuthLoginRequest {
    email: string;
    password: string;
}

// tslint:disable-next-line:no-empty-interface
export interface AuthLoginResponse extends AuthToken {}

export interface AuthToken {
    access_token: string;
    refresh_token: string;
    id_token: string;
    scope: string;
    expires_in: number;
    token_type: string;
}
