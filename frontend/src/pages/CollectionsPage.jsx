import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { createCollection, deleteCollection, getCollection, getCollections, getToken, me } from "../api";

export default function CollectionsPage() {
    const navigate = useNavigate();
    
    const [token] = useState(getToken() || "");
    const [meData, setMeData] = useState(null);
    const [status, setStatus] = useState("");
    const [error, setError] = useState("");

    const [collections, setCollections] = useState([]);
    const [loading, setLoading] = useState(true);

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

    async function loadCollections() {
            try {
                setLoading(true);
                setError("");
                const data = await getCollections();
                setCollections(data);
            } catch (err) {
                setError(err.message);            
            } finally {
                setLoading(false);
            }        
    }

    async function onCreateCollection() {
            const id = await createCollection();
            navigate(`/collections/${id}`);
    }

    async function onDeleteCollection(id) {
            if (!confirm("Delete collection?")) {
                return;
            }
            try {
                await deleteCollection(id);
                await loadCollections();                
            } catch (err) {
                setError(err.message)
            }            
    }

    useEffect(() => {
            if (!token) {
                navigate("/login", { replace: true });
                return;
            }
            loadMe();
            loadCollections();            
            // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return(
        <div className="container py-4" style={{maxWidth: 900}}>
            <div className="d-flex align-items-center justify-content-between mb-3">
                <h2 className="mb-0">My Collections</h2>
                <button 
                    className="btn btn-primary" 
                    onClick={onCreateCollection}
                >
                    Create Collection
                </button>
            </div>            
            <hr/>

            {error ? <div className="alert alert-danger">{error}</div> : null}
            {status ? <div className="alert alert-info">{status}</div> : null}

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
                                    <div className="text-muted small">
                                        {new Date(c.createdAtUtc).toLocaleString()}
                                    </div>
                                    <div className="text-muted small">
                                        Photos: {c.totalPhotos}
                                    </div>
                                    <div className="text-muted small mb-2">
                                        Size:{" "}{(c.totalBytes / (1024 * 1024)).toFixed(2)} MB
                                    </div>
                                    <div className="mt-auto d-flex gap-2">
                                        <button
                                            className="btn btn-primary"
                                            onClick={() => navigate(`/collections/${c.id}`)}
                                        >
                                            Open
                                        </button>
                                        <button                                           
                                            className="btn btn-danger"
                                            onClick={() => onDeleteCollection(c.id)}
                                        >
                                            Delete
                                        </button>
                                    </div>
                                </div>                                
                            </div>
                        </div>
                    ))}
                </div>
            )}


        </div>
    );
}