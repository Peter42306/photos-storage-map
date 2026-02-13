import { useEffect, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { confirmEmail } from "../api";

export default function ConfirmEmailPage() {
    const [sp] = useSearchParams();

    const userId = sp.get("userId");
    const token = sp.get("token");
    const missingParams = !userId || !token;

    const [status, setStatus] = useState(missingParams ? "" : "Confirming your email...");
    const [error, setError] = useState(missingParams ? "Missing userId or token in URL." : "");    

    useEffect(() => {
        if (missingParams) {            
            return;
        }

        confirmEmail(userId, token)
            .then((res) =>{
                setError("");
                setStatus((res?.message) ?? "Your email has been confirmed.")
            })
            .catch((ex) => {
                setStatus("");
                setError(ex?.message ?? "Email confirmation failed.");
            });
    }, [missingParams, userId, token]);

    return(
        <div className="container py-4" style={{ maxWidth: 560 }}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Email Confirmation Status</h2>
                    <hr/>

                    {error && <div className="alert alert-danger py-2">{error}</div>}
                    {status && <div className="alert alert-info py-2">{status}</div>}

                    {!missingParams &&(
                        <Link className="btn btn-primary" to="/login">Sign in</Link>
                    )}
                    

                </div>
            </div>
            
        </div>
    );
}