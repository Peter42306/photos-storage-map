import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import { downloadSharedCollectionStandardZip, downloadSharedStandardZipJob, getSharedCollection, getSharedStandardZipJobStatus, startSharedStandardZipJob } from "../api";
import { configMLGL } from "@maptiler/sdk";
import Lightbox from "yet-another-react-lightbox";
import "yet-another-react-lightbox/styles.css";
import "yet-another-react-lightbox/plugins/counter.css";
import { Counter, Fullscreen, Slideshow, Zoom } from "yet-another-react-lightbox/plugins";
import ZipProgressBar from "../components/ZipProgressBar";

function formatBytes(bytes) {
    if (!bytes) return "0 B";

    const sizes = ["B", "KB", "MB", "GB"];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));

    return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
}

function formatDistance(meters) {
    if (meters == null) return "";

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
    }).format(date);
}

export default function SharedCollectionPage() {
    const { token } = useParams();

    const [collection, setCollection] = useState(null);
    const [shareLink, setShareLink] = useState(null);

    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const [lightboxOpen, setLightboxOpen] = useState(false);
    const [lightboxIndex, setLightboxIndex] = useState(0);
    const [viewerMode, setViewerMode] = useState("standard");

    const navigate = useNavigate();

    const [zipPreparing, setZipPreparing] = useState(false);

    const [zipStatus, setZipStatus] = useState("");
    const [zipProgress, setZipProgress] = useState(0);
    const [zipProcessedFiles, setZipProcessedFiles] = useState(0);
    const [zipTotalFiles, setZipTotalFiles] = useState(0);


    async function load() {
        try {
            setError("");
            setLoading(true);

            const data = await getSharedCollection(token);

            setCollection(data?.collection ?? data?.Collection ?? null);
            setShareLink(data?.shareLink ?? data?.ShareLink ?? null);

        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }              
    }

    useEffect(() => {            
            if (!token) return;
            load();
    }, [token]);

    const photos = collection?.photos ?? collection?.Photos ?? [];
    const archives = collection?.archives ?? collection?.Archives ?? [];

    const allowSlideshowOriginals = shareLink?.allowSlideshowOriginals ?? shareLink?.AllowSlideshowOriginals ?? false;
    const allowDownloadResizedZip = shareLink?.allowDownloadResizedZip ?? shareLink?.AllowDownloadResizedZip ?? false;
    const allowDownloadOriginalFromCard = shareLink?.allowDownloadOriginalFromCard ?? shareLink?.AllowDownloadOriginalFromCard ?? false;

    const totalPhotos = collection?.totalPhotos ?? collection?.TotalPhotos ?? 0;
    const totalDistance = collection?.totalDistance ?? collection?.TotalDistance ?? 0;
    const totalArchives = collection?.totalArchives ?? collection?.TotalArchives ?? 0;

    const standardSlides = photos
        .filter(p => (p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl))
        .map(p => ({
            src: p.standardUrl ?? p.StandardUrl ?? p.thumbUrl ?? p.ThumbUrl,
            alt: p.originalFileName ?? p.OriginalFileName ?? "photo",
        }));
    
    const originalSlides = photos
        .filter(p => (p.originalUrl ?? p.OriginalUrl ?? p.standardUrl ?? p.StandardUrl))
        .map(p => ({
            src: p.originalUrl ?? p.OriginalUrl ?? p.standardUrl ?? p.StandardUrl,
            alt: p.originalFileName ?? p.OriginalFileName ?? "photo",
        }));
    
    const slides = viewerMode === "original" ? originalSlides : standardSlides;

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

    async function copyCoordinatesHandler(photo) {
        const latitude = photo.latitude ?? photo.Latitude;
        const longitude = photo.longitude ?? photo.Longitude;

        if (latitude == null || longitude == null) return;

        try {
            await navigator.clipboard.writeText(`${latitude}, ${longitude}`);
        } catch {
            alert("Failed to copy coordinates.");
        }
    }

    function viewOriginalHandler(photo) {
        const url = photo.originalUrl ?? photo.OriginalUrl;
        if (url) window.open(url, "_blank");
    }

    function downloadOriginalHandler(photo) {
        const url = photo.originalUrl ?? photo.OriginalUrl;
        if (url) window.location.href = url;
    }

    function downloadArchiveHandler(archive) {
        const url = archive.downloadUrl ?? archive.DownloadUrl;
        if (url) window.location.href = url;
    }

    function downloadResizedZipHandler() {
        const confirmed = confirm("Download all resized photos as ZIP archive?");
        if (!confirmed) {
            return;
        }
        
        setError("");
        setZipPreparing(true);

        downloadSharedCollectionStandardZip(token);        
    }

    // const sleep = (ms) => new Promise((r) => setTimeout(r, ms));

    // async function downloadResizedZipHandler() {
    //     const confirmed = confirm("Download all resized photos as ZIP archive?");
    //     if (!confirmed) return;        
        
    //     try {
    //         setError("");
    //         setZipStatus("Starting ZIP archive...");
    //         setZipProgress(0);
    //         setZipProcessedFiles(0);
    //         setZipTotalFiles(0);

    //         const started = await startSharedStandardZipJob(token);
    //         const jobId = started.jobId ?? started.JobId;

    //         while(true){
    //             const status = await getSharedStandardZipJobStatus(jobId);

    //             const currentStatus = status.status ?? status.Status;
    //             const percent = status.percent ?? status.Percent ?? 0;
    //             const processedFiles = status.processedFiles ?? status.ProcessedFiles ?? 0;
    //             const totalFiles = status.totalFiles ?? status.TotalFiles ?? 0;

    //             setZipProgress(percent);
    //             setZipProcessedFiles(processedFiles);
    //             setZipTotalFiles(totalFiles);
    //             setZipStatus(`Preparing ZIP archive: ${percent}%`);

    //             if (currentStatus === "Ready") {
    //                 setZipProgress(100);
    //                 setZipStatus("ZIP archive is ready. Starting download...");
    //                 downloadSharedStandardZipJob(jobId);

    //                 setTimeout(() => {
    //                     setZipStatus("");
    //                     setZipProgress(0);
    //                     setZipProcessedFiles(0);
    //                     setZipTotalFiles(0);
    //                 }, 3000);

    //                 break;
    //             }

    //             if (currentStatus === "Failed") {
    //                 throw new Error(status.error ?? status.Error ?? "ZIP archive creation failed.")
    //             }

    //             await sleep(1000);
    //         }
    //     } catch (err) {
    //         setError(err.message);
    //         setZipStatus("");
    //     } 
    // }







    if (loading) {
        return(
            <div className="container py-4">
                <div className="alert alert-info">
                    Loading shared collection
                </div>
            </div>
        );
    }

    if (error) {
        return(
            <div className="container py-4">
                <div className="alert alert-danger">
                    {error}
                </div>
            </div>
        );
    }

    if (!collection) {
        return(
            <div className="container py-4">
                <div className="alert alert-warning">
                    Shared collection not found.
                </div>
            </div>
        );
    }

    return(
        <div className="container py-4">                        
            <h2>{collection?.title ?? collection?.Title ?? "-"}</h2>
            <hr/>
            {/* <p>{collection.description ?? collection.Description ?? "No description yet."}</p> */}
            <p>{collection?.description ?? collection?.Description ?? "No description yet."}</p>
            <hr/>
            <div className="d-flex gap-2 flex-wrap">
                <button
                    className="btn btn-primary"
                    onClick={() => navigate(`/shared/${token}/map`)}
                >
                    Map View
                </button>

                <button
                    className="btn btn-primary"
                    onClick={() => {
                        if (standardSlides.length === 0) {
                            setError("No resized photos available for slides.");
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

                {allowSlideshowOriginals && (
                    <button
                        className="btn btn-primary"
                        onClick={() => {
                            if (originalSlides.length === 0) {
                                setError("No original photos available for slides.");
                                return;
                            }

                            setError("");
                            setViewerMode("original");
                            setLightboxIndex(0);
                            setLightboxOpen(true);
                        }}                        
                    >
                        Slideshow Originals
                    </button>
                )}

                {allowDownloadResizedZip && (
                    <button
                        className="btn btn-primary"
                        onClick={downloadResizedZipHandler}
                    >
                        Download Resized ZIP
                    </button>
                )}
            </div>

            {zipPreparing && (
                <div className="alert alert-info mt-3">
                    Preparing ZIP archive...

                    <div className="progress mt-2">
                        <div
                            className="progress-bar progress-bar-striped progress-bar-animated"
                            role="progressbar"
                            style={{ width: "100%"}}
                        />
                    </div>                    
                </div>
            )}

            <ZipProgressBar
                status={zipStatus}
                percent={zipProgress}
                processedFiles={zipProcessedFiles}
                totalFiles={zipTotalFiles}
            />
            
            <hr/>

            {/* Photo cards */}

            <h5>Photos</h5>

            <div className="small mb-3">
                Distance by geo tags: {formatDistance(totalDistance)}<br/>
                Total photos: {totalPhotos}
            </div>

            
            {photos.length === 0 ? (
                <div className="alert alert-info">No photos available.</div>
            ) : (
                <div className="row">
                    {photos.map((p, index) => (
                        <div key={p.id ?? p.Id} className="col-6 col-md-4 col-lg-3 mb-3">
                            <SharedPhotoCard
                                photo={p}
                                onViewOriginal={viewOriginalHandler}
                                onDownloadOriginal={allowDownloadOriginalFromCard ? downloadOriginalHandler : null}
                                onLocation={showLocationHandler}
                                onCopyCoordinates={copyCoordinatesHandler}
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

            <div className="small mb-3">
                Total archives: {totalArchives}
            </div>

            {archives.length === 0 ? (
                <div>No archives available.</div>
            ) : (
                <div className="row">
                    {archives.map((a) => (
                        <div key={a.id ?? a.Id} className="col-6 col-md-4 col-lg-3 mb-3">
                            <SharedArchiveCard
                                archive={a}
                                onDownload={downloadArchiveHandler}
                            />
                        </div>
                    ))}
                </div>
            )}

            
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

        </div>
    );
}

// Photo Card Component
function SharedPhotoCard({
    photo,
    onViewOriginal,
    onDownloadOriginal,
    onLocation,    
    onCopyCoordinates,
    onOpenLightBox
}) {    
    const thumbUrl = photo.thumbUrl ?? photo.ThumbUrl;
    const originalFileName = photo.originalFileName ?? photo.OriginalFileName;
    const description = photo.description ?? photo.Description;
    const latitude = photo.latitude ?? photo.Latitude;
    const longitude = photo.longitude ?? photo.Longitude;
    const distanceFromPrevious = photo.distanceFromPrevious ?? photo.DistanceFromPrevious;
    const takenAt = photo.takenAt ?? photo.TakenAt;

    return(
        <div className="card shadow-sm h-100 position-relative">
            
            {/* Photo */}
            {thumbUrl ? (
                <div onClick={onOpenLightBox} style={{cursor: "pointer"}}>
                    <img
                        src={thumbUrl}
                        alt={originalFileName || "photo"}
                        loading="lazy"
                        style={{ width: "100%", height: 160, objectFit: "cover" }}
                    />
                </div>
            ) : (
                <div>
                    <span>No photo</span>
                </div>
            )}

            {/* Photo original name */}
            <div className="card-body p-2 d-flex flex-column">
                
                <div className="d-flex align-items-start justify-content-between">
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

                {/* Description */}
                {description && (
                    <div className="small">
                        <hr/>
                        {description}
                    </div>
                )}

                <hr/>

                <div className="d-flex flex-wrap gap-1 mt-auto">
                    <button
                        className="btn btn-outline-secondary btn-sm"
                        onClick={() => onViewOriginal?.(photo)}
                        title="View original"
                    >
                        <i className="bi bi-eye"></i>
                    </button>

                    {onDownloadOriginal && (
                        <button
                            className="btn btn-outline-secondary btn-sm"
                            onClick={() => onDownloadOriginal?.(photo)}
                            title="Download original"
                        >
                            <i className="bi bi-download"></i>
                        </button>
                    )}

                    {latitude != null && longitude != null && (
                        <>
                            <button
                                className="btn btn-outline-secondary btn-sm"
                                onClick={() => onLocation?.(photo)}
                                title="Show location"
                            >
                                <i className="bi bi-geo-alt"></i>
                            </button>                             
                            <button
                                className="btn btn-outline-secondary btn-sm"
                                onClick={() => onCopyCoordinates?.(photo)}
                                title="Copy coordinates"
                            >
                                <i className="bi bi-copy"></i>
                            </button>                             
                        </>
                    )}
                </div>
                
            </div>


        </div>
    );
}

// Archive Card Component
function SharedArchiveCard({
    archive,
    onDownload
}) {
    const originalFileName = archive.originalFileName ?? archive.OriginalFileName;
    const sizeBytes = archive.sizeBytes ?? archive.SizeBytes;
    const createdAt = archive.createdAtUtc ?? archive.CreatedAtUtc;
    const description = archive.description ?? archive.Description;

    return(
        <div className="card shadow-sm h-100 position-relative">
            <div
                className="d-flex flex-column align-items-center justify-content-center bg-light"
                style={{width: "100%", height: 80 }}
            >
                <i className="bi bi-file-earmark-zip" style={{ fontSize: 40 }}></i>
            </div>

            <div className="card-body p-2 d-flex flex-column">
                <div className="small text-truncate">
                    {originalFileName || "(no name)"}
                </div>

                <div className="small text-truncate">
                    {formatBytes(sizeBytes)}
                </div>

                <div className="small text-truncate">
                    {formatTakenAt(createdAt)}
                </div>

                {description && (
                    <div>
                        <hr/>
                        <div className="small">{description}</div>
                    </div>
                )}

                <hr/>

                <div className="d-flex flex-wrap gap-1 mt-auto">
                    <button
                        className="btn btn-outline-secondary btn-sm"
                        onClick={() => onDownload?.(archive)}
                        title="Download archive"
                    >
                        <i className="bi bi-download"></i>
                    </button>
                </div>
            </div>
        </div>
    );
}