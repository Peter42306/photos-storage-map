import { useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import { createCollection, deleteCollection, getCollection, getCollections, getStorageSummary, getThumbUrl, getToken, me } from "../api";

function formatBytes(bytes) {
    if (!bytes) return "0 B";

    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));

    return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
}

function formatTakenAt(dateString) {
    if (!dateString) return "";

    const date = new Date(dateString);

    if (isNaN(date)) return "";

    return new Intl.DateTimeFormat("en-GB", {
        day: "2-digit",
        month: "long",
        year: "numeric",
        // hour: "2-digit",
        // minute: "2-digit",
        // hour12: false
    }).format(date);
    // .replace(",", " at");
    
}

export default function CollectionsPage() {
    const navigate = useNavigate();
    
    // const [token] = useState(getToken() || "");
    const [meData, setMeData] = useState(null);
    const [status, setStatus] = useState("");
    const [error, setError] = useState("");

    const [collections, setCollections] = useState([]);
    const [loading, setLoading] = useState(true);

    const [deletingId, setDeletingId] = useState(null);

    const [summary, setSummary] = useState(null);

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

                const [collectionsData, summaryData] = await Promise.all([
                    getCollections(),
                    getStorageSummary()
                ]);

                // const data = await getCollections();
                setCollections(collectionsData ?? []);
                setSummary(summaryData);
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

    // async function onDeleteCollection(id) {
    //         if (!confirm("Delete collection?")) {
    //             return;
    //         }
    //         try {
    //             await deleteCollection(id);
    //             await loadCollections();                
    //         } catch (err) {
    //             setError(err.message)
    //         }            
    // }

    async function onDeleteCollection(id) {
        if (!confirm("Delete collection with all photos & archives?")) {
            return;
        }

        try {
            setDeletingId(id);
            await deleteCollection(id);
            await loadCollections();

        } catch (err) {
            alert(err.message)
        } finally {
            setDeletingId(null);
        }        
    }

    useEffect(() => {
        const handler = (e) => {
            if (deletingId) {
                e.preventDefault();
                e.returnValue = "";
            }
        }

        window.addEventListener("beforeunload", handler);
        return () => window.removeEventListener("beforeunload", handler);
    }, [deletingId]);
    

    useEffect(() => {
        const token = getToken();

        if (!token) {
            navigate("/login", { replace: true });
            return;
        }
            loadMe();
            loadCollections();            
            // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    return(
        <div className="container py-4">
        {/* <div className="container py-4" style={{maxWidth: 900}}> */}
            <div className="d-flex align-items-center justify-content-between">
                <h2 className="mb-0">My Collections</h2>
                <button 
                    className="btn btn-primary" 
                    disabled={!!deletingId}
                    onClick={onCreateCollection}
                >
                    Create Collection
                </button>
            </div>            
            <hr/>

            {error ? <div className="alert alert-danger">{error}</div> : null}
            {status ? <div className="alert alert-info">{status}</div> : null}

            {summary && (
                <div className="d-flex align-items-start justify-content-between small mb-3">
                    <div>                        
                        Photos: {summary.totalPhotos ?? summary.TotalPhotos ?? 0} / {formatBytes(summary.totalPhotosBytes ?? summary.TotalPhotosBytes ?? 0)}<br/>
                        Archives: {summary.totalArchives ?? summary.TotalArchives ?? 0} / {formatBytes(summary.totalArchivesBytes ?? summary.TotalArchivesBytes ?? 0)}<br/>
                    </div>
                    <div className="text-end">                        
                        Collections: {summary.totalCollections ?? summary.TotalCollections ?? 0} / {formatBytes(summary.totalStorageBytes ?? summary.totalStorageBytes ?? 0)}<br/>
                    </div>
                </div>
            )}            

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
                                    <hr/>
                                    <div className="text-muted small">
                                        Created: {formatTakenAt (new Date(c.createdAtUtc).toLocaleString())}
                                    </div>
                                    {/* <div className="text-muted small">
                                        Id: {c.id}
                                    </div> */}
                                    {/* {c.totalPhotos > 0 && (
                                        <div className="text-muted small">
                                            Photos: {c.totalPhotos} / {formatBytes(c.totalBytes)}
                                        </div>
                                    )} */}
                                    
                                    <div className="text-muted small">
                                        {c.totalPhotos > 0 ? (
                                            <>Photos: {c.totalPhotos} / {formatBytes(c.totalBytes)}</>
                                        ) : (
                                            <>Photos: 0</>
                                        )}                                        
                                    </div>                                    
                                    <div className="text-muted small mb-2">                                        
                                        {c.totalArchives> 0 ? (
                                            <>Archives: {c.totalArchives} / {formatBytes(c.totalArchivesBytes)}</>
                                        ) : (
                                            <>Archives: 0</>
                                        )}                                                                                
                                    </div>
                                    <hr/>
                                    <div className="mt-auto d-flex gap-2">
                                        <button
                                            className="btn btn-primary"
                                            disabled={!!deletingId}
                                            onClick={() => navigate(`/collections/${c.id}`)}
                                        >
                                            Open
                                        </button>
                                        <button
                                            className="btn btn-danger"
                                            disabled={!!deletingId}
                                            onClick={() => onDeleteCollection(c.id)}
                                        >
                                            Delete
                                        </button>
                                        {/* <button                                           
                                            className="btn btn-danger"
                                            onClick={() => onDeleteCollection(c.id)}
                                        >
                                            Delete
                                        </button> */}
                                    </div>
                                </div>                                
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {deletingId && (
            <div
                style={{
                position: "fixed",
                inset: 0,                
                zIndex: 9999,
                display: "flex",
                alignItems: "center",
                justifyContent: "center",
                }}
                //onClick={(e) => e.stopPropagation()}
            >
                <div className="alert alert-danger">
                Deleting collection… please wait
                </div>
            </div>
            )}


        </div>
    );
}