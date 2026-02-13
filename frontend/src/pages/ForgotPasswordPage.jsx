import { useState } from "react";
import { Link } from "react-router-dom";
import { forgotPassword } from "../api";

export default function ForgotPasswordPage() {
    const [email, setEmail] = useState("");
    const [status, setStatus] = useState("");
    const [error, setError] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);

    async function handleSubmit(e) {
        e.preventDefault();
        if (isSubmitting) {
            return;
        }

        setError("");
        setStatus("");
        setIsSubmitting(true);

        try {
            await forgotPassword();
            setStatus("If an account exists, we have sent a password reset link to your email.");
        } catch (ex) {
            setError(ex?.message ?? "Failed to send reset email");
        } finally {
            setIsSubmitting(false);
        }
    }

    return(
        <div className="container py-4" style={{maxWidth: 560}}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Forgot your password?</h2>
                    <hr/>
                    <p className="text-muted">
                        Enter your email and we will send you a link to reset your password.
                    </p>

                    {error && <div className="alert alert-danger py-2">{error}</div>}
                    {status && <div className="alert alert-info py-2">{status}</div>}

                    <form onSubmit={handleSubmit}>
                        <div className="mb-3">
                            <label className="form-label">Email</label>
                            <input
                                className="form-control"
                                type="email"
                                name="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                autoComplete="email"
                                required
                            />
                        </div>

                        <button 
                            className="btn btn-primary"
                            type="submit"
                            disabled={isSubmitting}
                        >
                            Send reset link
                        </button>
                    </form>
                    <hr/>
                    <Link to="/login">
                        Back to sign in
                    </Link>
                </div>
            </div>
        </div>
    );
}