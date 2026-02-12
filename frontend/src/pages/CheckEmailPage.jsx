import { Link, useLocation } from "react-router-dom";

export default function CheckEmailPage() {
    const location = useLocation();
    const email = location.state?.email;

    return(
        <div className="container py-4" style={{ maxWidth: 560 }}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="card-title mb-3">Check Email</h2>
                    <hr/>

                    <div className="alert alert-info py-2">
                        {email ? (
                            <>
                            We sent a confirmation email to <strong>{email}</strong>.
                            </>
                        ) : (
                            <>
                            We sent you a confirmation email.
                            </>
                        )}
                        <br/>
                        Please open it and click the confirmation link.
                    </div>

                    <Link to="/login">Back to sign in</Link>
                </div>
            </div>
        </div>
    );
}