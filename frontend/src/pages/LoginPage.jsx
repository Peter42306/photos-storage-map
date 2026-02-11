import { useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { login, setToken } from "../api";

export default function LoginPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const [status, setStatus] = useState("");
    const [error, setError] = useState("");

    const [showPassword, setShowPassword] = useState(false);

    const navigate = useNavigate();

    async function handleLogin(e) {
        e.preventDefault();

        setError("");
        setStatus("Logging in ...");

        try {
            const res = await login(email, password);

            setToken(res.accessToken);

            setStatus("");
            navigate("/dashboard");
        } catch (ex) {
            setStatus("");
            setError(ex.message);
        }
    }

    return(
        <div className="container py-4" style={{maxWidth: 560}}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Log in</h2>
                    <hr/>

                    {error && <div className="alert alert-danger py-2">{error}</div>}
                    {status && <div className="alert alert-info py-2">{status}</div>}

                    <form onSubmit={handleLogin}>
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

                        <div className="mb-3">
                            <label className="form-label">Password</label>
                                <div className="input-group">
                                    <input
                                        className="form-control"
                                        type={showPassword ? "text" : "password"}
                                        name="password"
                                        value={password}                                
                                        onChange={(e) => setPassword(e.target.value)}
                                        autoComplete="current-password"
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
                                    {/* <button
                                        type="button"
                                        className="btn btn-outline-secondary border"
                                        onClick={() => setShowPassword(v => !v)}
                                        onMouseDown={(e) => e.preventDefault()}
                                        tabIndex={-1}
                                        aria-label={showPassword ? "Hide password" : "Show password"}                                
                                        title={showPassword ? "Hide password" : "Show password"}
                                    >
                                        <i className={showPassword ? "bi bi-eye-slash" : "bi bi-eye"}/>                                
                                    </button> */}
                                </div>

                            
                        </div>

                        <button
                            className="btn btn-primary"
                            type="submit"
                        >
                            Log in
                        </button>

                        
                    </form>

                    <hr/>

                    <div className="mb-2">
                        <Link to="/forgot-password">
                            Forgot your password?
                        </Link>                        
                    </div>

                    <div className="mb-2">
                        <Link to="/register">
                            Register as a new user
                        </Link>                        
                    </div>

                    <div className="mb-2">
                        <Link to="/resend-confirmation">
                            Resend email confirmation
                        </Link>                        
                    </div>

                    

                </div>
            </div>
        </div>
    );
}