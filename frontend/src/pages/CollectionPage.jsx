import TextareaAutosize from 'react-textarea-autosize';
import React, { useEffect, useRef, useState } from "react";
import { data, useNavigate, useParams } from "react-router-dom";
// import { completeArchiveUpload, completeUpload, createOrUpdateSharedLink, deleteArchive, deleteCollection, deleteOriginalPhotosFromCollection, deletePhoto, downloadCollectionStandardZip, getArchiveDownloadUrl, getCollection, getCollectionArchives, getOriginalDownloadUrl, getOriginalUrl, getPhotoStatus, getSharedLinkByCollectionId, getThumbUrl, getToken, initArchiveUploads, initUpload, putToPresignedUrl, putToPresignedUrlWithProgress, revokeSharedLink, updateArchiveDescription, updateCollection, updatePhotoDescription } from "../api";
import { completeUpload, createOrUpdateSharedLink, deleteOriginalPhotosFromCollection, deletePhoto, downloadCollectionStandardZip, getCollection, getOriginalDownloadUrl, getOriginalUrl, getPhotoStatus, getSharedLinkByCollectionId, getToken, initUpload, putToPresignedUrl, revokeSharedLink, updateCollection, updatePhotoDescription } from "../api";
import Lightbox from 'yet-another-react-lightbox';
import "yet-another-react-lightbox/styles.css";
// import { Slideshow } from 'yet-another-react-lightbox/plugins';
import { Slideshow, Counter, Fullscreen, Zoom } from 'yet-another-react-lightbox/plugins';
import "yet-another-react-lightbox/plugins/counter.css";


//const S3_BASE = "https://hel1.your-objectstorage.com/photos-storage-map";

function formatBytes(bytes) {
    if (!bytes) return "0 B";

    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));

    return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
}

