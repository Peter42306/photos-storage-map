import { Link } from "react-router-dom";

export default function LandingPage(){
    return(
        <div className="container py-5" style={{ maxWidth: 980 }}>
            <div className="row align-items-center g-4">
                <div className="col-12 col-md-7">                    
                    <h1 className="display-5 fw-semibold mb-3">Landing page</h1>
                    <p className="lead text-muted ">Upload photos, keep them organized, and view them on a map using geolocation.</p>

                    <div className="d-flex gap-2 flex-wrap">
                        <Link 
                            className="btn btn-outline-primary"
                            to="/register"
                            title="Register a new user"
                        >
                            Get started
                        </Link>
                        {/* <Link className="btn btn-outline-secondary" to="/login">Sign in</Link> */}
                    </div>
                </div>
                <div className="col-12 col-md-5">
                    <div className="card shadow-sm">
                        <div className="card-body">
                            <h5 className="card-title">How it works</h5>
                            <ol className="mb-0 text-muted">
                                <li>Create an accout</li>
                                <li>Confirm email</li>
                                <li>Sign in and use the app</li>
                            </ol>
                        </div>
                    </div>
                </div>                
            </div>
            
        </div>
    );
}