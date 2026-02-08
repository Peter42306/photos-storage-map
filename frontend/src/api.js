const BASE_URL = "https://localhost:7067";

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

    if(body) headers["Content-Type"] = "application/json";

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
        body:{email,password},
        auth:false,
    });
}

export function me() {
    return request("/api/me");
}