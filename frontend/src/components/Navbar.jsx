import { Link, NavLink, useNavigate } from "react-router-dom";
import { clearToken, getToken, me } from "../api";
import { useEffect, useState } from "react";

export default function Navbar(){
    const navigate = useNavigate();
    const token = getToken();

    const [userEmail, setUserEmail] = useState("");

    function logout(){
        clearToken();
        navigate("/login", {replace: true});
    }

    // eslint-disable-next-line react-hooks/exhaustive-deps
    useEffect(() => {
        if(!token){
            setUserEmail("");
            return;
        }

        me()
            .then((data) => {
                setUserEmail(data.email);
            })
            .catch(() =>{
                clearToken();
                setUserEmail("");
                navigate("/login", { replace: true });
            })
    }, [token, navigate])

    return(
        <nav className="navbar navbar-expand-lg bg-white border-bottom">
            <div className="container">
                <Link className="navbar-brand fw-semibold" to="/">
                    PhotosStorageMap
                </Link>

                <button
                    className="navbar-toggler"
                    type="button"
                    data-bs-toggle="collapse"
                    data-bs-target="#nav"
                    aria-controls="nav"
                    aria-expanded="false"
                    aria-label="Toggle navigation"
                >
                    <span className="navbar-toggler-icon"/>
                </button>

                <div className="collapse navbar-collapse" id="nav">
                    <ul className="navbar-nav me-auto mb-2 mb-lg-0">
                        <li className="nav-item">
                            <NavLink className="nav-link" to="/">
                                Home
                            </NavLink>                            
                        </li>
                        {token ? (
                            <>
                                <li className="nav-item">
                                    <NavLink className="nav-link" to="/dashboard">
                                        Dashboard
                                    </NavLink>
                                </li>
                                <li className="nav-item">
                                    <NavLink className="nav-link" to="/upload-test">
                                        Upload Test
                                    </NavLink>
                                </li>
                                <li className="nav-item">
                                    <NavLink className="nav-link" to="/collections">
                                        My Collections
                                    </NavLink>
                                </li>
                            </>                            
                        ) : null}
                    </ul>

                    <div className="d-flex gap-2">
                        {!token ? (
                            <>
                                <Link className="nav-link" to="/login">
                                    Sign in
                                </Link>
                                {/* <Link className="nav-link" to="/register">
                                    Create account
                                </Link> */}
                            </>
                        ) : (
                            <div className="d-flex flex-column flex-lg-row align-items-start align-items-lg-center gap-3">
                                <span >
                                    {userEmail} logged in
                                </span>
                                <button className="nav-link" onClick={logout}>
                                    Log out
                                </button>
                            </div>                            
                        )}
                    </div>
                </div>
            </div>
        </nav>
    );
}