import TextareaAutosize from 'react-textarea-autosize';
import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { completeArchiveUpload, completeUpload, deleteArchive, deleteCollection, deletePhoto, downloadCollectionStandardZip, getArchiveDownloadUrl, getCollection, getCollectionArchives, getOriginalDownloadUrl, getOriginalUrl, getPhotoStatus, getThumbUrl, getToken, initArchiveUploads, initUpload, putToPresignedUrl, putToPresignedUrlWithProgress, updateArchiveDescription, updateCollection, updatePhotoDescription } from "../api";
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

    const [archives, setArchives] = useState([]);
    const [archiveUploading, setArchiveUploading] = useState(false);
    const [archiveUploadProgress, setArchiveUploadProgress] = useState(0);
    const [archiveUploadStatus, setArchiveUploadStatus] = useState("");

    const [lightboxOpen, setLightboxOpen] = useState(false);
    const [lightboxIndex, setLightboxIndex] = useState(0);
    const [viewerMode, setViewerMode] = useState("standard");

    
    
    

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
            setArchives(data?.archives ?? data?.Archives ?? []);
        } catch (err) {
            setError(err.message);
            setCollection(null);
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

    async function runWithConcurrency(items, concurrency, handler) {
        const results = new Array(items.length);
        let index = 0;

        async function worker() {
            while(true){
                const i = index++;
                if (i >= items.length) {
                    return;
                }

                results[i] = await handler(items[i], i);
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

    async function onFilesSelected(e) {
        const UPLOAD_CONCURRENCY = 4;
        const MAX_UPLOAD_BATCH = 500; // 500 
        const MAX_COLLECTION_PHOTOS = 1500; // 1500

        const currentTotalPhotosInCollection = collection.totalPhotos ?? 0;
        const possibleToLoadToCollection = MAX_COLLECTION_PHOTOS - currentTotalPhotosInCollection;        

        const files = Array.from(e.target.files || []);


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
            setError(`You selected ${files.length} photos, but only ${possibleToLoadToCollection} photos more can be added to this collecftion.`)
            e.target.value = "";
            return;
        }



        setError("");
        setUploading(true);        

        try {

            const total1 = files.length;
            let done = 0;

            // 1) parallel threads to S3
            const uploaded = await runWithConcurrency(files, UPLOAD_CONCURRENCY, async(file, i) => {

                // init
                const { photoId, uploadUrl } = await initUpload(id, file.name, file.size);

                // put to S3
                await putToPresignedUrl(uploadUrl, file);

                done++;
                // setUploadStatus(`Uploading original: ${done}/${total1} (last: ${file.name}/${file.size} MB)`);
                setUploadStatus(`Uploading originals: ${done} of ${total1} photos`);

                return { photoId, fileName: file.name };
            });

            // 2) processing files one by one
            setUploadStatus(`Starting processing: 0 of ${uploaded.length} photos`);

            for (let i = 0; i < uploaded.length; i++) {
                await completeUpload(uploaded[i].photoId);
                setUploadStatus(`Starting processing: ${i + 1} / ${uploaded.length} photos`);                
            }

            // 3) Poll statuses until all done
            const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
            const total2 = uploaded.length;            

            while (true) {
                const statuses = await Promise.all(
                    uploaded.map(async (x) => {
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

            setUploadStatus("All done. Loading gallery...");           
            await load();
            setUploadStatus("");

        } catch (err) {
            setError(err.message);
            setUploadStatus("");

        } finally {
            setUploading(false);
            e.target.value = "";
        }
    }    

    // async function onFilesSelected(e) {
    //     const files = Array.from(e.target.files || []);
    //     if (files.length === 0) return;

    //     setError("");
    //     setUploading(true);

    //     const uploaded = [];

    //     try {
    //         // 1) Upload ALL originals S3

    //         for (let i = 0; i < files.length; i++) {
    //             const file = files[i];
    //             setUploadStatus(`Uploading ${i + 1}/${files.length}: ${file.name}`);

    //             // 1) init
    //             const { photoId, uploadUrl } = await initUpload(id, file.name);

    //             // 2) PUT to S3
    //             await putToPresignedUrl(uploadUrl, file);

    //             uploaded.push({ photoId, fileName: file.name });

    //             // // 3) complete (processing + queue)
    //             // await completeUpload(photoId);                
    //         }

    //         // 2) Starting processing for ALL          

    //         setUploadStatus(`Starting processing 0/${uploaded.length}`);

    //         for (let i = 0; i < uploaded.length; i++) {
    //             await completeUpload(uploaded[i].photoId);
    //             setUploadStatus(`Starting processing: ${i + 1}/${uploaded.length}`);
    //             // const element = uploaded[i];
                
    //         }

    //         // 3) Poll statuses until all done
    //         const total = uploaded.length;

    //         const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

    //         while (true) {
    //             const statuses = await Promise.all(
    //                 uploaded.map(async (x) => {
    //                     try {
    //                         return await getPhotoStatus(x.photoId);
    //                     } catch (err) {
    //                         return{
    //                             status: "Processing",
    //                             error: String(err?.message || err)
    //                         };
    //                     }
    //                 })
    //             );

    //             const ready = statuses.filter((s) => (s?.status || "").toLowerCase() === "ready").length;
    //             const failed = statuses.filter((s) => (s?.status || "").toLowerCase() === "failed").length;

    //             setUploadStatus(`Processing photos: Ready ${ready}/${total}, Failed ${failed}/${total}`);

    //             if (ready + failed >= total) {
    //                 break;
    //             }

    //             await sleep(1500);
    //         }

    //         setUploadStatus("All done. Loading gallery...");           
    //         await load();
    //         setUploadStatus("");

    //     } catch (err) {
    //         setError(err.message);
    //         setUploadStatus("");
    //     } finally {
    //         setUploading(false);
    //         e.target.value = "";
    //     }
    // }    

    async function deletePhotoHandler(photoId, fileName) {
        const confirmed = confirm(`Delete this photo?\n${fileName ?? "photo"}`);
        if (!confirmed) {
            return;
        }
        
        try {
            setError("");
            await deletePhoto(photoId);
            await refreshCollectionData();

            // setCollection(prev => {
            //     if (!prev) {
            //         return prev;
            //     }

            //     const photos = prev.photos ?? prev.Photos ?? [];
            //     const newPhotos = photos.filter(p => (p.id ?? p.Id) !== photoId);                

            //     return prev.photos ? { ...prev, photos: newPhotos } : { ...prev, Photos: newPhotos }
                
            // })
        } catch (err) {
            setError(err.message)
        }
    }    

    // async function viewOriginalHandler(photoId, fileName) {
    //     const confirmed = confirm(`View original in browser?\n${fileName ?? "photo"}`);
    //     if (!confirmed) {
    //         return;
    //     }

    //     try {
    //         const res = await getOriginalUrl(photoId);
    //         const url = typeof res === "string" ? res : res?.url;

    //         if (url) {
    //             window.open(url, "_blank");
    //         }

    //     } catch (err) {
    //         alert(err.message);
    //     }
    // }

    async function viewOriginalHandler(photoId) {
        // const confirmed = confirm(`View original in browser?\n${fileName ?? "photo"}`);
        // if (!confirmed) {
        //     return;
        // }

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

    // async function downloadOriginalHandler(photoId, fileName) {
    //     const confirmed = confirm(`Download original file?\n${fileName ?? "photo"}`);
    //     if (!confirmed) {
    //         return;
    //     }

    //     try {
    //         const res = await getOriginalDownloadUrl(photoId);
    //         const url = typeof res === "string" ? res : res?.url;

    //         if (url) {
    //             window.location.href = url;
    //         }
    //     } catch (err) {
    //         alert(err.message);
    //     }
    // }

    async function downloadOriginalHandler(photoId) {
        // const confirmed = confirm(`Download original file?\n${fileName ?? "photo"}`);
        // if (!confirmed) {
        //     return;
        // }

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

    async function onArchiveSelected(e) {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!file.name.toLowerCase().endsWith(".zip")) {
        setError("Only .zip archives are allowed.");
        e.target.value = "";
        return;
    }

    try {
        setError("");
        setArchiveUploading(true);
        setArchiveUploadProgress(0);
        setArchiveUploadStatus("Initializing archive upload...");
        
        const { archiveId, uploadUrl } = await initArchiveUploads(id, file.name, file.size);

        setArchiveUploadStatus("Uploading archive...");

        await putToPresignedUrlWithProgress(uploadUrl, file, (loaded, total) => {
            const percent = Math.round((loaded / total) * 100);
            setArchiveUploadProgress(percent);
            setArchiveUploadStatus(`Uploading archive: ${percent}%`);
        });

        setArchiveUploadStatus("Saving archive metadata...");

        await completeArchiveUpload(archiveId, id, file.name, file.size);

        await refreshCollectionData();

        setArchiveUploadStatus("Archive uploaded successfully.");
        setArchiveUploadProgress(100);
        // const archivesData = await getCollectionArchives(id);        
        // 
        
        } catch (err) {
            setError(err.message);
            setArchiveUploadStatus("");
            setArchiveUploadProgress(0);
        } finally {
            setArchiveUploading(false);
            // setArchiveUploadProgress(0);
            e.target.value = "";
        }
    }

    async function deleteArchiveHandler(archiveId, fileName) {
        const confirmed = confirm(`Delete this archive?\n${fileName ?? "archive"}`);
        if (!confirmed) {
            return;
        }

        try {
            setError("");
            await deleteArchive(archiveId);
            await refreshCollectionData();

            // setArchives(prev => 
            //     prev.filter(a => (a.id ?? a.Id) !== archiveId)
            // );
        } catch (err) {
            setError(err.message);
        }
    }

    async function downloadArchiveHandler(archiveId) {
        try {
            setError("");

            const res = await getArchiveDownloadUrl(archiveId);
            const url = typeof res === "string" ? res : res?.url;

            if (url) {
                window.location.href = url;
            }
        } catch (err) {
            setError(err.message);
        }
    }

    async function refreshCollectionData() {
        try {
            setError("");

            const data = await getCollection(id);
            // const archivesData = await getCollectionArchives(id);

            setCollection(data);
            setTitle(data?.title ?? "");
            setDescription(data?.description ?? "");

            const archivesData = data?.archives ?? data?.Archives ?? [];
            setArchives(archivesData ?? []);
        } catch (err) {
            setError(err.message);
        }
    }

    async function saveArchiveDescriptionHandler(archiveId, descriptionValue) {
        try {
            setError("");
            const trimmed = descriptionValue?.trim() ?? "";

            const res = await updateArchiveDescription(
                archiveId,
                trimmed === "" ? null : trimmed
            );

            const newDescription = res?.description ?? (trimmed === "" ? null : trimmed);

            setCollection(prev => {
                if (!prev) {
                    return prev;
                }

                const archives = prev.archives ?? prev.Archives ?? [];

                const updatedArchives = archives.map(a => {
                    const currentId = a.id ?? a.Id;
                    if (currentId !== archiveId) {
                        return a;
                    }

                    if (prev.archives) {
                        return { ...a, description: newDescription };
                    }

                    return { ...a, Description: newDescription };
                });

                return prev.archives 
                    ? { ...prev, archives: updatedArchives }
                    : { ...prev, Archives: updatedArchives }
            });

            setArchives(prev => 
                prev.map(a => {
                    const currentId = a.id ?? a.Id;
                    if (currentId !== archiveId) {
                        return a;
                    }

                    return a.description !== undefined
                        ? { ...a, description: newDescription }
                        : { ...a, Description: newDescription }
                })
            );

        } catch (err) {
            setError(err.message)
        }
    }



    
    
    const photos = collection?.photos ?? collection?.Photos ?? [];

    // const totalArchives = archives.length;
    // const totalArchivesSize = archives.reduce(
    //     (sum, archive) => sum + (archive.sizeBytes ?? archive.SizeBytes ?? 0),
    //     0
    // );    

    const totalPhotos = collection?.totalPhotos ?? collection?.TotalPhotos ?? 0;
    const totalPhotosSize = collection?.totalBytes ?? collection?.TotalBytes ?? 0;
    const totalDistance = collection?.totalDistance ?? collection?.TotalDistance ?? 0;
    const totalOriginal = collection?.totalOriginalSizeBytes ?? collection?.TotalOriginalSizeBytes ?? 0;
    const totalStandard = collection?.totalStandardSizeBytes ?? collection?.TotalStandardSizeBytes ?? 0;
    const totalThumb = collection?.totalThumbSizeBytes ?? collection?.TotalThumbSizeBytes ?? 0;

    const totalArchives = collection?.totalArchives ?? collection?.TotalArchives ?? 0;
    const totalArchivesSize = collection?.totalArchivesBytes ?? collection?.TotalArchivesBytes ?? 0;    

    // const slides = photos
    //     .filter(p => (p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl))
    //     .map(p => ({
    //         src: p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl,
    //         alt: p.originalFileName ?? p.OriginalFileName ?? "photo",
    //     }));

    const standardSlides = photos
        .filter(p => (p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl))
        .map(p => ({
            src: p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl,
            alt: p.originalFileName ?? p.OriginalFileName ?? "photo",
        }));

    const originalSlides = photos
        .filter(p => (p.originalUrl ?? p.OriginalUrl ?? p.standardUrl ?? p.StandardUrl))
        .map(p => ({
            src: p.originalUrl ?? p.OriginalUrl ??p.standardUrl ?? p.StandardUrl,
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
        <div className="container py-4">
        {/* <div className="container py-4" style={{ maxWidth: 900 }}> */}
            {/* <div className="card shadow-sm"> */}
                {/* <div className="card-body"> */}
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
                    <p>{collection?.description || "No description of collection yet."}</p>
                    {/* <p>Collection Id: {collection?.id}</p> */}

                    <div className='mb-2'>
                        {!isEditing ? (
                            <>

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
                            onClick={() => navigate(`/collections/${collection.id}/map`)}
                        >
                            Map View
                        </button>

                        <button
                            className='btn btn-primary'
                            onClick={() => {
                                if(standardSlides.length === 0){
                                    setError("No resized photos available for slideshow.");
                                    return;
                                }

                                setError("");
                                setViewerMode("standard");
                                setLightboxIndex(0);
                                setLightboxOpen(true);
                            }}
                        >
                            Slideshow Resized
                        </button>

                        <button
                            className='btn btn-primary'
                            onClick={() => {
                                if(originalSlides.length === 0){
                                    setError("No original photos available for slideshow.");
                                    return;
                                }

                                setError("");
                                setViewerMode("original");
                                setLightboxIndex(0);
                                setLightboxOpen(true);
                            }}>
                            Slideshow Originals
                        </button>                        

                        <button
                            className='btn btn-primary'
                            onClick={downloadStandardZipHandler}
                        >
                            Download Resized ZIP
                        </button>              
                        
                        <button 
                            className='btn btn-primary' 
                            type='button' 
                            data-bs-toggle="collapse" 
                            data-bs-target="#collapseShareLink" 
                            aria-expanded="false" 
                            aria-controls="collapseShareLink"
                        >
                            Share Collection
                        </button>
                    </div>

                    
                    <div className='collapse' id='collapseShareLink'>                            
                        <div className='card card-body mt-3'>
                            
                            <div className='small'>Active share link: No active link yet</div>                                

                            <hr/>                            

                            <div className='d-flex gap-2 flex-wrap'>
                                <button className='btn btn-primary' type='button'
                                >
                                    Copy Link
                                </button>
                                <button className='btn btn-primary' type='button'
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
                                <label className='form-check-label small' htmlFor='disabledCheckBox'>Photo Card: View Original, Location, Coordinates, Description</label>                                
                            </div>                            
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareArchiveCard' checked disabled/>
                                <label className='form-check-label small' htmlFor='shareArchiveCard'>Archive Card: Download, Description</label>                                
                            </div>                            

                            <div className='small mt-3 mb-2'>
                                Additional options for this share link:
                            </div>
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareSlideshowOriginals'/>
                                <label className='form-check-label small' htmlFor='shareSlideshowOriginals'>Slideshow Originals</label>                                
                            </div>
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareDownloadResizedZip'/>
                                <label className='form-check-label small' htmlFor='shareDownloadResizedZip'>Download Resized ZIP</label>                                
                            </div>
                            <div className='form-check form-switch'>                                
                                <input className='form-check-input' type='checkbox' role='switch' id='shareDownloadOriginalFromPhotoCard'/>
                                <label className='form-check-label small' htmlFor='shareDownloadOriginalFromPhotoCard'>Download Original from Photo Card</label>                                
                            </div>

                            <hr/>

                            <div className='d-flex gap-2 flex-wrap'>                                
                                <button className='btn btn-primary' type='button'
                                >
                                    Create New Link
                                </button>
                            </div>                 
                            

                        </div>
                        
                    </div>
                    
                    
                    <hr/>

                    {/* <hr/>                             */}
                    {uploadStatus ? <div className='alert alert-info'>{uploadStatus}</div> : null}

                    <h5>Photos</h5>

                    <label className='form-label'>Upload photos (.jpg, .jpeg)</label>
                        <input
                            type='file'
                            className='form-control mb-3'
                            accept='image/*'
                            multiple
                            disabled={uploading || isEditing}
                            onChange={onFilesSelected}
                        />
                    {/* <hr/>                   */}
                    
                    <div className="d-flex align-items-start justify-content-between small mb-3">
                        <div>
                            Distance by geo tags: {formatDistance(totalDistance)}<br/>
                            Photos: {totalPhotos}<br/>
                            Size: {formatBytes(totalPhotosSize)}
                        </div>
                        <div className='text-end'>                        
                            Originals size: {formatBytes(totalOriginal)}<br/>
                            Resized standards size: {formatBytes(totalStandard)}<br/>
                            Resized thumbnails size: {formatBytes(totalThumb)}<br/>
                        </div>
                    </div>                    

                    {uploading ? (
                        <div className='alert alert-info'>Uploading/Processing... please wait</div>
                    ) : photos.length === 0 ? (
                        <div className='alert alert-info'>No photos uploaded yet</div>
                    ) : (
                        <div className='row'>
                            {photos.map((p, index) => (
                                <div key={p.id ?? p.Id} className='col-6 col-md-4 col-lg-3 mb-3'>
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
                                        }}
                                    />
                                </div>
                            ))}
                        </div>
                    )}

                    
                    <hr/>
                    <h5>Archives</h5>
                    
                    {/* Archives */}
                    <label className='form-label'>Upload archive (.zip)</label>
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
                    </div>
                    

                    {archiveUploadStatus ? (
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
                    ) : null}

                    {/* <hr/> */}
                    {/* Archive Cards */}
                    {archives.length === 0 ? (
                        <div className='alert alert-info'>No archives uploaded yet</div>
                    ) : (
                        <div className='row'>
                            {archives.map((a) => (
                                <div key={a.id ?? a.Id} className='col-6 col-md-4 col-lg-3 mb-3'>
                                    <ArchiveCard
                                        archive={a}
                                        onDownload={downloadArchiveHandler}
                                        onDelete={deleteArchiveHandler}
                                        onSaveDescription={saveArchiveDescriptionHandler}
                                    />


                                    {/* <div className='card shadow-sm h-100 position-relative'>
                                        <div
                                            className="d-flex flex-column align-items-center justify-content-center bg-light"
                                            style={{ width: "100%", height: 80 }}
                                        >
                                            <i className="bi bi-file-earmark-zip" style={{ fontSize: 40 }}></i>                                            
                                        </div>                                            

                                        <div className='card-body p-2'>

                                            
                                            <div className='small text-truncate'>
                                                {a.originalFileName ?? a.OriginalFileName}
                                            </div>
                                            <div className='small text-truncate'>
                                                {formatBytes(a.sizeBytes ?? a.SizeBytes)}
                                            </div>
                                            <div className='small text-truncate'>
                                                {formatTakenAt(a.createdAtUtc ?? a.CreatedAtUtc)}
                                            </div>
                                            
                                            <hr/>
                                            <div className='d-flex flex-wrap gap-1 mt-2'>
                                                <button
                                                    className='btn btn-outline-secondary btn-sm'
                                                    onClick={() => downloadArchiveHandler(a.id ?? a.Id)}
                                                    title='Download archive'
                                                >
                                                    <i className='bi bi-download'></i>
                                                </button>
                                                <button
                                                    className='btn btn-outline-secondary btn-sm'
                                                    onClick={() => deleteArchiveHandler(a.id ?? a.Id, a.originalFileName ?? a.OriginalFileName)}
                                                    title='Delete archive'
                                                >
                                                    <i className='bi bi-trash'></i>
                                                </button>
                                                <button
                                                    className='btn btn-outline-secondary btn-sm'
                                                    title='Edit description'
                                                >
                                                    <i className='bi bi-pencil'></i>
                                                </button>
                                            </div>


                                        </div>
                                    </div> */}
                                </div>
                            ))}                            
                        </div>
                    )}

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
                            delay: viewerMode === "standard" ? 2000 : 4000, 
                            autoplay: true
                        }}                        
                    />

                    
                {/* </div>                 */}
            {/* </div> */}
        </div>        
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
                            style={{ width: "100%", height: 160, objectFit: "cover" }}
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

                {/* Photo original name */}
                <div className="card-body p-2 d-flex flex-column">
                    <div className='d-flex align-items-start justify-content-between'>
                        <div className="small text-truncate">
                            {originalFileName || "(no name)"}                        
                        </div>                
                        {(distanceFromPrevious !== null && distanceFromPrevious > 0) && (
                        <div className="small text-truncate">
                            + {formatDistance(distanceFromPrevious)}    
                        </div>                        
                    )}                   
                    </div>
                    

                    {/* Photo taken */}
                    {takenAt && (
                        <div className="small text-truncate">
                            {formatTakenAt(takenAt)}
                        </div>
                    )}
                    
                    

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
const ArchiveCard = React.memo(function ArchiveCard({
    archive,
    onDownload,
    onDelete,
    onSaveDescription
}) {
    const archiveId = archive.id ?? archive.Id;
    const originalFileName = archive.originalFileName ?? archive.OriginalFileName;
    const sizeBytes = archive.sizeBytes ?? archive.SizeBytes;
    const createdAt = archive.createdAtUtc ?? archive.CreatedAtUtc;
    const initialDescription = archive.description ?? archive.Description ?? "";

    const [isEditingDescriptionArchive, setIsEditingDescriptionArchive] = useState(false);
    const [descriptionArchive, setDescriptionArchive] = useState(initialDescription);

    async function handleSaveDescriptionArchive() {
        await onSaveDescription?.(archiveId, descriptionArchive);
        setIsEditingDescriptionArchive(false);
    }

    return(
        <div className='card shadow-sm h-100 position-relative'>
            <div
                className="d-flex flex-column align-items-center justify-content-center bg-light"
                style={{ width: "100%", height: 80 }}
            >
                <i className="bi bi-file-earmark-zip" style={{ fontSize: 40 }}></i>                                            
            </div>
            <div className='card-body p-2 d-flex flex-column'>
                <div className='small text-truncate'>
                    {originalFileName || "(no name)"}
                </div>
                <div className='small text-truncate'>
                    {formatBytes(sizeBytes)}
                </div>
                <div className='small text-truncate'>
                    {formatTakenAt(createdAt)}
                </div>

                {!isEditingDescriptionArchive 
                    ? descriptionArchive 
                        ? (
                            <div>
                                <hr/>
                                <div className='small text-truncate'>
                                    {descriptionArchive}
                                </div>
                            </div>
                        ) 
                        : null 
                    : (
                        <div className='mt-2'>
                            <hr/>
                            <TextareaAutosize
                                className='form-control form-control-sm'
                                minRows={2}
                                value={descriptionArchive}
                                style={{overflow: "hidden"}}
                                onChange={(e) => setDescriptionArchive(e.target.value)}
                                placeholder='Add archive description'
                            />
                            <div className='d-flex gap-2 mt-2'>
                                <button
                                    className='btn btn-outline-secondary btn-sm'
                                    onClick={handleSaveDescriptionArchive}
                                    title='Save description'
                                >
                                    Save
                                </button>
                                <button
                                    className='btn btn-outline-secondary btn-sm'
                                    onClick={() =>{
                                        setDescriptionArchive(initialDescription);
                                        setIsEditingDescriptionArchive(false);
                                    }}
                                    title='Cancel description'
                                >
                                    Cancel
                                </button>
                            </div>
                        </div>
                )}

                {/* Archive card small buttons block */}
                <hr/>
                <div className='d-flex flex-wrap gap-1 mt-auto'>
                    <button
                        className='btn btn-outline-secondary btn-sm'
                        onClick={() => onDownload?.(archiveId)}
                        title='Download archive'
                    >
                        <i className='bi bi-download'></i>
                    </button>

                    <button
                        className='btn btn-outline-secondary btn-sm'
                        onClick={() => onDelete?.(archiveId, originalFileName)}
                        title='Delete archive'
                    >
                        <i className='bi bi-trash'></i>
                    </button>

                    <button
                        className='btn btn-outline-secondary btn-sm'
                        onClick={() => setIsEditingDescriptionArchive((v) => !v)}
                        title='Edit description'
                    >
                        <i className='bi bi-pencil'></i>
                    </button>                    
                </div>
            </div>
        </div>
    );
});
