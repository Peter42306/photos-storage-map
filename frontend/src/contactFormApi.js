const CONTACT_FORM_API_URL = import.meta.env.VITE_CONTACT_FORM_API_URL || "";

async function publicFormRequest(path, body) {
    const response = await fetch(`${CONTACT_FORM_API_URL}${path}`,{
        method:"POST",
        headers:{
            "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
    });

    const text = await response.text();
    const data = text ? tryJson(text) : null;
    
    if (!response.ok) {
        const message = 
            data?.message ||
            data?.error ||
            data?.title ||
            (typeof data === "string" ? data : "") ||
            `HTTP ${response.status}`;

        throw new Error(message);            
    }

    return data;
}

function tryJson(text) {
    try {
        return JSON.parse(text);
    } catch {
        return text;
    }
}

export function sendContactMessage({
    senderName,
    senderEmail,
    subject,
    body,
}) {
    return publicFormRequest("/api/contact",{
        appKey:"photo-map",
        senderName,
        senderEmail,
        subject,
        body,
    });
}

export function submitFeedback({
    userId = null,
    senderEmail = null,
    type,
    subject = null,
    body,
}) {
    return publicFormRequest("/api/feedback",{
        appKey:"photo-map",
        userId,
        senderEmail,
        type,
        subject,
        body,
    });
}