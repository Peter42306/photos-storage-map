import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { clearToken, getToken, me } from "../api";

export default function DashboardPage() {
    const navigate = useNavigate();
    
    const [token] = useState(getToken() || "");
    const [meData, setMeData] = useState(null);
    const [status, setStatus] = useState("");
    const [error, setError] = useState("");

    async function loadMe() {
        setError("");
        setStatus("Loading /api/me...");

        try {
            const data = await me();
            setMeData(data);
            setStatus("OK");            
        } catch (ex) {
            setMeData(null);
            setStatus("");            
            setError(ex.message);
        }
    }

    function logout() {
        clearToken();
        setMeData(null);
        setStatus("Logged out");
        setError("");
        navigate("/login", { replace: true });
    }

    useEffect(() => {
        if (!token) {
            navigate("/login", { replace: true });
            return;
        }
        loadMe();
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    
    return(
        <div className="container py-4" style={{maxWidth: 560}}>
            <div className="d-flex align-items-center justify-content-between mb-3">
                <h2 className="mb-0">Dashboard</h2>

                <button className="btn btn-outline-danger" onClick={logout}>
                    Logout
                </button>
            </div>            

            {status && (<div className="alert alert-info py-2">{status}</div>)}
            {error && (<div className="alert alert-danger py-2">{error}</div>)}

            <div className="card shadow-sm">
                <div className="card-body">
                    <div className="d-flex gap-2 flex-wrap">
                        <button
                            className="btn btn-outline-primary"
                            onClick={loadMe}
                        >
                            Reload /api/me
                        </button>

                        <button
                            className="btn btn-outline-secondary"
                            onClick={() => navigator.clipboard.writeText(token)}
                            title="Copy token to clipboard"
                        >
                            Copy token
                        </button>
                    </div>

                    <hr/>

                    <h5 className="card-title">Token</h5>
                    <pre className="bg-light border rounded p-2 mt-2 small">
                        {token ? token : "-"}
                    </pre>

                    <h5 className="card-title">/api/me response</h5>
                    <pre className="bg-light border rounded p-2 mt-2 small">
                        {meData ? JSON.stringify(meData,null,2) : "-"}
                    </pre>
                </div>
            </div>
        </div>
    );
}