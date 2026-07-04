import { useEffect, useState } from "react";

export default function BackToTopButton() {
    const [visible, setVisible] = useState(false);

    useEffect(() => {
        function handleScroll() {
            setVisible(window.scrollY > 300);
        }

        window.addEventListener("scroll", handleScroll);

        return () => window.removeEventListener("scroll", handleScroll);
    }, []);

    function scrollToTop() {
        window.scrollTo({
            top: 0,
            behavior: "smooth"
        });
    }

    if (!visible) {
        return null;
    }

    return (
        <button
            className="btn btn-primary rounded-circle shadow back-to-top"
            onClick={scrollToTop}
            aria-label="Back to top"            
        >
            <i className="bi bi-arrow-up"></i>
        </button>
    );
}