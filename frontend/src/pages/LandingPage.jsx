import { Link } from "react-router-dom";
import { getToken } from "../api";
import { useState } from "react";
import ProPlanModal from "../components/ProPlanModal";
import Lightbox from "yet-another-react-lightbox";
import Zoom from "yet-another-react-lightbox/plugins/zoom";
import FaqAccordion from "../components/faqAccordion";
import { sendContactMessage } from "../contactFormApi";
import { Carousel } from "react-bootstrap";

export default function LandingPage(){
    const token = getToken();
    const [showProPlanModal, setShowProPlanModal] = useState(false);
    const [lightboxOpen, setLightboxOpen] = useState(false);
    const [selectedImage, setSelectedImage] = useState(null);
    const [contactForm, setContactForm] = useState({
        senderName: "",
        senderEmail: "",
        subject: "",
        body: "",
    });
    const [contactFormSending, setContactFormSending] = useState(false);
    const [contactFormSuccess, setContactFormSuccess] = useState("");
    const [contactFormError, setContactFormError] = useState("");
    const slides = [
        {
            title: "Memories",
            image: "/images/landing/20250923_190110.jpg"
        },
        {
            title: "Travel",
            image: "/images/landing/20250928_111140.jpg"
        },
        {
            title: "Pet Memories",
            image: "/images/landing/Here_Carousel_Pets_20260411_100038.jpg"
        },
        {
            title: "Family Memories",
            image: "/images/landing/Hero_Carousel_Family_Screenshot_20260228_134943.jpg"
        },
        {
            title: "Vehicle History",
            image: "/images/landing/Hero_Carousel_Car_20260519_085448.jpg"
        },
        {
            title: "Construction Projects",
            image: "/images/landing/Hero_Carousel_Flat_IMG_20220523_122940.jpg"
        },
        {
            title: "Reporting",
            image: "/images/landing/Hero_Carousel_Vessel_Rotterdam_20251021_074557.jpg"
        },
        {
            title: "Inspections",
            image: "/images/landing/Hero_Carousel_Vessel_Holds_DSCN3299.JPG"
        },
        // Memories
        // Travel
        // Pet Memories
        // Family Memories
        // Vehicle History
        // Utility Records
        // Home Renovation
        // Inspections
        // Project Reporting
        // Construction Prohects
    ];


    async function handleContactFormSubmit(event) {
        event.preventDefault();

        setContactFormSending(true);
        setContactFormSuccess("");
        setContactFormError("");

        try {
            const result = await sendContactMessage(contactForm);

            setContactFormSuccess(result.message || "Your message has been sent successfully.");

            setContactForm({
                senderName: "",
                senderEmail: "",
                subject: "",
                body: "",
            });
        } catch (err) {
            setContactFormError(err.message || "Failed to send the message.");
        } finally {
            setContactFormSending(false);
        }
    }

    function handleContactFormChange(event){
        const { name, value } = event.target;

        setContactForm(current => ({
            ...current,
            [name]: value,
        }));
    }
    
    return(
        <>
        <section className="container py-5" id="home">
            <div className="row align-items-center g-4 mb-5">
                <div className="col-12 col-lg-7">                    
                    <h1 className="display-5 fw-semibold mb-3">Store and explore your photo collections</h1>
                    <p className="lead text-muted ">Organize photos into collections, view them on a map, add notes, and share them with secure links.</p>
                    <p className="lead text-muted">Built for travel, field work, inspections, project documentation and much more.</p>
                    

                    <Carousel 
                        fade interval={5000}
                        pause="hover"
                        indicators={false}
                        className="mb-3"
                    >
                        {slides.map((slide) => (
                            <Carousel.Item key={slide.title}>
                                <img
                                    className="d-block w-100 hero-use-case-image"
                                    src={slide.image}
                                    alt={slide.title}
                                />
                                <Carousel.Caption className="pb-0">
                                    <h5>{slide.title}</h5>
                                </Carousel.Caption>                                
                            </Carousel.Item>
                        ))}
                    </Carousel>
                    {/* <img 
                        src="/images/landing/20250923_190110.jpg" 
                alt="Photo" 
                className="img-fluid mb-4 mt-4"                
                            /> */}
                    

                    {!token ? (
                        <div className="d-flex gap-2 flex-wrap">
                            {/* <Link 
                                className="btn btn-outline-primary"
                                to="/register"
                                title="Register as new user"
                            >
                                Create account
                            </Link> */}

                            <Link 
                                className="btn btn-outline-primary"
                                to="/login"
                                title="Sign in to your account"
                            >
                                Sign in
                            </Link>

                            

                            <Link 
                                className="btn btn-outline-success"
                                to="/shared/547530dbfe594b038907d65be19fd7a5"
                                target="_blank"
                                rel="noopener noreferrer"
                                title="Open Demo link of Shared Link"
                            >
                                Demo
                            </Link>
                            {/* <Link 
                                className="btn btn-outline-success"
                                to="/shared/547530dbfe594b038907d65be19fd7a5/map"
                                target="_blank"
                                rel="noopener noreferrer"
                                title="Open Demo link of Shared Link"
                            >
                                Demo Map
                            </Link> */}
                        </div>
                        
                        
                    ) : (
                        <div className="d-flex gap-2 flex-wrap">
                            <Link 
                                className="btn btn-outline-primary"
                                to="/collections"
                                title="Go to your activities page"
                            >
                                Open my collections
                            </Link>
                            <Link 
                                className="btn btn-outline-success"
                                to="/shared/547530dbfe594b038907d65be19fd7a5"
                                target="_blank"
                                rel="noopener noreferrer"
                                title="Open Demo link of Shared Link"
                            >
                                Demo
                            </Link>
                        </div>

                        
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
                                <FeatureLine 
                                    title="Photo collections" 
                                    text="Upload original JPG photos into collections."
                                />
                                <FeatureLine 
                                    title="Interactive map" 
                                    text="View photos on an interactive map when GPS information is available."
                                    // link="http://192.168.1.108:5173/shared/547530dbfe594b038907d65be19fd7a5/map"
                                    // linkText="Open demo shared map"
                                />
                                <FeatureLine 
                                    title="Archives" 
                                    text="Store ZIP archives together with your photo collections."
                                />
                                <FeatureLine 
                                    title="Notes" 
                                    text="Add notes to photos, archives, and collections."
                                />                                
                                <FeatureLine 
                                    title="Sharing" 
                                    text="Share collections using secure read-only links. "
                                    // link="http://192.168.1.108:5173/shared/547530dbfe594b038907d65be19fd7a5"
                                    // linkText="Open demo shared collection"
                                />
                            </div>                            
                        </div>
                    </div>                    
                </div>                
            </div>
        </section>            
                
        <section className="bg-light py-5" id="about">
            <div className="container">
                <div className="mb-3">
                    <h2 className="display-6 text-center mb-4">ABOUT</h2>                    
                    <div className="row justify-content-center">                        
                        <div className="col-12 col-lg-8">
                            <p className="text-muted">Photos are organised into collections for trips, inspections, projects or any other activity. Notes can be added to individual photos and archives, making collections useful for both professional reporting and personal memories. Everything stays easy to browse, manage and securely share.</p>
                        </div>                                                
                    </div>                    
                </div>                

                <div className="row g-3">
                    <div className="col-12 col-lg-4">
                        <ImageCard
                            image="/images/landing/Screenshot-2026-06-29-150758-my-collections.jpg"                            
                            title="My Collections"
                            text="Create and manage your photo collections. View storage statistics."
                            onClick={() =>{
                                setSelectedImage("/images/landing/Screenshot-2026-06-29-150758-my-collections.jpg")
                                setLightboxOpen(true);
                            }}
                        />
                    </div>
                    <div className="col-12 col-lg-4">
                        <ImageCard
                            image="/images/landing/Screenshot-2026-06-29-150857-collection-page.jpg"                            
                            title="Collection Details"
                            text="Manage photos and archives, add notes, see slideshow, and share collection."
                            onClick={() =>{
                                setSelectedImage("/images/landing/Screenshot-2026-06-29-150857-collection-page.jpg")
                                setLightboxOpen(true);
                            }}
                        />
                    </div>
                    <div className="col-12 col-lg-4">
                        <ImageCard
                            image="/images/landing/Screenshot-2026-06-29 151055-map-page.jpg"                            
                            title="Interactive Map"
                            text="Explore photos by location using GPS information."
                            onClick={() =>{
                                setSelectedImage("/images/landing/Screenshot-2026-06-29 151055-map-page.jpg")
                                setLightboxOpen(true);
                            }}
                        />
                    </div>
                </div>                
            </div>
            
        </section>
        

        <section className="container py-5" id="features">
            <div className="mb-4">
                <h2 className="display-6 text-center mb-4">FEATURES</h2>                                    
                <p className="text-center text-muted">Covers the most common workflows for organising, managing and sharing photo collections.</p>
            </div>                
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
                    title="Notes"
                    text="Add notes to individual photos, archives, and collections. Perfect for travel, inspections, reporting and project documentation."
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

        <section className="faq-parallax py-5" id="how-it-works">
            <div className="container">
                <div className="text-center mb-5">
                    <h2 className="display-6 mb-4 text-white">HOW IT WORKS</h2>
                </div>
                <div className="row g-4 align-items-stretch">                    
                    <div className="col-12 col-md-8 d-flex">
                        <div className="card shadow-sm w-100">
                            <div className="card-body">
                                {/* <h5 className="card-title">How it works</h5>                             */}
                                {/* <hr/> */}
                                <ol>
                                    <li>Create an account</li>
                                    <li>Create your collections</li>
                                    <li>Upload JPG photos</li>
                                    <li>Upload ZIP archives (optional)</li>
                                    <li>Add notes to photos, archives, collections</li>
                                    <li>Explore the gallery, map, slideshow and statistics</li>
                                    <li>Share your collections</li>
                                    <li>Disable sharing link at any time</li>

                                </ol>
                            </div>                        
                        </div>                    
                    </div>
                    <div className="col-12 col-md-4 d-flex">      
                        {/* <p>Watch YouTube demo</p>                   */}
                        <div className="ratio ratio-16x9 mb-2 w-100 h-100">                            
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

        <section className="container py-5" id="pricing">
            <div className="text-center mb-4">
                <h2 className=" display-6 mb-4">PRICING</h2>
                <p className="text-muted">Choose the plan that fits your storage needs.</p>                
            </div>

            <div className="row justify-content-center g-4">

                {/* Free */}
                <div className="col-12 col-md-6 col-lg-4">
                    <div className="card shadow-sm h-100">
                        {/* <div className="card-header text-center display-4">
                            Free
                        </div> */}
                        <div className="card-header text-center">
                            <h3 className="mb-0">Free Plan</h3>
                        </div>
                        
                        <div className="card-body d-flex flex-column">
                            <div className="text-center mb-3">
                                <span className="display-4">&#8364;0.00</span>
                                <span className="text-muted"> / month</span>
                            </div>
                            
                            <ul className="list-group list-group-flush mb-3">
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>5 GB cloud storage</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>500 photos per collection</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Interactive maps</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Slideshows</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>ZIP archives</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Photo & archive notes</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Secure sharing</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Storage optimisation</li>
                            </ul>                            

                            <div className="mt-auto text-center">
                                <Link
                                    to="/register"
                                    className="btn btn-outline-secondary"
                                >
                                    Start Free
                                </Link>
                            </div>
                            
                        </div>
                        
                    </div>
                </div>

                {/* Pro */}
                <div className="col-12 col-md-6 col-lg-4">
                    <div className="card shadow-sm h-100">
                        {/* <div className="card-header text-center display-4">
                            Pro
                        </div> */}
                        <div className="card-header text-center">
                            <h3 className="mb-0">Pro Plan</h3>
                        </div>
                        
                        <div className="card-body d-flex flex-column">
                            <div className="text-center mb-3">
                                <span className="display-4">&#8364;4.99</span>
                                <span className="text-muted"> / month</span>
                            </div>
                            
                            <ul className="list-group list-group-flush mb-3">
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>50 GB cloud storage</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>1500 photos per collection</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Interactive maps</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Slideshows</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>ZIP archives</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Photo & archive notes</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Secure sharing</li>
                                <li className="list-group-item"><i className="bi bi-check-lg text-success me-2"></i>Storage optimisation</li>
                            </ul>                            

                            <div className="mt-auto text-center">
                                <button 
                                    className="btn btn-outline-secondary"
                                    onClick={() => setShowProPlanModal(true)}
                                >
                                    Activate Pro
                                </button>
                            </div>
                            
                        </div>
                        
                    </div>
                </div>
            </div>
        </section>

        <section className="faq-parallax py-5" id="faq">
            <div className="container">
                <div className="text-center text-white mb-4">
                    <h2 className="display-6 mb-4">FAQ</h2>                
                    <p>Frequently asked questions.</p>
                </div>
                <div className="row justify-content-center">
                    <div className="col-12 col-lg-8">
                        <FaqAccordion/>
                    </div>
                </div>
            </div>            
        </section>

        {/* Contact Form section */}
        <section className="container py-5" id="contact">
            <div className="text-center mb-4">
                <h2 className="display-6 mb-4">CONTACT</h2>
                {/* <p className="text-muted">
                    Have a question, suggestion, or feedback?<br/>Send me a message.
                </p> */}
            </div>            
            
            <div className="row justify-content-center align-items-start">                
                <div className="col-12 col-lg-4">
                    <div className="card mb-3">
                        <div className="card-body p-4">                    
                        <p className="text-muted">Have a question or need assistance? Send me a message by email or using contact form.</p>                    
                        <div className="d-flex align-items-center">
                            <i className="bi bi-envelope fs-4 text-primary me-2"></i><span className="text-muted">Email: pzalizko@gmail.com</span>
                        </div>
                        {/* <div className="d-flex align-items-center mb-3">
                            <i className="bi bi-geo-alt fs-4 text-primary me-2"></i><span className="text-muted">Location: Constanta, Romania</span>
                        </div> */}                    
                        {/* <div className="d-flex align-items-center">
                            <i className="bi bi-code-slash fs-4 text-primary me-2"></i><span className="text-muted">Developed by Petr Zalizko</span>
                        </div>                     */}
                        </div>
                    </div>       
                </div>
                <div className="col-12 col-lg-5">
                    <div className="card shadow-sm h-100">
                        <div className="card-body">
                            <div className="row justify-content-center">
                                <div className="col-12">
                                    <form onSubmit={handleContactFormSubmit}>
                                        <div className="mb-3">
                                            <label htmlFor="contact-form-name" className="form-label">Name</label>
                                            <input
                                                type="text"
                                                className="form-control"
                                                id="contact-form-name"
                                                name="senderName"                                        
                                                value={contactForm.senderName}
                                                onChange={handleContactFormChange}
                                                maxLength={100}
                                                required
                                            />
                                        </div>
                                        <div className="mb-3">
                                            <label htmlFor="contact-form-email" className="form-label">Email</label>
                                            <input
                                                type="email"
                                                className="form-control"
                                                id="contact-form-email"
                                                name="senderEmail"                                        
                                                value={contactForm.senderEmail}
                                                onChange={handleContactFormChange}
                                                maxLength={200}
                                                required
                                            />
                                        </div>
                                        <div className="mb-3">
                                            <label htmlFor="contact-form-subject" className="form-label">Subject</label>
                                            <input
                                                type="text"
                                                className="form-control"
                                                id="contact-form-subject"
                                                name="subject"                                        
                                                value={contactForm.subject}
                                                onChange={handleContactFormChange}
                                                maxLength={200}
                                                required
                                            />
                                        </div>
                                        <div className="mb-3">
                                            <label htmlFor="contact-form-message" className="form-label">Message</label>
                                            <textarea
                                                className="form-control"
                                                id="contact-form-message"
                                                name="body"                                        
                                                value={contactForm.body}
                                                onChange={handleContactFormChange}
                                                rows={5}
                                                maxLength={5000}
                                                required
                                            />                                    
                                            <div className="form-text text-end">
                                                {contactForm.body.length} / 5000
                                            </div>
                                        </div>

                                        {contactFormSuccess && (
                                            <div className="alert alert-success" role="alert">
                                                {contactFormSuccess}
                                            </div>
                                        )}

                                        {contactFormError && (
                                            <div className="alert alert-danger" role="alert">
                                                {contactFormError}
                                            </div>
                                        )}

                                        <div className="text-center">
                                            <button 
                                                type="submit" 
                                                className="btn btn-primary"
                                                disabled={contactFormSending}
                                            >
                                                Send Message
                                            </button>
                                        </div>                                
                                    </form>
                                </div>
                            </div>                    
                        </div>                        
                    </div>                    
                </div>
            </div>
        </section>        

        <footer className="border-top">            
            <div className="container py-4 d-flex justify-content-between align-items-center">
                <div className="text-muted small">                    
                    <div>
                        PhotoMap © 2026
                    </div>
                    <div>
                        <Link to="/privacy" className="text-decoration-none text-muted">
                            Privacy Policy
                        </Link>
                    </div>
                    <div>
                        <Link to="/terms" className="text-decoration-none text-muted">
                            Terms of Service
                        </Link>
                    </div>
                    <div>
                        <Link to="/feedback" className="text-decoration-none text-muted">
                            Feedback
                        </Link>
                    </div>
                    
                    
                </div>

                <div className="d-flex gap-3">
                    <a 
                        href="https://www.linkedin.com/in/petr-zalizko/" 
                        target="_blank" 
                        rel="noreferrer noopener">
                        <i className="bi bi-linkedin fs-5 text-primary"></i>
                    </a>
                    <a 
                        href="https://github.com/Peter42306" 
                        target="_blank" 
                        rel="noreferrer noopener">
                        <i className="bi bi-github fs-5 text-black"></i>
                    </a>
                    <a 
                        href="https://www.youtube.com/playlist?list=PLO3w_Jmi7YCpGQnorvV_eJu1DU3V9xrja" 
                        target="_blank" 
                        rel="noreferrer noopener">
                        <i className="bi bi-youtube fs-5 text-danger"></i>
                    </a>
                    <a 
                        href="mailto:pzalizko@gmail.com" 
                        target="_blank" 
                        rel="noreferrer noopener">
                        <i className="bi bi-envelope fs-5 text-primary"></i>
                    </a>
                </div>
            </div>
        </footer>

        <ProPlanModal
            show={showProPlanModal}
            onHide={() => setShowProPlanModal(false)}
        />
        <Lightbox
            open={lightboxOpen}
            close={() => setLightboxOpen(false)}
            slides={[
                { src: selectedImage }
            ]}
            carousel={{
                finite: true
            }}
            plugins={[Zoom]}
        />
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

function FeatureLine({ title, text, link, linkText }) {
    return(
        <div>
            <h5>{title}</h5>            
            <div className="text-muted">{text}</div>
            {link && (
                <Link 
                    to={link}
                    target="_blank"
                    rel="noopener noreferrer"
                    className="text-decoration-none"
                    
                >
                    {linkText} &rarr;
                </Link>
            )}
        </div>
    );
}

function ImageCard({ image, title, text, onClick }) {
    return(
        <div className="card shadow-sm overflow-hidden h-100">
            <img 
                src={image} 
                alt={title ?? "Photo"} 
                className="img-fluid"
                style={{ cursor: "pointer" }}
                onClick={onClick}
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