import { useState } from "react";
import { register } from "../api";
import { Link } from "react-router-dom";

export default function RegisterPage() {
    const [fullName, setFullName] = useState("");
    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [confirmPassword, setConfirmPassword] = useState("");

    const [status, setStatus] = useState("");
    const [error, setError] = useState("");

    async function handleRegister(e){
        e.preventDefault();

        setError("");
        setStatus("");

        if (password !== confirmPassword) {
            setError("Passwords do not match");
            return;
        }

        setStatus("Registering...");

        try {
            const res = await register(email, password, fullName);
            setStatus(res?.message ?? "Registration successful. Please confirm your email.")
        } catch (ex) {
            setStatus("");
            setError(ex.message);
        }
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
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}                                
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
                                required
                            />
                        </div>
                        <div className="mb-3">
                            <label className="form-label">Confirm Password</label>
                            <input
                                className="form-control"
                                type="password"
                                value={confirmPassword}
                                onChange={(e) => setConfirmPassword(e.target.value)}                                
                                required
                            />
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
                        >
                            Create account
                        </button>

                        <div className="mt-3">
                            <Link to="/login">Back to login</Link>
                        </div>
                    </form>
                </div>
            </div>
            

            

            
        </div>
    );
}