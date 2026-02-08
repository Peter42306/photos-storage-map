import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { login, setToken } from "../api";

export default function LoginPage() {
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");

    const [status, setStatus] = useState("");
    const [error, setError] = useState("");

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
        <div className="container py-4">
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Login</h2>

                    {error && (
                        <div className="alert alert-danger py-2">{error}</div>
                    )}

                    {status && (
                        <div className="alert alert-info py-2">{status}</div>
                    )}

                    <form onSubmit={handleLogin}>
                        <div className="mb-3">
                            <label className="form-label">Email</label>
                            <input
                                className="form-control"
                                type="email"
                                value={email}                                
                                onChange={(e) => setEmail(e.target.value)}
                                autoComplete="email"
                                required
                            />
                        </div>

                        <div className="mb-3">
                            <label className="form-label">Password</label>
                            <input
                                className="form-control"
                                type="password"
                                value={password}                                
                                onChange={(e) => setPassword(e.target.value)}
                                autoComplete="current-password"
                                required
                            />
                        </div>

                        <button
                            className="btn btn-primary"
                            type="submit"
                        >
                            Login
                        </button>
                    </form>
                </div>
            </div>
        </div>
    );
}