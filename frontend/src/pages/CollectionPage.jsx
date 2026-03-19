import TextareaAutosize from 'react-textarea-autosize';
import React, { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { completeUpload, deleteCollection, deletePhoto, downloadCollectionStandardZip, getCollection, getOriginalDownloadUrl, getOriginalUrl, getPhotoStatus, getThumbUrl, getToken, initUpload, putToPresignedUrl, updateCollection, updatePhotoDescription } from "../api";



//const S3_BASE = "https://hel1.your-objectstorage.com/photos-storage-map";

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

    

    async function load() {
        console.log("load() called, id =", id);

        try {
            setError("");
            setLoading(true);

            console.log("load() called, before getCollection")
            const data = await getCollection(id);

            console.log("load() called, after getCollection, response data: ", data);
            setCollection(data);
            setTitle(data?.title ?? "");
            setDescription(data?.description ?? "");
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

            setCollection(prev => {
                if (!prev) {
                    return prev;
                }

                const photos = prev.photos ?? prev.Photos ?? [];
                const newPhotos = photos.filter(p => (p.id ?? p.Id) !== photoId);

                return prev.photos ? { ...prev, photos: newPhotos } : { ...prev, Photos: newPhotos }
                
            })
        } catch (err) {
            setError(err.message)
        }
    }    

    async function viewOriginalHandler(photoId, fileName) {
        const confirmed = confirm(`View original in browser?\n${fileName ?? "photo"}`);
        if (!confirmed) {
            return;
        }

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

    async function downloadOriginalHandler(photoId, fileName) {
        const confirmed = confirm(`Download original file?\n${fileName ?? "photo"}`);
        if (!confirmed) {
            return;
        }

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



    
    
    const photos = collection?.photos ?? collection?.Photos ?? [];

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
                    <p>{collection?.description || "-"}</p>
                    {/* <p>Collection Id: {collection?.id}</p> */}

                    <div className='mb-2'>
                        {!isEditing ? (
                            <button
                                className='btn btn-outline-secondary'
                                onClick={() => setIsEditing(true)}
                            >Edit Collection Title & Description</button>
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
                    <div className='d-flex gap-2'>
                        <button
                            className='btn btn-primary'
                            onClick={() => navigate(`/collections/${collection.id}/map`)}
                        >
                            Map view
                        </button>
                        <button className='btn btn-primary'>
                            Download originals
                        </button>
                        <button
                            className='btn btn-primary'
                            onClick={downloadStandardZipHandler}
                        >
                            Download standard
                        </button>
                        <button className='btn btn-primary'>
                            Share link
                        </button>
                        
                    </div>
                    <hr/>

                    {/* <hr/>                             */}
                            {uploadStatus ? <div className='alert alert-info'>{uploadStatus}</div> : null}

                            <label className='form-label'>Upload photos</label>
                            <input
                                type='file'
                                className='form-control'
                                accept='image/*'
                                multiple
                                disabled={uploading || isEditing}
                                onChange={onFilesSelected}
                            />
                            <hr/>                  

                    {uploading ? (
                        <div className='alert alert-info'>Uploading/Processing... please wait</div>
                    ) : photos.length === 0 ? (
                        <div className='alert alert-info'>No photos uploaded yet</div>
                    ) : (
                        <div className='row'>
                            {photos.map((p) => (
                                <div key={p.id ?? p.Id} className='col-6 col-md-4 col-lg-3 mb-3'>
                                    <PhotoCard 
                                        photo={p} 
                                        onDeleted={deletePhotoHandler} 
                                        onViewOriginal={viewOriginalHandler}
                                        onDownloadOriginal={downloadOriginalHandler}
                                        onLocation={showLocationHandler}
                                        onSaveDescription={savePhotoDescriptionHandler}
                                    />
                                </div>
                            ))}
                        </div>
                    )}

                    
                    <hr/>
                    
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
        onSaveDescription 
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
                    <img
                        src={thumbUrl}
                        alt={originalFileName || "photo"}
                        loading='lazy'
                        style={{ width: "100%", height: 160, objectFit: "cover" }}
                    />
                ) : (
                    <div 
                        className="d-flex align-items-center justify-content-center" 
                        style={{ height: 160}}
                    >
                        <span>{status}</span>
                    </div>
                )}

                {/* Photo original name */}
                <div className="card-body p-2">
                    <div className="small text-truncate">
                        {originalFileName || "(no name)"}
                    </div>                

                    {/* Photo description */}
                    {!isEditingDescriptionPhoto 
                        ? (descriptionPhoto) 
                            ? (<div className='small text-muted text-truncate'>{descriptionPhoto}</div>) 
                            : null
                        : (
                            <div className='mt-2'>
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
                    <div className="d-flex flex-wrap gap-1 mt-2">
                        <button
                            // className='btn btn-outline-danger btn-sm'
                            className='btn-close position-absolute top-0 end-0 m-2'
                            onClick={() => onDeleted?.(photoId, photo.originalFileName)}
                            title='Delete photo'
                        >                            
                        </button>

                        <button
                            className='btn btn-outline-secondary btn-sm'
                            onClick={() => onViewOriginal?.(photoId, photo.originalFileName)}
                            title='View original'
                        >
                            <i className='bi bi-eye'></i>
                        </button>

                        <button
                            className='btn btn-outline-secondary btn-sm'
                            onClick={() => onDownloadOriginal?.(photoId, photo.originalFileName)}
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