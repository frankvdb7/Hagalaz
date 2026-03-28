export interface AuthToken {
    access_token: string;
    refresh_token: string;
    id_token: string;
    scope: string;
    expires_in: number;
    token_type: string;
}

export interface UserInfo {
    sub: string;
    username: string;
    preferred_username: string;
    email: string;
    email_verified: boolean;
    role: string[];
}

export interface ResultDto {
    succeeded: boolean;
}
