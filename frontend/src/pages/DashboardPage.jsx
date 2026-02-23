import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { clearToken, createCollection, deleteCollection, getCollections, getToken, me } from "../api";

export default function DashboardPage() {
    const navigate = useNavigate();
    
    const [token] = useState(getToken() || "");
    const [meData, setMeData] = useState(null);
    const [status, setStatus] = useState("");
    const [error, setError] = useState("");

    // const [collections, setCollections] = useState([]);
    // const [loading, setLoading] = useState(true);


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

    // function logout() {
    //     clearToken();
    //     setMeData(null);
    //     setStatus("Logged out");
    //     setError("");
    //     navigate("/login", { replace: true });
    // }

    useEffect(() => {
        if (!token) {
            navigate("/login", { replace: true });
            return;
        }
        loadMe();
        // loadCollections();
        
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);
    
    // async function loadCollections() {
    //     try {
    //         setLoading(true);
    //         const data = await getCollections();
    //         setCollections(data);
    //     } catch (err) {
    //         setError(err.message);            
    //     } finally {
    //         setLoading(false);
    //     }        
    // }

    // async function onCreateCollection() {
    //     const id = await createCollection();
    //     navigate(`/collections/${id}`);
    // }

    // async function onDeleteCollection(id) {
    //     if (!confirm("Delete collection?")) {
    //         return;
    //     }

    //     await deleteCollection(id);
    //     await loadCollections();
    // }

    


    
    return(
        <div className="container py-4" style={{maxWidth: 560}}>
            {/* <div className="d-flex align-items-center justify-content-between mb-3">
                <h2 className="mb-0">My Collections</h2>
                <button
                    className="btn btn-primary"
                    onClick={onCreateCollection}
                >
                    Create Collection
                </button>
            </div>            
            <hr/>

            {loading ? (
                <div>Loading...</div>
            ) : collections.length === 0 ? (
                <div>No collections yet</div>
            ) : (
                <div className="row">
                    {collections.map ((c) => (
                        <div key={c.id} className="col-md-4 mb-3">
                            <div className="card shadow-sm h-100">
                                <div className="card-body d-flex flex-column">
                                    <h5 className="card-title"
                                    >
                                        {c.title || "Untitled"}
                                    </h5>
                                    <div>
                                        {new Date(c.reatedAtUtc).toLocaleString()}
                                    </div>
                                    <div>
                                        Photos: {c.totalPhotos}
                                    </div>
                                    <div>
                                        Size:{" "}{(c.totalBytes / (1024 * 1024)).toFixed(2)} MB
                                    </div>
                                    <div>
                                        <button
                                            className="btn btn-primary"
                                            onClick={() => navigate(`/collections/${c.id}`)}
                                        >
                                            Open
                                        </button>
                                        <button
                                            type="button"
                                            className="btn-close"
                                            aria-label="Close"
                                        >
                                        </button>
                                    </div>
                                </div>                                
                            </div>
                        </div>
                    ))}
                </div>
            )} */}


            
            {/* !!! Legace code for testing */}
            <div className="d-flex align-items-center justify-content-between mb-3">
                <h2 className="mb-0">Dashboard</h2>
                

                {/* <button className="btn btn-outline-danger" onClick={logout}>
                    Logout
                </button> */}
            </div>            

            <hr/>

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