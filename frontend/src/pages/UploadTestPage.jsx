import { useMemo, useState } from "react"
import { completeUpload, createCollection, initUpload, putToPresignedUrl } from "../api";
// import { redirect } from "react-router-dom";

export default function UploadTestPage() {
    const [collectionId, setCollectionId] = useState("");
    const [files,setFiles] = useState([]);
    const [isUploading, setIsUploading] = useState(false);

    const [lastPhotoId, setLastPhotoId] = useState("");
    // const [photoId, setPhotoId] = useState("");
    // const [uploadUrl, setUploadUrl] = useState("");
    // const [file, setFile] = useState(null);
    const [log, setLog] = useState([]);

    const totalBytes = useMemo(() => files.reduce((s, f) => s + (f?.size ?? 0), 0), [files]);

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

    async function onUploadAll() {
        if (!collectionId) {
            return addLog("No collectionId");
        }

        if (files.length === 0) {
            return addLog("Choose files first");
        }

        setIsUploading(true);
        try {
            addLog(`Start uploading ${files.length} files...`);

            for (let i = 0; i < files.length; i++) {
                const f = files[i];
                
                addLog(`(${i + 1}/${files.length}) Init upload for: ${f.name}`);
                const { photoId, uploadUrl } = await initUpload(collectionId);
                

                addLog(`PUT to S3: ${f.name} (${f.size} bytes) photoId=${photoId}`);
                const putRes = await putToPresignedUrl(uploadUrl, f);
                addLog(`PUT result: status=${putRes.status} etag=${putRes.etag ?? "-"}`);

                if (putRes.status < 200 || putRes.status >= 300) {
                    throw new Error(`PUT failed for ${f.name}: status:${putRes.status}`);                    
                }

                addLog(`Complete (enqueue worker) photoId=${photoId}`);
                
                await completeUpload(photoId);
                setLastPhotoId(photoId);
                addLog(`OK - queued: ${photoId}`);
            }

            addLog("DONE - all files uploaded & queued");
        } catch (err) {
            addLog(`ERROR uploadAll: ${err.message}`);
        } finally {
            setIsUploading(false);
        }
    }

    function onClear() {
        setFiles([]);
        setLastPhotoId("");
        setLog([]);
    }



    // Function for 1 file upload
    // async function onInitUpload() {
    //     try {
    //         if (!collectionId) {
    //             addLog("No collectionId");
    //             return;
    //         }

    //         addLog("Init upload (get presigned URL)...");
    //         const data = await initUpload(collectionId);
    //         setPhotoId(data.photoId);
    //         setUploadUrl(data.uploadUrl);
    //         addLog(`Init OK. photoId=${data.photoId}`);
    //     } catch (err) {
    //         addLog(`ERROR initUpload: ${err.message}`);
    //     }
    // }

    // Function for 1 file upload
    // async function onPutToS3() {
    //     try {
    //         if (!uploadUrl) {
    //             return addLog("No uploadUrl");
    //         }

    //         if (!file) {
    //             return addLog("choose a file first");
    //         }

    //         addLog(`PUT to S3: ${file.name} (${file.size} bytes)...`);
    //         const res = await putToPresignedUrl(uploadUrl, file);
    //         addLog(`PUT OK. status=${res.status} etag=${res.etag ?? "-"}`);
    //     } catch (err) {
    //         addLog(`ERROR PUT: ${err.message}`);
    //     }
    // }

    // async function onComplete() {
    //     try {
    //         if (!photoId) {
    //             return addLog("No photoId");
    //         }

    //         addLog("Complete upload (enqueue worker)...");
    //         await completeUpload(photoId);
    //         addLog("Complete OK. Worker started")
    //     } catch (err) {
    //         addLog(`ERROR complete: ${err.message}`);
    //     }
    // }

    return(
        <div className="container py-4" style={{ maxWidth: 900 }}>
            <div className="card shadow-sm">
                <div className="card-body">
                    <h2 className="mb-3">Upload Test</h2>
                    <hr className="mb-4"/>

                    <div className="d-flex flex-wrap align-items-center gap-3 mb-3">
                        <button
                            className="btn btn-primary w-50"
                            onClick={onCreateCollection}
                            disabled={isUploading}
                        >
                            Create Collection
                        </button>
                        <div className="text-muted">
                            collectionId: {collectionId || "-"}
                        </div>
                    </div>

                    <div className="mb-3">
                        <label className="form-label">Choose images</label>
                        <input
                            className="form-control"
                            type="file"
                            accept="image/*"
                            multiple
                            disabled={!collectionId || isUploading}
                            onChange={(e) => setFiles(Array.from(e.target.files ?? []))}
                        />
                        <div className="form-text">
                            Selected: {files.length} files, total{" "}{(totalBytes / (1024 * 1024)).toFixed(2)} MB
                        </div>
                    </div>

                    <div className="d-flex flex-wrap gap-2 mb-3">
                        <button
                            className="btn btn-success w-50"
                            onClick={onUploadAll}
                            disabled={!collectionId || files.length === 0 || isUploading}
                        >
                            {isUploading ? "Uploading..." : "Upload ALL to S3 + Complete"}
                        </button>
                        <button
                            className="btn btn-secondary w-25"
                            onClick={onClear}
                            disabled={isUploading}
                        >
                            Clear
                        </button>
                    </div>

                    <div>
                        Last photoId: {lastPhotoId || "-"}
                    </div>

                    <hr/>

                    <h5>Log</h5>

                    <div className="form-text">
                        {log.length === 0 ? (
                            <div>No logs yet</div>
                        ) : (
                            log.map((l, i) => <div key={i}>{l}</div>)
                        )}
                    </div>

                    {/* <div className="d-grid gap-3">
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

                    </div> */}
                </div>

            </div>
            
        </div>
    );    
}