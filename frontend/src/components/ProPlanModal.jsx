import { Button, Modal } from "react-bootstrap";

export default function ProPlanModal({ show, onHide}) {
    return(        
        <Modal
            show={show}
            onHide={onHide}
            centered
        >
            <Modal.Header closeButton>
                <Modal.Title>
                     <i className="bi bi-stars text-warning me-2"></i>
                    Activate Pro Plan
                </Modal.Title>                
            </Modal.Header>
            <Modal.Body>
                <p>Online subscription payments are currently under development.</p>
                <p>To activate your Pro plan:</p>
                <ul>
                    <li>Create a free account.</li>
                    <li>Contact us using the contact form.</li>
                    <li>Your account will be upgraded to the Pro plan.</li>
                    <li>Automatic online subscriptions will be available soon.</li>
                </ul>
                <p>Online subscriptions will be available soon. Until then, Pro plan is activated manually through our support team.</p> 
            </Modal.Body>
            <Modal.Footer>
                <Button
                    variant="outline-secondary"
                    onClick={onHide}
                >
                    Close
                </Button>
                <Button
                    variant="outline-secondary"
                    // href="#contact"                    
                    onClick={onHide}
                >
                    Contact us
                </Button>
            </Modal.Footer>
        </Modal>
    );
}