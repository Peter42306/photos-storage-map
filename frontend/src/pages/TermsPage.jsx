import { Link } from "react-router-dom";

export default function TermsPage() {
    return(
        <section className="container py-5">

            <h1 className="display-5 mb-4">
                Terms of Service
            </h1>

            <p className="text-muted">
                Last updated: July 2026
            </p>

            <hr className="mb-5"/>

            <h3>Acceptance of Terms</h3>

            <p>
                By creating an account or using PhotoMap, you agree to these Terms of Service.
            </p>

            <h3>User Accounts</h3>

            <p>
                You are responsible for maintaining the confidentiality of your account
                credentials and for all activities performed using your account.
            </p>

            <h3>User Content</h3>

            <p>
                You retain ownership of all photos, notes and archives that you upload.
                You are responsible for ensuring that you have the necessary rights to
                upload and share your content.
            </p>

            <h3>Acceptable Use</h3>

            <p>
                You agree not to:
            </p>

            <ul>
                <li>Upload illegal or harmful content.</li>
                <li>Attempt unauthorised access to other accounts.</li>
                <li>Interfere with the operation or security of the service.</li>
                <li>Distribute malicious software through the service.</li>
            </ul>

            <h3>Storage Limits</h3>

            <p>
                Free and Pro plans are subject to their respective storage and collection
                limits.
            </p>

            <h3>Service Availability</h3>

            <p>
                PhotoMap is provided on an "as available" basis. While every effort is
                made to provide reliable service, uninterrupted availability cannot be
                guaranteed.
            </p>

            <h3>Changes to the Service</h3>

            <p>
                Features, pricing and these Terms of Service may be updated from time to
                time without prior notice.
            </p>

            <h3>Contact</h3>

            <p>
                If you have any questions regarding these Terms of Service, please use
                the Contact page.
            </p>

            <hr className="mt-5"/>
            <div className="text-center mt-5">
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