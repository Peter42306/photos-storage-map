const API_PORT = 5008;
const BASE_URL = 
    import.meta.env.VITE_API_BASE_URL || 
    `${window.location.protocol}//${window.location.hostname}:${API_PORT}`;

export function getToken(){
    return localStorage.getItem("accessToken");
}
export function setToken(token) {
    localStorage.setItem("accessToken", token);
}
export function clearToken() {
    localStorage.removeItem("accessToken");
}

async function request(path, {method = "GET", body, auth = true} = {}) {
    const headers = {};

    if(body){
        headers["Content-Type"] = "application/json";
    } 

    if(auth){
        const token = getToken();
        if(token) headers.Authorization = `Bearer ${token}`;
    }

    const res = await fetch(`${BASE_URL}${path}`, {
        method,
        headers,
        body: body ? JSON.stringify(body) : undefined,
    });

    const text = await res.text();
    const data = text ? tryJson(text) : null;

    if(!res.ok){
        const msg = 
            (data && (data.message || data.error || data.title)) || 
            (typeof data === "string" ? data : "") || 
            `HTTP ${res.status}`;
        throw new Error(msg);
    }

    return data;
}

function tryJson(text) {
    try{
        return JSON.parse(text);
    } catch{
        return text;
    }
}

export function login(email, password) {
    return request("/api/auth/login",{
        method:"POST",
        body:{email, password},
        auth:false,
    });
}

export function register(email, password, fullName) {
    return request("/api/auth/register",{
        method: "POST",
        body: {email, password, fullName},
        auth: false,
    });
}

export function confirmEmail(userId, token) {
    const qs = new URLSearchParams({ userId, token }).toString();
    return request(`/api/auth/confirm-email?${qs}`,{
        method: "GET", 
        auth: false});
}

export function resendConfirmation(email) {
    return request("/api/auth/resend-confirmation",{
        method: "POST",
        body: {email},
        auth: false,
    });
}

export function forgotPassword(email){
    return request("/api/auth/forgot-password",{
        method: "POST",
        body: {email},
        auth: false,
    });
}

export function resetPassword(userId, token, newPassword){
    return request("/api/auth/reset-password",{
        method: "POST",
        body: {userId, token, newPassword},
        auth: false,
    });
}

export async function googleLogin(idToken) {
    const res = await fetch(`${BASE_URL}/api/auth/google`,{
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ idToken }),
    });

    if (!res.ok) {
        const text = await res.text();
        throw new Error(text || "Google login failed.");        
    }

    return await res.json();
}

export function me() {
    return request("/api/me");
}


// Uploads (presigned flow)
export function createCollection() {
    return request("/api/uploads/collection",{
        method: "POST",
        auth: true,
    });
}

export function initUpload(collectionId) {
    const qs = new URLSearchParams({ collectionId}).toString();
    return request(`/api/uploads/init?${qs}`,{
        method: "POST",
        auth: true,
    });
}

export function completeUpload(photoId) {
    return request(`/api/uploads/${photoId}/complete`,{
        method: "POST",
        auth: true,
    });
}

// IMPORTANT: presigned PUT goes directly to S3
export async function putToPresignedUrl(uploadUrl, file) {
    const res = await fetch(uploadUrl,{
        method: "PUT",
        body: file,
        // TODO Content-Type ???
    });

    if (!res.ok) {
        const text = await res.text().catch(() => "");
        throw new Error(`S3 PUT faied: ${res.status} ${text}`);
        
    }

    return {status: res.status, etag: res.headers.get("ETag")};
}