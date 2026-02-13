import { useState } from "react";
import { Link, useNavigate, useSearchParams } from "react-router-dom";
import { resetPassword } from "../api";

export default function ResetPasswordPage() {
    const [sp] = useSearchParams();    

    const userId = sp.get("userId");
    const token = sp.get("token");
    const missingParams = !userId || !token;

    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");
    
    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    const [status, setStatus] = useState("");
    const [error, setError] = useState(missingParams ? "Missing userId or token in URL." : "")
    const [isSubmitting, setIsSubmitting] = useState(false);

    const navigate = useNavigate();

    async function handleSubmit(e) {
        e.preventDefault();

        if (isSubmitting) {
            return;
        }

        setError("");
        setStatus("");

        if (missingParams) {
            setError("Missing userId or token in URL.");
            return;
        }

        if (password !== confirmPassword) {
            setError("Passwords do not match.");
            return;
        }

        setIsSubmitting(true);
        setStatus("Resetting password...");

        try {
            const res = await resetPassword(userId, token, password);
            setStatus(res?.message ?? "Password has been reset. You can sign in now.");
            navigate("/login", { replace: true });
        } catch (ex) {
            setStatus("");
            setError(ex?.message ?? "Failed to reset password.");
        } finally {
            setIsSubmitting(false);
        }
    }

    return(
        <div className="container py-4" style={{maxWidth: 560}}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Reset password</h2>
                    <hr/>

                    {error && <div className="alert alert-danger py-2">{error}</div>}
                    {status && <div className="alert alert-info py-2">{status}</div>}
                    

                    {/* TODO: !missingparams */}
                    {!missingParams && (                        
                        <form onSubmit={handleSubmit}>
                            <p className="text-muted">
                                Enter new password.
                            </p>
                            <div className="mb-3">
                                <label className="form-label">New password</label>
                                <div className="input-group">
                                    <input 
                                        className="form-control"
                                        type={showPassword ? "text" : "password"}
                                        name="newPassword"
                                        value={password}
                                        onChange={(e) => setPassword(e.target.value)}
                                        autoComplete="new-password"
                                        required
                                    />
                                    <span
                                        className="input-group-text bg-white"
                                        role="button"
                                        onClick={() => setShowPassword(v => !v)}
                                        onMouseDown={(e) => e.preventDefault()}
                                        aria-label={showPassword ? "Hide password" : "Show password"}
                                        title={showPassword ? "Hide password" : "Show password"}
                                        style={{ cursor: "pointer" }}
                                    >
                                        <i className={showPassword ? "bi bi-eye-slash" : "bi bi-eye"}/>
                                    </span>
                                </div>                               
                            </div>
                            <div className="mb-3">
                                <label className="form-label">Confirm new password</label>
                                <div className="input-group">
                                    <input 
                                        className="form-control"
                                        type={showConfirmPassword ? "text" : "password"}
                                        name="confirmPassword"
                                        value={confirmPassword}
                                        onChange={(e) => setConfirmPassword(e.target.value)}
                                        autoComplete="new-password"
                                        required
                                    />
                                    <span
                                        className="input-group-text bg-white"
                                        role="button"
                                        onClick={() => setShowConfirmPassword(v => !v)}
                                        onMouseDown={(e) => e.preventDefault()}
                                        aria-label={showConfirmPassword ? "Hide password" : "Show password"}
                                        title={showConfirmPassword ? "Hide password" : "Show password"}
                                        style={{ cursor: "pointer" }}
                                    >
                                        <i className={showConfirmPassword ? "bi bi-eye-slash" : "bi bi-eye"}/>
                                    </span>
                                </div>                               
                            </div>
                            <button 
                                className="btn btn-primary"
                                type="submit"
                                disabled={isSubmitting}
                            >
                                Set new password
                            </button>
                        </form>
                    )}

                    <hr/>                    
                    
                    <Link to="/login">
                        Back to sign in
                    </Link>
                </div>
            </div>
        </div>
    );
}