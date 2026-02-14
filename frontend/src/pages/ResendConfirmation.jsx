import { useState } from "react";
import { Link, replace, useNavigate } from "react-router-dom";
import { resendConfirmation } from "../api";

export default function ResendConfirmationPage() {
    const [email, setEmail] = useState("");
    const [status, setStatus] = useState("");
    const [error, setError] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);
    const navigate = useNavigate();

    async function handleSubmit(e) {
        e.preventDefault();
        if (isSubmitting) {
            return;
        }

        setError("");
        setStatus("");
        setIsSubmitting(true);

        try {
            const res = await resendConfirmation(email);
            setStatus(res?.message ?? "If an account exists, a confirmation email has been sent.")
            //navigate("/login", { replace: true });
        } catch (ex) {
            setStatus("");
            setError(ex.message);
        }
    }

    return(
        <div className="container py-4" style={{maxWidth: 560}}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Resend Email Confirmation</h2>
                    <hr/>
                    <p className="text-muted">
                        If you have registered but haven't confirmed your email yet, please enter your registered email and we will send you a new confirmation link.
                    </p>                    

                    {error && <div className="alert alert-danger py-2">{error}</div>}
                    {status && <div className="alert alert-info py-2">{status}</div>}
                    {/* {isSubmitting && <div className="alert alert-info py-2">Sending...</div>} */}

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
                                disabled={isSubmitting}
                                required
                            />
                        </div>

                        <div className="d-flex justify-content-center">
                            <button
                                className="btn btn-primary"
                                type="submit"
                                disabled={isSubmitting}
                                style={{ width: 250 }}
                            >
                                Resend
                            </button>
                        </div>

                        
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