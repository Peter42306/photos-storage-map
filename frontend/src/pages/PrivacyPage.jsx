import { Link } from "react-router-dom";

export default function PrivacyPage() {
    return(        
        <section className="container py-5">

            <h1 className="display-5 mb-4">
                Privacy Policy
            </h1>

            <p className="text-muted">
                Last updated: July 2026
            </p>

            <hr className="mb-5"/>

            <h3>Information We Collect</h3>

            <p>
                PhotoMap may collect the following information:
            </p>

            <ul>
                <li>Your account information, including your name and email address.</li>
                <li>Photos and ZIP archives that you upload.</li>
                <li>Collection descriptions, notes and related metadata.</li>
                <li>Technical information required to provide and improve the service.</li>
            </ul>

            <h3>How We Use Your Information</h3>

            <p>
                The information you provide is used to:
            </p>

            <ul>
                <li>Provide the PhotoMap service.</li>
                <li>Store and manage your photo collections.</li>
                <li>Authenticate your account.</li>
                <li>Improve reliability, security and performance.</li>
                <li>Respond to support requests.</li>
            </ul>

            <h3>File Storage</h3>

            <p>
                Uploaded photos and ZIP archives are stored securely in cloud object storage.
                Your files remain private unless you intentionally create a shared link.
            </p>

            <h3>Shared Collections</h3>

            <p>
                Shared collections are available only through secure read-only links.
                Anyone with the link can access the shared collection until sharing is disabled.
            </p>

            <h3>Data Security</h3>

            <p>
                Reasonable technical and organisational measures are used to protect your
                account and uploaded content.
            </p>

            <h3>Contact</h3>

            <p>
                If you have any questions regarding this Privacy Policy, please use the
                Contact page.
            </p>

            <div className="mt-5">
                <Link
                    to="/"
                    className="btn btn-outline-secondary"
                >
                    Back to Home
                </Link>
            </div>

        </section>        
    );
}