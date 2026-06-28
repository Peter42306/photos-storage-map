import { Link } from "react-router-dom";
import { getToken } from "../api";

export default function LandingPage(){
    const token = getToken();
    
    return(
        <div className="container py-4">
            <section className="row align-items-center g-4 mb-5">
                <div className="col-12 col-lg-7">                    
                    <h1 className="display-5 fw-semibold mb-3">Store, share and explore your photo collections on a map</h1>
                    <p className="lead text-muted ">Upload JPG photos, keep collections and archives together, view GPS locations, add notes, download resized ZIP files, and share selected collections.</p>

                    {!token ? (
                        <div className="d-flex gap-2 flex-wrap">
                            <Link 
                                className="btn btn-outline-primary"
                                to="/register"
                                title="Register as new user"
                            >
                                Create account
                            </Link>

                            <Link 
                                className="btn btn-outline-primary"
                                to="/login"
                                title="Sign in to your account"
                            >
                                Sign in
                            </Link>
                        </div>
                        
                    ) : (
                        <Link 
                            className="btn btn-outline-primary"
                            to="/collections"
                            title="Go to your activities page"
                        >
                            Open my collections
                        </Link>
                    )}
                        
                        {/* <Link className="btn btn-outline-secondary" to="/login">Sign in</Link> */}
                    
                </div>

                <div className="col-12 col-lg-5">
                    <div className="card shadow-sm">
                        <div className="card-body p-4">
                            <h5>What you can do</h5>
                            <hr/>
                            <div className="d-flex flex-column gap-3">
                                <FeatureLine title="Photo collections" text="Upload and organize original photos."/>
                                <FeatureLine title="Map view" text="Show photos on a map when GPS data is available."/>
                                <FeatureLine title="Archives" text="Store ZIP archives together with collections."/>
                                <FeatureLine title="Sharing" text="Create read-only links for selected collections."/>
                            </div>                            
                        </div>
                    </div>
                </div>                
            </section>

            <section className="mb-5">
                <div className="row g-3">
                    <div className="col-12 col-lg-4">
                        <div className="card shadow-sm overflow-hidden">
                            <img
                                src="/images/landing/20260531_224234.jpg"
                                alt="PhotosStorageMap collection page"
                                className="img-fluid"
                            />
                            <div className="card-body">
                                <h5 className="mb-1">Collections, maps and archives</h5>
                                <div className="text-muted">
                                    Organize photos, view GPS locations and share collections.
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="col-12 col-lg-4">
                        <div className="card shadow-sm overflow-hidden">
                            <img
                                src="/images/landing/20260531_224234.jpg"
                                alt="PhotosStorageMap collection page"
                                className="img-fluid"
                            />
                            <div className="card-body">
                                <h5 className="mb-1">Collections, maps and archives</h5>
                                <div className="text-muted">
                                    Organize photos, view GPS locations and share collections.
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="col-12 col-lg-4">
                        <div className="card shadow-sm overflow-hidden">
                            <img
                                src="/images/landing/20260531_224234.jpg"
                                alt="PhotosStorageMap collection page"
                                className="img-fluid"
                            />
                            <div className="card-body">
                                <h5 className="mb-1">Collections, maps and archives</h5>
                                <div className="text-muted">
                                    Organize photos, view GPS locations and share collections.
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                
            </section>

            

            <section className="mb-5">
                <div className="row g-3">
                    <FeatureCard
                        icon="bi-images"
                        title="Photo storage"
                        text="Upload JPG/JPEG photos, generate resized versions and thumbnails, and keep the original files in storage."
                    />
                    <FeatureCard
                        icon="bi-geo-alt"
                        title="GPS and map"
                        text="View photo locations, distance between photos, and total distance based on available geolocation metadata. "
                    />
                    <FeatureCard
                        icon="bi-card-text"
                        title="Notes and description"
                        text="Add comments to photos and archives. Useful for travel notes, inspections, reports, or project documentation."
                    />
                    <FeatureCard
                        icon="bi-file-earmark-zip"
                        title="ZIP Archives"
                        text="Upload ZIP archives, download resized photo ZIP files, and keep additional project files near the photo collection."
                    />
                    <FeatureCard
                        icon="bi-share"
                        title="Shared collections"
                        text="Share collections with public read-only links. Choose what can be viewed or downloaded."
                    />
                    <FeatureCard
                        icon="bi-cloud-arrow-up"
                        title="Cloud storage"
                        text="Files are stored in S3-compatible object storage, while metadata is managed by the application."
                    />
                </div>
            </section>            

            <section className="row g-4 align-items-center mb-5">
                <div className="col-12 col-md-6">
                    <h3>Designed for real photo workflows</h3>
                    <p>PhotosStorageMap is useful when photos are more than just images: trips, inspections, surveys, field work, technical reports, and any case where for your reporting location and notes matter.</p>
                </div>

                <div className="col-12 col-md-6">
                    <div className="card shadow-sm">
                        <div className="card-body">
                            <h5 className="card-title">How it works</h5>                            
                            <hr/>
                            <ol>
                                <li>Create an account</li>
                                <li>Create a photo collection</li>
                                <li>Upload JPG photos and ZIP archives</li>
                                <li>View gallery, map, slideshow and statistics</li>
                                <li>Share the collections</li>
                            </ol>
                        </div>                        
                    </div>
                    
                </div>
            </section>

            <section className="border-top pt-4">
                <div className="d-flex flex-column flex-md-row justify-content-between gap-3 text-muted small">
                    <div>
                        PhotosStorageMap © 2026
                    </div>
                </div>
            </section>
            
        </div>
    );
}

function FeatureCard({ icon, title, text}) {
    return (
        <div className="col-12 col-md-6 col-lg-4">
            <div className="card h-100 shadow-sm">
                <div className="card-body">
                    <div className="mb-3">
                        <i className={`bi ${icon} fs-3 text-primary`}></i>
                    </div>
                    <h5 className="card-title">{title}</h5>
                    <p className="card-text text-muted mb-0">{text}</p>
                </div>
            </div>
        </div>
    )
}

function FeatureLine({ title, text }) {
    return(
        <div>
            <div className="fw-semibold">{title}</div>
            <div className="text-muted">{text}</div>
        </div>
    )
}