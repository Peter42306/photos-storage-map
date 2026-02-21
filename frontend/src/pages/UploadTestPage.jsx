import { useState } from "react"
import { completeUpload, createCollection, initUpload, putToPresignedUrl } from "../api";

export default function UploadTestPage() {
    const [collectionId, setCollectionId] = useState("");
    const [photoId, setPhotoId] = useState("");
    const [uploadUrl, setUploadUrl] = useState("");
    const [file, setFile] = useState(null);
    const [log, setLog] = useState([]);

    const addLog = (m) => setLog((x) => [...x, `[${new Date().toLocaleTimeString()}] ${m}`]);

    async function onCreateCollection() {
        try {
            addLog("Creating collection...");
            const id = await createCollection();

            setCollectionId(id);
            addLog(`Collection created: ${id}`);
        } catch (err) {
            addLog(`ERROR createCollection: ${err.message}`);
        }
    }

    async function onInitUpload() {
        try {
            if (!collectionId) {
                addLog("No collectionId");
                return;
            }

            addLog("Init upload (get presigned URL)...");
            const data = await initUpload(collectionId);
            setPhotoId(data.photoId);
            setUploadUrl(data.uploadUrl);
            addLog(`Init OK. photoId=${data.photoId}`);
        } catch (err) {
            addLog(`ERROR initUpload: ${err.message}`);
        }
    }

    async function onPutToS3() {
        try {
            if (!uploadUrl) {
                return addLog("No uploadUrl");
            }

            if (!file) {
                return addLog("choose a file first");
            }

            addLog(`PUT to S3: ${file.name} (${file.size} bytes)...`);
            const res = await putToPresignedUrl(uploadUrl, file);
            addLog(`PUT OK. status=${res.status} etag=${res.etag ?? "-"}`);
        } catch (err) {
            addLog(`ERROR PUT: ${err.message}`);
        }
    }

    async function onComplete() {
        try {
            if (!photoId) {
                return addLog("No photoId");
            }

            addLog("Complete upload (enqueue worker)...");
            await completeUpload(photoId);
            addLog("Complete OK. Worker started")
        } catch (err) {
            addLog(`ERROR complete: ${err.message}`);
        }
    }

    return(
        <div className="container py-4" style={{ maxWidth: 900 }}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="mb-3">Upload Test</h2>
                    <hr className="mb-4"/>

                    <div className="d-grid gap-3">
                        <div className="d-flex align-items-center gap-3">
                            <button className="btn btn-primary w-50" onClick={onCreateCollection}>
                                Create Collection
                            </button>
                            <span className="text-muted">
                                collectionId: {collectionId || "-"}
                            </span>
                        </div>

                        <div className="d-flex align-items-center gap-3">
                            <button className="btn btn-primary w-50" onClick={onInitUpload} disabled={!collectionId}>
                                Init Upload
                            </button>
                            <span className="text-muted">
                                photoId: {photoId || "-"}
                            </span>
                        </div>

                        <div>
                            <input
                                className="form-control w-75"
                                type="file"
                                accept="image/*"
                                onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                            />
                        </div>

                        <button
                            className="btn btn-success w-50"
                            onClick={onPutToS3}
                            disabled={!uploadUrl || !file}
                        >
                            Upload to S3
                        </button>

                        <button
                            className="btn btn-warning w-50"
                            onClick={onComplete}
                            disabled={!photoId}
                        >
                            Complete (enqueue worker)
                        </button>

                        <hr/>

                        <h5>Log</h5>

                        <div>
                            {log.length === 0 ? (
                                <span>No logs yet</span>
                            ) : (
                                log.map((l, i) => <div key={i}>{l}</div>)
                            )}
                        </div>

                    </div>
                </div>

            </div>
            
        </div>
    );    
}