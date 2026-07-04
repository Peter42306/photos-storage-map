import { Link, NavLink, useNavigate } from "react-router-dom";
import { clearToken, getToken, me } from "../api";
import { useEffect, useState } from "react";
import { HashLink } from "react-router-hash-link";

export default function Navbar(){
    const navigate = useNavigate();
    const token = getToken();

    const [userEmail, setUserEmail] = useState("");
    const [isAdmin, setIsAdmin] = useState(false);

    function logout(){
        clearToken();
        setUserEmail("");
        setIsAdmin(false);
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

                const roles = data.roles ?? data.Roles ?? [];
                setIsAdmin(roles.includes("Admin"));
            })
            .catch(() =>{
                clearToken();
                setUserEmail("");
                setIsAdmin(false);
                navigate("/login", { replace: true });
            })
    }, [token, navigate])

    return(
        <nav className="navbar navbar-expand-lg bg-white border-bottom">
            <div className="container">
                <Link className="navbar-brand fw-semibold" to="/">
                <i className="bi bi-camera fs-5 text-primary"> </i>
                    PhotoMap
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
                            <HashLink smooth className="nav-link" to="/#home">
                                Home
                            </HashLink>                            
                        </li>
                        <li className="nav-item">
                            <HashLink smooth className="nav-link" to="/#about">
                                About
                            </HashLink>                            
                        </li>
                        <li className="nav-item">
                            <HashLink smooth className="nav-link" to="/#features">
                                Features
                            </HashLink>                            
                        </li>
                        <li className="nav-item">
                            <HashLink smooth className="nav-link" to="/#how-it-works">
                                How It Works
                            </HashLink>                            
                        </li>
                        <li className="nav-item">
                            <HashLink smooth className="nav-link" to="/#faq">
                                FAQ
                            </HashLink>                            
                        </li>
                        <li className="nav-item">
                            <HashLink smooth className="nav-link" to="/#contact">
                                Contact
                            </HashLink>                            
                        </li>
                        {token ? (
                            <>
                                {/* <li className="nav-item">
                                    <NavLink className="nav-link" to="/dashboard">
                                        Dashboard
                                    </NavLink>
                                </li> */}
                                {/* <li className="nav-item">
                                    <NavLink className="nav-link" to="/upload-test">
                                        Upload Test
                                    </NavLink>
                                </li> */}
                                <li className="nav-item">
                                    <NavLink className="nav-link" to="/collections">
                                        My Collections
                                    </NavLink>
                                </li>
                                {isAdmin && (
                                    <li className="nav-item">
                                        <NavLink className="nav-link" to="/admin">
                                            Admin Panel
                                        </NavLink>
                                    </li>
                                )}
                                
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
                                <span>
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