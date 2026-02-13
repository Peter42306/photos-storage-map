import { useState } from "react";
import { register } from "../api";
import { Link, useNavigate } from "react-router-dom";

export default function RegisterPage() {
    const [fullName, setFullName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const [status, setStatus] = useState("");
    const [error, setError] = useState("");
    const [isSubmitting, setIsSubmitting] = useState(false);    

    const [showPassword, setShowPassword] = useState(false);
    const [showConfirmPassword, setShowConfirmPassword] = useState(false);

    const navigate = useNavigate();

    async function handleRegister(e){
        e.preventDefault();

        if (isSubmitting) {
            return;
        }

        setError("");
        setStatus("");

        if (password !== confirmPassword) {
            setError("Passwords do not match");
            return;
        }

        setIsSubmitting(true);
        setStatus("Registering...");

        try {
            await register(email, password, fullName);
            navigate("/check-email", { state: { email }});
        } catch (ex) {
            setStatus("");
            setError(ex.message);            
        } finally {
            setIsSubmitting(false);
        }

        // try {
        //     const res = await register(email, password, fullName);
        //     setStatus(res?.message ?? "Registration successful. Please confirm your email.")
        // } catch (ex) {
        //     setStatus("");
        //     setError(ex.message);
        // }
    }

    return(
        <div className="container py-4" style={{ maxWidth: 560 }}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Register</h2>
                    <h4 className="card-title mb-3">Create a new account</h4>
                    <hr/>
                    
                    {error && <div className="alert alert-danger py-2">{error}</div>}
                    {status && <div className="alert alert-info py-2">{status}</div>}

                    <form onSubmit={handleRegister}>                        
                        <div className="mb-3">
                            <label className="form-label">Email</label>
                            <input
                                className="form-control"
                                type="email"
                                name="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}                                
                                required
                            />
                        </div>
                        <div className="mb-3">
                            <label className="form-label">Password</label>
                            <div className="input-group">
                                <input
                                    className="form-control"
                                    type={showPassword ? "text" : "password"}
                                    name="password"
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
                                    <i className={showPassword ? "bi bi-eye-slash" : "bi bi-eye"} />
                                </span>
                            </div>                            
                        </div>
                        <div className="mb-3">
                            <label className="form-label">Confirm Password</label>
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
                                    <i className={showConfirmPassword ? "bi bi-eye-slash" : "bi bi-eye"} />
                                </span>
                            </div>                            
                        </div>
                        <div className="mb-3">
                            <label className="form-label">Full Name (optional)</label>
                            <input
                                className="form-control"
                                type="text"
                                value={fullName}
                                onChange={(e) => setFullName(e.target.value)}                                
                            />
                        </div>                        
                        <button
                            className="btn btn-primary"
                            type="submit"
                            disabled={isSubmitting}
                        >
                            Create account
                        </button>                        
                    </form>

                    <hr/>
                    <div className="mt-3">
                        <Link to="/">Back to Home</Link>
                    </div>
                    
                </div>
            </div>            
        </div>
    );
}