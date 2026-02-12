import { Link } from "react-router-dom";

export default function ResetPasswordPage() {
    return(
        <div className="container py-4" style={{maxWidth: 560}}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Reset your password?</h2>
                    <hr/>
                    <p className="text-muted">
                        ??? Enter your email and we will send you a link to reset your password.
                    </p>

                    <hr/>
                    <Link to="/login">
                        Back to sign in
                    </Link>
                </div>
            </div>
        </div>
    );
}