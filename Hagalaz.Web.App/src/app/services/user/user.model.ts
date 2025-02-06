export interface UserInfo {
    sub: string;
    username: string;
    preferred_username: string;
    email: string;
    email_verified: boolean;
    role: string[];
}