function formatDistance(meters){
        if (meters == null) {
            return "";
        }

        if (meters < 1000) {
            return `${Math.round(meters)} m`;
        }

        return `${(meters / 1000).toFixed(2)} km`;
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

export default function CollectionPage() {
    const { id } = useParams();
    const navigate = useNavigate();

    const [collection, setCollection] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const [isEditing, setIsEditing] = useState(false);
    const [title, setTitle] = useState("");
    const [description, setDescription] = useState("");

    const [uploading, setUploading] = useState(false);
    const [uploadStatus, setUploadStatus] = useState("");    
    const [uploadingOriginals, setUploadingOriginals] = useState(false);
    const cancelUploadRef = useRef(false);

    // const [archives, setArchives] = useState([]);
    // const [archiveUploading, setArchiveUploading] = useState(false);
    // const [archiveUploadProgress, setArchiveUploadProgress] = useState(0);
    // const [archiveUploadStatus, setArchiveUploadStatus] = useState("");

    const [lightboxOpen, setLightboxOpen] = useState(false);
    const [lightboxIndex, setLightboxIndex] = useState(0);
    const [viewerMode, setViewerMode] = useState("standard");
    const [slideshowAutoplay, setSlideshowAutoplay] = useState(true);

    const [shareLink, setShareLink] = useState(null);
    const [allowSlideshowOriginals, setAllowSlideshowOriginals] = useState(false);
    const [allowDownloadResizedZip, setAllowDownloadResizedZip] = useState(false);
    const [allowDownloadOriginalFromCard, setAllowDownloadOriginalFromCard] = useState(false);   
    

    async function load() {
        console.log("load() called, id =", id);

        try {
            setError("");
            setLoading(true);

            // console.log("load() called, before getCollection")
            const data = await getCollection(id);
            //const archivesData = await getCollectionArchives(id);            

            // console.log("load() called, after getCollection, response data: ", data);
            setCollection(data);
            setTitle(data?.title ?? "");
            setDescription(data?.description ?? "");
            // setArchives(data?.archives ?? data?.Archives ?? []);

            const currentSharedLink = await getSharedLinkByCollectionId(id);
            setShareLink(currentSharedLink);

            if (currentSharedLink && !(currentSharedLink.isRevoked ?? currentSharedLink.IsRevoked)) {
                setAllowSlideshowOriginals(currentSharedLink.allowSlideshowOriginals ?? currentSharedLink.AllowSlideshowOriginals ?? false);
                setAllowDownloadResizedZip(currentSharedLink.allowDownloadResizedZip ?? currentSharedLink.AllowDownloadResizedZip ?? false);
                setAllowDownloadOriginalFromCard(currentSharedLink.allowDownloadOriginalFromCard ?? currentSharedLink.AllowDownloadOriginalFromCard ?? false);
            }

        } catch (err) {
            setError(err.message);
            setCollection(null);
            setShareLink(null);
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {        
        const token = getToken();

    if (!token) {
      navigate("/login", { replace: true });
      return;
    }

    if (!id) return; // важно, пока id не появился — не грузим

    // setLoading(true);
        load();    
    }, [id]);

    async function onSave() {
        try {
            setError("");
            await updateCollection(id, title, description);
            setIsEditing(false);
            await load();
        } catch (err) {
            setError(err.message);
        }
    }

    function onCancel(){
        setIsEditing(false);
        setTitle(collection?.title ?? "");
        setDescription(collection?.description ?? "");
    }

    async function runWithConcurrency(items, concurrency, handler, onProgress, shouldCancel) {
        const results = new Array(items.length);
        let index = 0;
        let completed = 0;

        async function worker() {
            while(true){
                if(shouldCancel?.()){
                    return;
                }

                const i = index++;
                
                if (i >= items.length) {
                    return;
                }
                
                try {
                    const result = await handler(items[i], i);

                    results[i] = {
                        success: true,
                        data: result
                    };                    
                    
                } catch (err) {
                    results[i] = {
                        success: false,
                        error: err.message,
                        fileName: items[i]?.name ?? items[i]?.fileName ?? "unknown"
                    };                    
                }

                completed++;

                if (onProgress) {
                    onProgress(completed, items.length);
                }
            }
        }

        const workers = [];

        let workerCount = concurrency;

        if (items.length < concurrency) {
            workerCount = items.length;
        }

        for (let i = 0; i < workerCount; i++) {
            workers.push(worker())
            
        }

        await Promise.all(workers)

        return results;
    }

    async function retryAsync(operation, maxAttempts = 5) {
        for (let attempt = 1; attempt <= maxAttempts; attempt++) {
            try {
                return await operation();
            } catch (err) {
                const isRetryable = 
                    err.status == null || // network error, no status code
                    err.status === 408 || // Request Timeout
                    err.status === 429 || // Too Many Requests
                    err.status >= 500 && err.status < 600; // Server errors (5xx)
                    

                if (!isRetryable || attempt === maxAttempts) throw err;

                //const delay = 1000 * attempt; // exponential backoff: 1s, 2s, 3s, ...
                const delay = 1000 * Math.pow(2, attempt); // exponential backoff: 2s, 4s, 8s, 16s

                await new Promise(resolve => setTimeout(resolve, delay)); // try again after delay
            }
        }
    }

    function cancelUpload() { 
        if(!uploadingOriginals || cancelUploadRef.current) {
            return;
        }
        
        cancelUploadRef.current = true;
        setUploadStatus("Cancelling upload...");
    }

    async function onFilesSelected(e) {
        const UPLOAD_CONCURRENCY = 4;
        const MAX_UPLOAD_BATCH = 500; // 500 
        const MAX_COLLECTION_PHOTOS = 1500; // 1500

        const currentTotalPhotosInCollection = collection.totalPhotos ?? 0;
        const possibleToLoadToCollection = MAX_COLLECTION_PHOTOS - currentTotalPhotosInCollection;        

        const files = Array.from(e.target.files || []);

        const invalidFiles = files.filter(f => {
            const ext = f.name.split('.').pop()?.toLowerCase();
            return ext !== "jpg" && ext !== "jpeg";
        });

        if (invalidFiles.length > 0) {
            setError("Only JPG and JPEG photos are supported. iPhone users: Settings → Camera → Formats → Most Compatible.");
            e.target.value = "";
            return;
        }


        if (files.length === 0) return;

        if (possibleToLoadToCollection <= 0) {
            setError(`Collection limit reached. Maximum allowed ${MAX_COLLECTION_PHOTOS} photos per collection.`)
            e.target.value = "";
            return;
        }

        if (files.length > MAX_UPLOAD_BATCH) {
            setError(`You can upload up to ${MAX_UPLOAD_BATCH} photos at once.`)
            e.target.value = "";
            return;
        }

        if (files.length > possibleToLoadToCollection) {
            setError(`You selected ${files.length} photos, but only ${possibleToLoadToCollection} photos more can be added to this collection.`)
            e.target.value = "";
            return;
        }



        cancelUploadRef.current = false;
        setError("");
        setUploading(true);        
        setUploadingOriginals(true);

        try {

            // const total1 = files.length;
            // let done = 0;

            // 1) parallel threads to S3
            const uploaded = await runWithConcurrency(
                files, 
                
                UPLOAD_CONCURRENCY, 
                
                async(file) => {

                    const clientUploadId = crypto.randomUUID();

                    // init
                    //const { photoId, uploadUrl } = await initUpload(id, file.name, file.size, clientUploadId, clientUploadId);
                    const { photoId, uploadUrl } = await retryAsync(async () => {
                        return await initUpload(id, file.name, file.size, clientUploadId);
                    });
                    

                    // put to S3
                    //await putToPresignedUrl(uploadUrl, file);                
                    await retryAsync(async () =>{
                        return await putToPresignedUrl(uploadUrl, file);
                    });

                    // done++;                
                    // setUploadStatus(`Uploading originals: ${done} of ${total1} photos`);

                    return { photoId, fileName: file.name };
                },

                (completed, total) => {
                    if (cancelUploadRef.current) {
                        setUploadStatus("Cancelling upload...");
                    } else {
                        setUploadStatus(`Uploading originals: ${completed} of ${total} photos`);
                    }                    
                },

                () => cancelUploadRef.current
            );

            setUploadingOriginals(false);

            const successfulUploads = uploaded.filter(x => x?.success).map(x => x.data);
            const failedUploads = uploaded.filter(x => x?.success === false);

            // 2) confirm uploads and start processing files
            const completedUploads = [];
            const failedCompletions = [];

            setUploadStatus(`Starting processing: 0 of ${successfulUploads.length} photos`);
            

            for (let i = 0; i < successfulUploads.length; i++) {
                const photo = successfulUploads[i];

                try {
                    await retryAsync(async () => completeUpload(photo.photoId));
                    completedUploads.push(photo);
                } catch (err) {
                    failedCompletions.push({
                        ...photo,
                        error: err.message
                    });

                    console.error(`Failed to complete upload for photoId ${photo.fileName}:`, err)
                }                

                setUploadStatus(`Starting processing: ${i + 1} / ${successfulUploads.length} photos`);                
            }

            // 3) Poll statuses until all done
            const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
            const total2 = completedUploads.length;            

            while (true) {
                const statuses = await Promise.all(
                    completedUploads.map(async (x) => {
                        try {
                            return await getPhotoStatus(x.photoId);

                        } catch (err) {
                            return{
                                status: "Processing",
                                error: String(err?.message || err)
                            };
                        }
                    })
                );

                const ready = statuses.filter((s) => (s?.status || "").toLowerCase() === "ready").length;
                const failed = statuses.filter((s) => (s?.status || "").toLowerCase() === "failed").length;

                setUploadStatus(`Background processing: Ready ${ready} of ${total2} photos, Failed ${failed} photos`);

                if (ready + failed >= total2) {
                    break;
                }

                await sleep(1500);
            }

            setUploadStatus("Loading gallery...");           
            await load();

            const failedUploadsCount = failedUploads.length + failedCompletions.length;

            if(cancelUploadRef.current){
                setError(
                    `Upload cancelled. ` + 
                    `${successfulUploads.length} photos uploaded to S3. ` + 
                    `${failedUploadsCount} failures.`
                );
            }
            else if (failedUploadsCount > 0) {
                setError(
                    `Upload completed with ${failedUploadsCount} failed photos. ` +
                    `S3 upload failures: ${failedUploads.length}. ` +
                    `Confirmation failures: ${failedCompletions.length}.`
                );
            } else {
                setError("");
            }

            setUploadStatus("");

        } catch (err) {
            setError(err.message);
            setUploadStatus("");

        } finally {
            setUploading(false);
            setUploadingOriginals(false);
            e.target.value = "";
        }
    }
    

    async function deletePhotoHandler(photoId, fileName) {
        const confirmed = confirm(`Delete this photo?\n${fileName ?? "photo"}`);
        if (!confirmed) {
            return;
        }
        
        try {
            setError("");
            await deletePhoto(photoId);
            await refreshCollectionData();
            
        } catch (err) {
            setError(err.message)
        }
    }    
    

    async function viewOriginalHandler(photoId) {        

        try {
            const res = await getOriginalUrl(photoId);
            const url = typeof res === "string" ? res : res?.url;

            if (url) {
                window.open(url, "_blank");
            }

        } catch (err) {
            alert(err.message);
        }
    }
    

    async function downloadOriginalHandler(photoId) {       

        try {
            const res = await getOriginalDownloadUrl(photoId);
            const url = typeof res === "string" ? res : res?.url;

            if (url) {
                window.location.href = url;
            }
        } catch (err) {
            alert(err.message);
        }
    }

    function showLocationHandler(photo) {
        const latitude = photo.latitude ?? photo.Latitude;
        const longitude = photo.longitude ?? photo.Longitude;

        if (latitude == null || longitude == null) {
            alert("Location is not available for this photo.");
            return;
        }

        const url = `https://www.google.com/maps?q=${latitude},${longitude}`;
        window.open(url, "_blank");
    }       

    async function savePhotoDescriptionHandler(photoId, descriptionValue) {
        try {
            setError("");
            const trimmed = descriptionValue?.trim() ?? "";

            const res = await updatePhotoDescription(
                photoId,
                trimmed === "" ? null : trimmed
            );

            const newDescription = res?.description ?? (trimmed === "" ? null : trimmed);

            setCollection(prev => {
                if (!prev) {
                    return prev;
                }

                const photos = prev.photos ?? prev.Photos ?? [];

                const updatedPhotos = photos.map(p =>{
                    const currentId = p.id ?? p.Id;
                    if (currentId !== photoId) {
                        return p;
                    }

                    if (prev.photos) {
                        return { ...p, description: newDescription };
                    }

                    return { ...p, Description: newDescription };
                });

                return prev.photos 
                    ? { ...prev, photos: updatedPhotos } 
                    : { ...prev, Photos: updatedPhotos };
            });

        } catch (err) {
            setError(err.message);
        }
    }

    async function downloadStandardZipHandler() {
        const confirmed = confirm("Download all standard photos as ZIP archive?");
        if (!confirmed) {
            return;
        }

        try {
            setError("");
            await downloadCollectionStandardZip(id);
        } catch (err) {
            setError(err.message);
        }
    }
    

    async function refreshCollectionData() {
        try {
            setError("");

            const data = await getCollection(id);            

            setCollection(data);
            setTitle(data?.title ?? "");
            setDescription(data?.description ?? "");
            
        } catch (err) {
            setError(err.message);
        }
    }
    

    async function createNewShareLinkHandler() {
        try {
            setError("");

            const created = await createOrUpdateSharedLink(id, {
                allowSlideshowOriginals,
                allowDownloadResizedZip,
                allowDownloadOriginalFromCard
            });

            setShareLink(created);
        } catch (err) {
            setError(err.message);
        }
    }

    async function copySharedLinkHandler() {
        const url = shareLink?.url ?? shareLink?.Url;

        if (!url) {
            return;
        }

        try {
            await navigator.clipboard.writeText(url);
            alert("Share link copied to clipboard.");
        } catch (err) {
            setError("Failed to copy share link.", err)
        }
    }

    async function disableShareLinkHandler() {
        const shareLinkId = shareLink?.id ?? shareLink?.Id;

        if (!shareLinkId) {
            return;
        }

        const confirmed = confirm("Disable this share link?");
        if(!confirmed){
            return;
        }

        try {
            setError("");
            await revokeSharedLink(shareLinkId);

            setShareLink(prev => prev 
                ? {...prev, isRevoked: true, IsRevoked: true}
                : prev
            );
                
        } catch (err) {
            setError(err.message);
        }
    }

    async function deleteOriginalPhotosFromCollectionHandler() {
        const confirmed = confirm(
            "Delete all original photos from this collection?\n\n" + 
            "Resized photos, thumbnails, descriptions, map view, shared links will remain available.\n\n" +
            "This action cannot be undone."
        );

        if (!confirmed) {
            return;
        }

        try {
            setError("")            

            await deleteOriginalPhotosFromCollection(id);
            
            await refreshCollectionData();
            
        } catch (err) {
            setError(err.message);
        }
    }



    
    
    const photos = collection?.photos ?? collection?.Photos ?? [];

    const hasGeoPhotos = photos.some(p => {
        const lat = p.latitude ?? p.Latitude;
        const lon = p.longitude ?? p.Longitude;

        return lat != null && lon != null;
    });

    const hasOriginalPhotos = photos.some(p => 
        p.originalUrl ?? p.OriginalUrl
    );

    const originalsQueuedToDelete = photos.some(p =>
        p.originalDeleteRequested ?? p.OriginalDeleteRequested
    );

    const totalPhotos = collection?.totalPhotos ?? collection?.TotalPhotos ?? 0;
    const totalPhotosSize = collection?.totalBytes ?? collection?.TotalBytes ?? 0;
    const totalDistance = collection?.totalDistance ?? collection?.TotalDistance ?? 0;
    const totalOriginal = collection?.totalOriginalSizeBytes ?? collection?.TotalOriginalSizeBytes ?? 0;
    const totalStandard = collection?.totalStandardSizeBytes ?? collection?.TotalStandardSizeBytes ?? 0;
    const totalThumb = collection?.totalThumbSizeBytes ?? collection?.TotalThumbSizeBytes ?? 0;    

    const standardSlides = photos
        .filter(p => (p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl))
        .map(p => ({
            src: p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl,
            alt: p.originalFileName ?? p.OriginalFileName ?? "photo",
        }));

    const originalSlides = photos
        .filter(p => (p.originalUrl ?? p.OriginalUrl))
        .map(p => ({
            src: p.originalUrl ?? p.OriginalUrl,
            alt: p.originalFileName ?? p.OriginalFileName ?? "photo",
        }));

    const slides = viewerMode === "original" ? originalSlides : standardSlides;


    


    if (loading) {        
        return(
            <div className='container py-4'>
                <div className='alert alert-info'>Loading...</div>
            </div>
        );
        
                 
    }

    return(
        <>
        <div className="container py-4">    
            <div className="d-flex align-items-center justify-content-between">
                <h2 className='mb-0'>{collection?.title || "-"}</h2>
                <button
                    className="btn btn-primary"
                    onClick={() => navigate("/collections")}
                >
                    Back to My Collections
                    </button>
                </div>
                    
                <hr/>

                {error ? <div className="alert alert-danger">{error}</div> : null}
                    {/* {status ? <div className="alert alert-info">{status}</div> : null} */}

                    {/* <h5>{collection?.title}</h5> */}
                    <p style={{ whiteSpace: "pre-wrap" }}>{collection?.description || "No description of collection yet."}</p>
                    {/* <p>Collection Id: {collection?.id}</p> */}

                    <div className='mb-2'>
                        {!isEditing ? (
                            <>

                            {/* <hr/> */}

                            <button
                                className='btn btn-outline-secondary'
                                type='button'
                                data-bs-toggle="collapse" 
                                data-bs-target="#collapseEditCollectionDetails" 
                                aria-expanded="false" 
                                aria-controls="collapseEditCollectionDetails"
                                // onClick={() => setIsEditing(true)}
                            >Edit Collection Title & Description</button>

                            <div className='collapse' id='collapseEditCollectionDetails'>
                                <div className='card card-body mt-3'>
                                    <form className='mt-3'>
                                        <div className="mb-3">
                                            <label className="form-label">Collection Title</label>
                                            <input
                                                className="form-control"
                                                value={title}
                                                onChange={(e) => setTitle(e.target.value)}
                                                // disabled={!isEditing}
                                            />
                                        </div>
                                        <div className="mb-3">
                                            <label className="form-label">Collection Description</label>
                                            <TextareaAutosize
                                                className="form-control"
                                                minRows={2}
                                                value={description}
                                                onChange={(e) => setDescription(e.target.value)}
                                                // disabled={!isEditing}
                                            />
                                            
                                        </div>                        
                                    </form>
                                    <div className='d-flex gap-2'>
                                        <button
                                            className='btn btn-primary'
                                            onClick={onSave}
                                        >
                                            Save
                                        </button>
                                        <button
                                            className='btn btn-secondary'
                                            onClick={onCancel}
                                        >
                                            Cancel
                                        </button>                                
                                    </div>
                                </div>
                            </div>

                            </>                            
                        ) : (
                            <>                            

                            <div className='d-flex gap-2'>
                                <button
                                    className='btn btn-primary'
                                    onClick={onSave}
                                >
                                    Save
                                </button>
                                <button
                                    className='btn btn-secondary'
                                    onClick={onCancel}
                                >
                                    Cancel
                                </button>                                
                            </div>

                            <form className='mt-3'>
                                <div className="mb-3">
                                    <label className="form-label">Collection Title</label>
                                    <input
                                        className="form-control"
                                        value={title}
                                        onChange={(e) => setTitle(e.target.value)}
                                        disabled={!isEditing}
                                    />
                                </div>
                                <div className="mb-3">
                                    <label className="form-label">Collection Description</label>
                                    <TextareaAutosize
                                        className="form-control"
                                        minRows={2}
                                        value={description}
                                        onChange={(e) => setDescription(e.target.value)}
                                        disabled={!isEditing}
                                    />
                                    
                                </div>                        
                            </form>

                            

                            </>
                            
                            
                        )}
                    </div>  
                
                <hr/>
                    <div className='d-flex gap-2 flex-wrap'>
                        <button
                            className='btn btn-primary'
                            disabled={!hasGeoPhotos}                            
                            title='View all geotagged photos on an interactive map.'
                            onClick={() => navigate(`/collections/${collection.id}/map`)}
                        >
                            Map View
                        </button>

                        <button
                            className='btn btn-primary'
                            title='Start a slideshow using resized photos.'
                            disabled={totalPhotos === 0}
                            onClick={() => {
                                if(standardSlides.length === 0){
                                    setError("No resized photos available for slideshow.");
                                    return;
                                }

                                setError("");
                                setViewerMode("standard");
                                setLightboxIndex(0);
                                setLightboxOpen(true);
                                setSlideshowAutoplay(true);
                            }}
                        >
                            Slideshow
                        </button>

                        <button
                            className='btn btn-primary'
                            title='Start a slideshow using original full-resolution photos.'
                            disabled={!hasOriginalPhotos}
                            onClick={() => {
                                if(originalSlides.length === 0){
                                    setError("No original photos available for slideshow.");
                                    return;
                                }

                                setError("");
                                setViewerMode("original");
                                setLightboxIndex(0);
                                setLightboxOpen(true);
                                setSlideshowAutoplay(true);
                            }}>
                            Slideshow Originals
                        </button>                        

                        <button
                            className='btn btn-primary'
                            title='Download all resized photos as a ZIP archive.'
                            disabled={totalPhotos === 0}
                            onClick={downloadStandardZipHandler}
                        >
                            Download Resized ZIP
                        </button>              
                        
                        <button 
                            className='btn btn-primary' 
                            title='Create and manage a shareable link for this collection.'
                            disabled={totalPhotos === 0}
                            type='button' 
                            data-bs-toggle="collapse" 
                            data-bs-target="#collapseShareLink" 
                            aria-expanded="false" 
                            aria-controls="collapseShareLink"
                        >
                            Share Collection
                        </button>

                        {!hasGeoPhotos && (
                            <div className='small text-muted w-100'>
                                Map View is unavailable: no photos with GPS coordinates.
                            </div>
                        )}
                    </div>
                
                <div className='collapse' id='collapseShareLink'>                            
                        <div className='card card-body mt-3'>
                            
                            {/* <div className='small'>Active share link: No active link yet</div>                                 */}
                            <div className='small'>
                                {
                                    shareLink && !(shareLink.isRevoked ?? shareLink.IsRevoked)
                                        ? (
                                            <div className='alert alert-warning'>
                                                Active share link:{" "}
                                                <a
                                                    href={shareLink.url ?? shareLink.Url}
                                                    target='_blank'
                                                    rel='noopener noreferrer'
                                                >
                                                    {shareLink.url ?? shareLink.Url}
                                                </a>
                                                
                                            </div>
                                        )
                                        : (
                                            <div className='alert alert-info'>Active share link: No active link yet</div>
                                        )
                                }
                            </div>

                            {/* <hr/>                             */}

                            <div className='d-flex gap-2 flex-wrap'>
                                <button 
                                    className='btn btn-primary' 
                                    type='button'
                                    onClick={copySharedLinkHandler}
                                    disabled={!shareLink || (shareLink.isRevoked ?? shareLink.IsRevoked)}
                                >
                                    Copy Link
                                </button>
                                <button 
                                    className='btn btn-primary' 
                                    type='button'
                                    onClick={disableShareLinkHandler}
                                    disabled={!shareLink || (shareLink.isRevoked ?? shareLink.IsRevoked)}
                                >
                                    Disable Link
                                </button>                                
                            </div>                 

                            <hr/>           

                            <div className='small mb-2'>
                                Shared page includes by default:
                            </div>
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareCollectionTitleAndDescription' checked disabled/>
                                <label className='form-check-label small' htmlFor='shareCollectionTitleAndDescription'>Collection title and description</label>                                
                            </div>
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareMapView' checked disabled/>
                                <label className='form-check-label small' htmlFor='shareMapView'>Map View</label>                                
                            </div>
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareSlideshowResized' checked disabled/>
                                <label className='form-check-label small' htmlFor='shareSlideshowResized'>Slideshow Resized</label>                                
                            </div>
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='sharePhotoCard' checked disabled/>
                                <label className='form-check-label small' htmlFor='sharePhotoCard'>Photo Card: View Original, Location, Coordinates, Description</label>                                
                            </div>                            
                            {/* <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareArchiveCard' checked disabled/>
                                <label className='form-check-label small' htmlFor='shareArchiveCard'>Archive Card: Download, Description</label>                                
                            </div>                             */}

                            <div className='small mt-3 mb-2'>
                                Additional options for this share link:
                            </div>
                            <div className='form-check form-switch'>                                
                                <input 
                                    className='form-check-input' 
                                    type='checkbox' role='switch' 
                                    id='shareSlideshowOriginals'
                                    checked={allowSlideshowOriginals}
                                    onChange={(e) => setAllowSlideshowOriginals(e.target.checked)}
                                />
                                <label className='form-check-label small' htmlFor='shareSlideshowOriginals'>Slideshow Originals</label>                                
                            </div>
                            <div className='form-check form-switch'>                                
                                <input 
                                    className='form-check-input' 
                                    type='checkbox' 
                                    role='switch' 
                                    id='shareDownloadResizedZip'
                                    checked={allowDownloadResizedZip}
                                    onChange={(e) => setAllowDownloadResizedZip(e.target.checked)}
                                />
                                <label className='form-check-label small' htmlFor='shareDownloadResizedZip'>Download Resized ZIP</label>                                
                            </div>
                            <div className='form-check form-switch'>                                
                                <input 
                                    className='form-check-input' 
                                    type='checkbox' 
                                    role='switch' 
                                    id='shareDownloadOriginalFromPhotoCard'
                                    checked={allowDownloadOriginalFromCard}
                                    onChange={(e) => setAllowDownloadOriginalFromCard(e.target.checked)}
                                />
                                <label className='form-check-label small' htmlFor='shareDownloadOriginalFromPhotoCard'>Download Original from Photo Card</label>                                
                            </div>

                            <hr/>

                            <div className='d-flex gap-2 flex-wrap'>                                
                                <button 
                                    className='btn btn-primary' 
                                    type='button'
                                    onClick={createNewShareLinkHandler}
                                >
                                    Create New Link
                                </button>
                            </div>                 
                        </div>                        
                    </div>

                    <hr/>

                    {/* <hr/>                             */}
                    {/* {uploadStatus ? <div className='alert alert-info'>{uploadStatus || "Preparing upload..."}</div> : null} */}
                    
                    {uploading && (
                        <div className='alert alert-info'>
                            <div className='mb-2'>
                                {uploadStatus || "Preparing upload..."}                                
                            </div>

                            <button
                                className='btn btn-danger'
                                type='button'
                                onClick={cancelUpload}
                                disabled={cancelUploadRef.current || !uploadingOriginals}
                            >
                                {cancelUploadRef.current ? "Cancelling..." : "Cancel Upload"}
                            </button>
                        </div>                        
                    )}
                    

                    <h5>Photos</h5>

                    <label className='form-label'>Upload photos (.jpg, .jpeg)</label>
                        <input
                            type='file'
                            className='form-control mb-3'
                            accept='.jpg,.jpeg,image/jpeg'
                            multiple
                            disabled={uploading || isEditing}
                            onChange={onFilesSelected}
                        />
                    {/* <hr/>                   */}
                                        
                    <div className="d-flex align-items-start justify-content-between small mb-3">
                        <div>
                            Distance: {formatDistance(totalDistance)}<br/>
                            Photos: {totalPhotos}<br/>
                            Size: {formatBytes(totalPhotosSize)}
                        </div>
                        <div className='text-end'>                        
                            Originals: {formatBytes(totalOriginal)}<br/>
                            Resized photos: {formatBytes(totalStandard)}<br/>
                            Thumbnails: {formatBytes(totalThumb)}<br/>
                        </div>
                    </div>                    
        </div>

        <div className='container-fluid py-4 py-md-3 py-xl-4'>
                    

                    {uploading ? (
                        <div className='alert alert-info'>Uploading/Processing... please wait</div>
                    ) : photos.length === 0 ? (
                        <div className='alert alert-info'>No photos uploaded yet</div>
                    ) : (
                        <div className='row'>
                            {photos.map((p, index) => (
                                <div key={p.id ?? p.Id} className='col-12 col-sm-6 col-lg-4 col-xl-3 col-xxl-2 photo-grid-item mb-3'>
                                    <PhotoCard 
                                        photo={p} 
                                        onDeleted={deletePhotoHandler} 
                                        onViewOriginal={viewOriginalHandler}
                                        onDownloadOriginal={downloadOriginalHandler}
                                        onLocation={showLocationHandler}
                                        onSaveDescription={savePhotoDescriptionHandler}
                                        onOpenLightBox={() => {
                                            setViewerMode("standard");
                                            setLightboxIndex(index);
                                            setLightboxOpen(true);
                                            setSlideshowAutoplay(false);
                                        }}
                                    />
                                </div>
                            ))}
                        </div>
                    )}
        </div>

        <div className="container py-4">

                    <section className='mt-2'>
                        <h6>Storage optimisation</h6>

                        {totalPhotos === 0 ? (
                            <p className='small'>Upload photos to this collection before using storage optimisation.</p>
                        ) : hasOriginalPhotos ? (
                            <p className='small'>Delete original photo files to reduce storage usage. Resized photos, thumbnails, notes, map view and sharing will remain available. Original photo downloads and original slideshow will no longer be available.</p>                            
                        ) : originalsQueuedToDelete ? (
                            <p className='small'>Original photo files have been queued for background deletion. They are no longer available in this collection and will be physically removed from storage soon. The storage statistics above show the current actual stored size and may update after background cleanup is completed.</p>
                        ) : (
                            <p className="small">Original photo files have been removed. This collection now contains only resized photos and thumbnails.</p>
                        )}
                        
                        <button
                            className='btn btn-danger'
                            disabled={!hasOriginalPhotos}
                            onClick={deleteOriginalPhotosFromCollectionHandler}
                        >
                            Delete Originals
                        </button>
                    </section>

                    
                    {/* <hr/>
                    <h5>Archives</h5> */}
                    
                    {/* Archives */}
                    {/* <label className='form-label'>Upload archive (.zip)</label>
                    <input
                        type='file'
                        className='form-control mb-3'                        
                        accept='.zip,application/zip,application/x-zip-compressed'
                        // disabled={archiveUploadStatus || isEditing}
                        disabled={archiveUploading || isEditing}
                        onChange={onArchiveSelected}
                    />

                    <div className="d-flex align-items-start justify-content-between small mb-3">
                        <div>
                            Total archives: {totalArchives}<br/>                            
                        </div>
                        <div className='text-end'>                        
                            Total archives size: {formatBytes(totalArchivesSize)}<br/>                            
                        </div>
                    </div> */}
                    

                    {/* {archiveUploadStatus ? (
                        <div className='alert alert-info mt-2'>
                            {archiveUploadStatus}
                            {archiveUploading &&(
                                <div className='progress mt-2'>
                                    <div
                                        className='progress-bar'
                                        role='progressbar'
                                        style={{width: `${archiveUploadProgress}%`}}
                                    >
                                        {archiveUploadProgress}
                                    </div>
                                </div>
                            )}
                        </div>
                    ) : null} */}

                    {/* <hr/> */}
                    {/* Archive Cards */}
                    {/* {archives.length === 0 ? (
                        <div className='alert alert-info'>No archives uploaded yet</div>
                    ) : (
                        <div className='row'>
                            {archives.map((a) => (
                                <div key={a.id ?? a.Id} className='col-12 col-md-6 col-lg-3 mb-3'>
                                    <ArchiveCard
                                        archive={a}
                                        onDownload={downloadArchiveHandler}
                                        onDelete={deleteArchiveHandler}
                                        onSaveDescription={saveArchiveDescriptionHandler}
                                    />
                                    
                                </div>
                            ))}                            
                        </div>
                    )} */}

                    

                    {/* Lightbox gallery */}
                    {/* <div className='row'>
                        {photos.map((p, index) =>{
                            const thumbUrl = p.thumbUrl ?? p.ThumbUrl;

                            return(
                                <div className='col-md-3 mb-3' key={p.id}>
                                    <img
                                        src={thumbUrl}
                                        alt={p.originalFileName ?? p.OriginalFileName ?? "photo"}
                                        style={{width:"100%", height: 160, objectFit:"cover", cursor:"pointer"}}
                                        onClick={() => {
                                            setLightboxIndex(index);
                                            setLightboxOpen(true);
                                        }}
                                    />
                                </div>
                            );
                        })}                        
                    </div> */}

                    {/* Lightbox */}
                    <Lightbox
                        open={lightboxOpen}
                        close={() => setLightboxOpen(false)}
                        index={lightboxIndex}
                        slides={slides}
                        plugins={[Slideshow, Counter, Fullscreen, Zoom]}
                        slideshow={{
                            delay: viewerMode === "standard" ? 4000 : 5000, 
                            autoplay: slideshowAutoplay
                        }}                        
                    />

                    
                {/* </div>                 */}
            {/* </div> */}
        </div>        
        {/* <footer className="border-top">
            <div className="container py-4 text-muted small">
                <div>
                    PhotosMap © 2026
                </div>
            </div>
        </footer> */}
        </>
        
    );
}

const PhotoCard = React.memo(function PhotoCard({ 
        photo, 
        onDeleted, 
        onViewOriginal, 
        onDownloadOriginal, 
        onLocation, 
        onSaveDescription,
        onOpenLightBox
    }) {
        // const [thumbUrl, setThumbUrl] = useState("");
        
        const thumbUrl = photo.thumbUrl ?? photo.ThumbUrl;
        const originalUrl = photo.originalUrl ?? photo.OriginalUrl;
        const hasOriginal = originalUrl != null;
        const [isEditingDescriptionPhoto, setIsEditingDescriptionPhoto] = useState(false);
        const [descriptionPhoto, setDescriptionPhoto] = useState(photo.description ?? photo.Description ?? "");

        const photoId = photo.id ?? photo.Id;
        console.log("PhotoCard render:", photoId); 

        const status = photo.status ?? photo.Status;
        const originalFileName = photo.originalFileName ?? photo.OriginalFileName;
        const latitude = photo.latitude ?? photo.Latitude;
        const longitude = photo.longitude ?? photo.Longitude;
        const distanceFromPrevious = photo.distanceFromPrevious ?? photo.DistanceFromPrevious;
        const takenAt = photo.takenAt ?? photo.TakenAt;

        
        

        // useEffect(() => {
        //     let cancelled = false;            

        //     async function loadThumb() {
        //         if (status !== "Ready") {
        //             setThumbUrl("");
        //             return;
        //         }

        //         try {
        //             const res = await getThumbUrl(photoId);
        //             const url = typeof res === "string" ? res : res?.url ?? res?.thumbUrl;

        //             if (!cancelled) {
        //                 setThumbUrl(url || "");
        //             }
        //         } catch (err) {
        //             console.error("thumb error", err);
        //         }
        //     }

        //     loadThumb();

        //     return () => {
        //         cancelled = true;
        //     };
        // }, [photoId, status]);        

        async function handleSaveDescription() {
            await onSaveDescription?.(photoId, descriptionPhoto);
            setIsEditingDescriptionPhoto(false);
        }

        async function handleCopyCoordinates() {
            if (latitude == null || longitude == null) {
                return;
            }

            const text = `${latitude}, ${longitude}`;

            try {
                await navigator.clipboard.writeText(text);
                console.log("Copied coordinates:", text);
            } catch (err) {
                console.log("Copy coordinates failed", err);
            }
        }
        

        

        return(
            <div className="card shadow-sm h-100 position-relative">

                {/* Photo */}
                {thumbUrl ? (
                    <div
                        onClick={onOpenLightBox}
                        style={{cursor:"pointer"}}
                    >
                        <img
                            src={thumbUrl}
                            alt={originalFileName || "photo"}
                            loading='lazy'
                            style={{ width: "100%", aspectRatio: "16/9", objectFit: "cover", display: "block" }}
                        />
                    </div>
                    
                ) : (
                    <div 
                        className="d-flex align-items-center justify-content-center" 
                        style={{ height: 160}}
                    >
                        <span>{status}</span>
                    </div>
                )}

                {/* Photo date & distance to previous */}
                <div className="card-body p-2 d-flex flex-column">

                    <div className='d-flex align-items-start justify-content-between'>

                        {/* Photo taken */}
                        {takenAt && (
                            <div className="small text-truncate">
                                {formatTakenAt(takenAt)}
                            </div>
                        )}

                        {/* Distance from previous */}
                        {distanceFromPrevious !== null && distanceFromPrevious > 0 && (
                            <div className="small text-truncate">
                                + {formatDistance(distanceFromPrevious)}
                            </div>
                        )}                  


                    </div>
                    

                    {/* Photo description */}
                    {!isEditingDescriptionPhoto 
                        ? (descriptionPhoto) 
                            ? (
                                <div>
                                    <hr/>
                                    <div className='small text-muted text-truncate'>{descriptionPhoto}</div>
                                </div>
                            ) 
                            : null
                        : (
                            <div className='mt-2'>
                                
                                <hr/>
                                <TextareaAutosize
                                    className='form-control form-control-sm'                                    
                                    minRows={2}
                                    value={descriptionPhoto}
                                    style={{overflow: "hidden"}}
                                    onChange={(e) => setDescriptionPhoto(e.target.value)}
                                    placeholder='Add photo description'
                                />
                                <div className='d-flex gap-2 mt-2'>
                                    <button
                                        className='btn btn-outline-secondary btn-sm'
                                        onClick={handleSaveDescription}
                                        title='Save description'
                                    >
                                        {/* <i className="bi bi-check me-1"></i> */}
                                        Save
                                    </button>
                                    <button
                                        className='btn btn-outline-secondary btn-sm'
                                        onClick={() => {
                                            setDescriptionPhoto(photo.description ?? photo.Description ?? "");
                                            setIsEditingDescriptionPhoto(false);
                                        }}
                                        title='Cancel description'
                                    >
                                        {/* <i className="bi bi-x me-1"></i> */}
                                        Cancel
                                    </button>
                                </div>                                
                            </div>
                        )
                    }

                    <hr/>
                    {/* Card buttons block */}
                    <div className="d-flex flex-wrap gap-1 mt-auto">
                        <button
                            // className='btn btn-outline-danger btn-sm'
                            className='btn-close position-absolute top-0 end-0 m-2'
                            onClick={() => onDeleted?.(photoId, originalFileName)}
                            title='Delete photo'
                        >                            
                        </button>

                        
                        {hasOriginal && (
                            <>
                            <button
                                className='btn btn-outline-secondary btn-sm'
                                onClick={() => onViewOriginal?.(photoId, originalFileName)}
                                title='View original'
                            >
                                <i className='bi bi-eye'></i>
                            </button>

                            <button
                                className='btn btn-outline-secondary btn-sm'
                                onClick={() => onDownloadOriginal?.(photoId, originalFileName)}
                                title='Download original'                            
                            >
                                <i className='bi bi-download'></i>
                            </button>
                            </>
                        )}
                        

                        {latitude != null && longitude != null && (
                            <>
                                <button
                                    className='btn btn-outline-secondary btn-sm'
                                    onClick={() => onLocation?.(photo)}
                                    title='Show location'
                                >
                                    <i className='bi bi-geo-alt'></i>
                                </button>
                                <button
                                    className='btn btn-outline-secondary btn-sm'
                                    onClick={handleCopyCoordinates}
                                    title='Copy coordinates'
                                >
                                    <i className='bi bi-copy'></i>
                                </button>
                            </>                            
                        )}                        

                        <button
                            className='btn btn-outline-secondary btn-sm'
                            onClick={() => setIsEditingDescriptionPhoto((v) => !v)}
                            title='Edit description'

                        >
                            <i className='bi bi-pencil'></i>
                        </button>
                    </div>

                </div>
            </div>
        );
    });


// ArchiveCard Component
// const ArchiveCard = React.memo(function ArchiveCard({
//     archive,
//     onDownload,
//     onDelete,
//     onSaveDescription
// }) {
//     const archiveId = archive.id ?? archive.Id;
//     const originalFileName = archive.originalFileName ?? archive.OriginalFileName;
//     const sizeBytes = archive.sizeBytes ?? archive.SizeBytes;
//     const createdAt = archive.createdAtUtc ?? archive.CreatedAtUtc;
//     const initialDescription = archive.description ?? archive.Description ?? "";

//     const [isEditingDescriptionArchive, setIsEditingDescriptionArchive] = useState(false);
//     const [descriptionArchive, setDescriptionArchive] = useState(initialDescription);

//     async function handleSaveDescriptionArchive() {
//         await onSaveDescription?.(archiveId, descriptionArchive);
//         setIsEditingDescriptionArchive(false);
//     }

//     return(
//         <div className='card shadow-sm h-100 position-relative'>
//             <div
//                 className="d-flex flex-column align-items-center justify-content-center bg-light"
//                 style={{ width: "100%", height: 80 }}
//             >
//                 <i className="bi bi-file-earmark-zip" style={{ fontSize: 40 }}></i>                                            
//             </div>
//             <div className='card-body p-2 d-flex flex-column'>
//                 <div className='small text-truncate'>
//                     {originalFileName || "(no name)"}
//                 </div>
//                 <div className='small text-truncate'>
//                     {formatBytes(sizeBytes)}
//                 </div>
//                 <div className='small text-truncate'>
//                     {formatTakenAt(createdAt)}
//                 </div>

//                 {!isEditingDescriptionArchive 
//                     ? descriptionArchive 
//                         ? (
//                             <div>
//                                 <hr/>
//                                 <div className='small text-truncate'>
//                                     {descriptionArchive}
//                                 </div>
//                             </div>
//                         ) 
//                         : null 
//                     : (
//                         <div className='mt-2'>
//                             <hr/>
//                             <TextareaAutosize
//                                 className='form-control form-control-sm'
//                                 minRows={2}
//                                 value={descriptionArchive}
//                                 style={{overflow: "hidden"}}
//                                 onChange={(e) => setDescriptionArchive(e.target.value)}
//                                 placeholder='Add archive description'
//                             />
//                             <div className='d-flex gap-2 mt-2'>
//                                 <button
//                                     className='btn btn-outline-secondary btn-sm'
//                                     onClick={handleSaveDescriptionArchive}
//                                     title='Save description'
//                                 >
//                                     Save
//                                 </button>
//                                 <button
//                                     className='btn btn-outline-secondary btn-sm'
//                                     onClick={() =>{
//                                         setDescriptionArchive(initialDescription);
//                                         setIsEditingDescriptionArchive(false);
//                                     }}
//                                     title='Cancel description'
//                                 >
//                                     Cancel
//                                 </button>
//                             </div>
//                         </div>
//                 )}

//                 {/* Archive card small buttons block */}
//                 <hr/>
//                 <div className='d-flex flex-wrap gap-1 mt-auto'>
//                     <button
//                         className='btn btn-outline-secondary btn-sm'
//                         onClick={() => onDownload?.(archiveId)}
//                         title='Download archive'
//                     >
//                         <i className='bi bi-download'></i>
//                     </button>

//                     <button
//                         className='btn btn-outline-secondary btn-sm'
//                         onClick={() => onDelete?.(archiveId, originalFileName)}
//                         title='Delete archive'
//                     >
//                         <i className='bi bi-trash'></i>
//                     </button>

//                     <button
//                         className='btn btn-outline-secondary btn-sm'
//                         onClick={() => setIsEditingDescriptionArchive((v) => !v)}
//                         title='Edit description'
//                     >
//                         <i className='bi bi-pencil'></i>
//                     </button>                    
//                 </div>
//             </div>
            
//         </div>
//     );
// });
