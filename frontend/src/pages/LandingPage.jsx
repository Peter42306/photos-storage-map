import { Link } from "react-router-dom";
import { getToken } from "../api";

export default function LandingPage(){
    const token = getToken();
    
    return(
        <>
        <section className="container py-5">
            <div className="row align-items-center g-4 mb-5">
                <div className="col-12 col-lg-7">                    
                    <h1 className="display-5 fw-semibold mb-3">Store and explore your photo collections</h1>
                    <p className="lead text-muted ">PhotoMap is designed for people who need more than just a photo gallery. Whether you're traveling, documenting inspections, working in the field, or creating project records, your photos stay connected with locations, notes, and related archive files.</p>
                    <p className="lead text-muted ">Upload original photos to secure cloud storage, organize them into collections, and explore GPS locations on an interactive map.</p>                    
                    <p className="lead text-muted ">Add notes to photos and archives, download resized ZIP files, and share your collections through secure read-only links.</p>

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
                            {/* <h5>WHAT YOU CAN DO</h5> */}
                            <h4 >What you can do</h4>
                            <hr/>
                            <div className="d-flex flex-column gap-3">
                                <FeatureLine title="Photo collections" text="Upload original JPG photos into collections."/>
                                <FeatureLine title="Interactive map" text="View photos on an interactive map when GPS information is available."/>
                                <FeatureLine title="Archives" text="Store ZIP archives together with your photo collections."/>
                                <FeatureLine title="Notes" text="Add notes to photos and archives."/>                                
                                <FeatureLine title="Sharing" text="Share collections using secure read-only links."/>
                            </div>                            
                        </div>
                    </div>
                </div>                
            </div>
        </section>            
                
        <section className="bg-secondary py-5">
            <div className="container">
                <div className="row g-3">
                <div className="col-12 col-lg-4">
                    <ImageCard
                        image="/images/landing/Screenshot-2026-06-29-150758-my-collections.jpg"                            
                        title="My Collections"
                        text="Create and manage your photo collections. View storage statistics."
                    />
                </div>
                <div className="col-12 col-lg-4">
                    <ImageCard
                        image="/images/landing/Screenshot-2026-06-29-150857-collection-page.jpg"                            
                        title="Collection Details"
                        text="Manage photos and archives, add notes, see slideshow, and share collection."
                    />
                </div>
                <div className="col-12 col-lg-4">
                    <ImageCard
                        image="/images/landing/Screenshot-2026-06-29 151055-map-page.jpg"                            
                        title="Interactive Map"
                        text="Explore photos by location using GPS information."
                    />
                </div>
            </div>                
            </div>
            
        </section>
        

        <section className="container py-5">
            <div className="row g-3">
                <FeatureCard
                    icon="bi-images"
                    title="Photo storage"
                    text="Upload original JPG/JPEG photos, generate resized versions and thumbnails automatically."
                />
                <FeatureCard
                    icon="bi-geo-alt"
                    title="GPS and map"
                    text="View photo locations, measure distances between photos, and calculate the total distance using GPS metadata. "
                />
                <FeatureCard
                    icon="bi-card-text"
                    title="Notes and description"
                    text="Add notes to photos and archives. Perfect for travel, inspections, reports, and project documentation."
                />
                <FeatureCard
                    icon="bi-file-earmark-zip"
                    title="ZIP Archives"
                    text="Upload ZIP archives, download resized photo ZIP files, and keep related project files together with your collections."
                />
                <FeatureCard
                    icon="bi-share"
                    title="Shared collections"
                    text="Share your collections using a public read-only link. Anyone with the link always sees the latest version of your collection. You can disable the sharing link at any time."
                />
                <FeatureCard
                    icon="bi-cloud-arrow-up"
                    title="Cloud storage"
                    text="Photos are securely stored in S3-compatible object storage, while metadata is managed by the application."
                 />
            </div>
        </section>            

        <section className="bg-info py-5">
            <div className="container">
                <div className="row g-4 align-items-center">                    
                    <div className="col-12 col-md-7">
                        <div className="card shadow-sm">
                            <div className="card-body">
                                <h5 className="card-title">How it works</h5>                            
                                <hr/>
                                <ol>
                                    <li>Create an account</li>
                                    <li>Create your collections</li>
                                    <li>Upload JPG photos</li>
                                    <li>Upload ZIP archives (optional)</li>
                                    <li>Add notes to photos and archives</li>
                                    <li>Explore the gallery, map, slideshow and statistics</li>
                                    <li>Share your collections</li>
                                    <li>Stop sharing at any time</li>

                                </ol>
                            </div>                        
                        </div>                    
                    </div>
                    <div className="col-12 col-md-5">      
                        {/* <p>Watch YouTube demo</p>                   */}
                        <div className="ratio ratio-16x9 mb-2">                            
                            <iframe
                                src="https://www.youtube-nocookie.com/embed/w-mKc8zRkAc?rel=0"
                                title="Brief Application Demo"
                                loading="lazy"
                                allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                                allowFullScreen>                                
                            </iframe>
                        </div>
                    </div>
                </div>
            </div>
        </section>       

        <footer>
            <div className="container py-4 text-muted small">
                <div>
                    PhotoMap © 2026
                </div>
            </div>
        </footer>
        </>        
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
    );
}

function FeatureLine({ title, text }) {
    return(
        <div>
            <h5>{title}</h5>
            {/* <div className="fw-semibold">{title}</div> */}
            <div className="text-muted">{text}</div>
        </div>
    );
}

function ImageCard({ image, title, text }) {
    return(
        <div className="card shadow-sm overflow-hidden h-100">
            <img 
                src={image} 
                alt={title ?? "Photo"} 
                className="img-fluid"
            />
            <div className="card-body">                    
                {title && (                        
                    <h5>{title}</h5>                                                    
                )}
                {text && (
                    <div className="text-muted">
                        {text}                            
                    </div>
                )}
            </div>
        </div>        
    );
}