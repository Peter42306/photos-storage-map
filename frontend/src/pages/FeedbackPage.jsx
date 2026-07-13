import { useState } from "react";
import { Link } from "react-router-dom";
import { submitFeedback } from "../contactFormApi";

const initialForm = {
    senderEmail: "",
    type: "1",
    subject: "",
    body: "",
};

export default function FeedbackPage() {
    const [form, setForm] = useState(initialForm);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [successMessage, setSuccessMessage] = useState("");
    const [errorMessage, setErrorMessage] = useState("");

    function handleChange(event) {
        const { name, value } = event.target;

        setForm((current) => ({
            ...current,
            [name]: value,
        }));
    }

    async function handleSubmit(event) {
        event.preventDefault();
        
        setSuccessMessage("");
        setErrorMessage("");

        const senderEmail = form.senderEmail.trim();
        const subject = form.subject.trim();
        const body = form.body.trim();

        if (!body) {
            setErrorMessage("Please enter your feedback.");
            return;
        }

        setIsSubmitting(true);
        
        try {
            const response = await submitFeedback({
                userId: null,
                senderEmail: senderEmail || null,
                type: Number(form.type),
                subject: subject || null,
                body,
            });

            setForm(initialForm);

            setSuccessMessage(
                response?.message || "Thank you for your feedback."
            );
        } catch (error) {
            setErrorMessage(error.message);
        } finally {
            setIsSubmitting(false);
        }
    }

    return(        
        <div className="container py-4">
            <div className="row justify-content-center">
                <div className="col-12 col-md-9 col-lg-7">
                    <div className="text-center mb-4">                
                        <h2 className="display-6 mb-4">FEEDBACK</h2>
                        <p>Found a bug or have an idea for improving this application? Let us know.</p>
                    </div>            

                    <div className="card shadow-sm">
                        <div className="card-body">
                            <form onSubmit={handleSubmit}>
                                <div className="mb-3">
                                    <label
                                        htmlFor="feedback-type"
                                        className="form-label"
                                    >
                                        Feedback type
                                    </label>
                                    
                                    <select                                
                                        id="feedback-type"
                                        name="type"
                                        value={form.type}
                                        onChange={handleChange}
                                        className="form-select"
                                        disabled={isSubmitting}
                                        required
                                    >
                                        <option value="1">General</option>
                                        <option value="2">Report a bug</option>
                                        <option value="3">Request a feature</option>
                                        <option value="4">Complaint</option>
                                        <option value="5">Praise</option>
                                    </select>
                                </div>                                
                                <div className="mb-3">
                                    <label
                                        htmlFor="feedback-subject"
                                        className="form-label"
                                    >
                                        Subject (optional)
                                    </label>
                                    <input
                                        id="feedback-subject"
                                        name="subject"
                                        type="text"
                                        className="form-control"
                                        value={form.subject}
                                        onChange={handleChange}
                                        maxLength={200}
                                        disabled={isSubmitting}                                        
                                    />
                                </div>
                                <div className="mb-3">
                                    <label
                                        htmlFor="feedback-email"
                                        className="form-label"
                                    >
                                        Email (optional)
                                    </label>
                                    <input
                                        id="feedback-email"
                                        name="senderEmail"
                                        type="email"
                                        className="form-control"
                                        value={form.senderEmail}
                                        onChange={handleChange}
                                        maxLength={254}
                                        autoComplete="email"
                                        disabled={isSubmitting}                                        
                                    />
                                    <div className="form-text">
                                        Add your email if you would like a reply.
                                    </div>
                                </div>
                                <div className="mb-3">
                                    <label
                                        htmlFor="feedback-body"
                                        className="form-label"
                                    >
                                        Message
                                    </label>
                                    <textarea
                                        id="feedback-body"
                                        name="body"                                        
                                        className="form-control"
                                        rows={7}
                                        value={form.body}
                                        onChange={handleChange}
                                        maxLength={4000}                                        
                                        disabled={isSubmitting}                                        
                                        required
                                    />
                                    <div className="form-text text-end">
                                        {form.body.length} / 4000
                                    </div>
                                </div>

                                {successMessage && (
                                    <div className="alert alert-success" role="status">
                                        {successMessage}
                                    </div>
                                )}
                                {errorMessage && (
                                    <div className="alert alert-danger" role="alert">
                                        {errorMessage}
                                    </div>
                                )}

                                <div className="text-center">
                                    <button
                                        type="submit"
                                        className="btn btn-primary"
                                        disabled={isSubmitting}
                                    >
                                        Send feedback
                                    </button>
                                </div>
                                
                            </form>
                        </div>
                    </div>
                    
                    <hr className="mt-5"/>
                    <div className="text-center mt-5">
                        <Link
                            to="/"
                            className="btn btn-outline-secondary"
                        >
                            Back to Home
                        </Link>
                    </div>
                </div>
            </div>
        </div>        
    );
}