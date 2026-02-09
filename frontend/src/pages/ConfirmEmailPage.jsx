import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { confirmEmail } from "../api";

export default function ConfirmEmailPage() {
    const [sp] = useSearchParams();
    const [status, setStatus] = useState("Confirming...");
    const [error, setError] = useState("");

    useEffect(() => {
        const userId = sp.get("userId");
        const token = sp.get("token");

        if (!userId || !token) {
            setStatus("");
            setError("Missing userId or token in URL.");
            return;
        }

        confirmEmail(userId, token)
            .then((res) => setStatus(res?.message ?? "Email confirmed."))
            .catch((ex) => {
                setStatus("");
                setError(ex.message);
            })
    }, [sp]);

    return(
        <div className="container py-4" style={{ maxWidth: 560 }}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Confirm Email</h2>
                    <hr/>

                    {error && <div className="alert alert-danger py-2">{error}</div>}
                    {status && <div className="alert alert-info py-2">{status}</div>}

                    <Link to="/login">Go to login</Link>

                </div>
            </div>
            
        </div>
    );
}