import { Accordion } from "react-bootstrap";

export default function FaqAccordion() {
    return(
        <Accordion defaultActiveKey="0" flush>
            {faqItems.map((item, index) => (
                <Accordion.Item
                    eventKey={index.toString()}
                    key={item.question}
                >
                    <Accordion.Header>{item.question}</Accordion.Header>
                    <Accordion.Body>{item.answer}</Accordion.Body>
                </Accordion.Item>
            ))}
        </Accordion>
    );    
}

const faqItems = [
    {
        question: "What is PhotoMap?",
        answer: "PhotoMap is a cloud-based application for storing, organising, exploring and sharing photo collections with maps, notes and archives."
    },
    {
        question: "Is PhotoMap free?",
        answer: "Yes. A Free plan is available with up to 5 GB of storage and up to 500 photos per collection. A Pro plan provides 50 GB of storage space and up to 1500 photos per collection."
    },    
    {
        question: "What photo and archive formats are supported?",
        answer: "PhotoMap currently supports JPG/JPEG photos and ZIP archives. ZIP archives can be uploaded to keep related files together with a collection."
    },
    {
        question: "Do my photos need GPS information?",
        answer: "No. GPS information is optional. Photos without location data can still be uploaded, organised and shared. Photos without GPS information will not be shown on the interactive map."
    },
    // {
    //     question: "Can I upload ZIP archives?",
    //     answer: "Yes. ZIP archives can be stored together with your photo collections to keep related project files in one place."
    // },
    {
        question: "Can I share my collections?",
        answer: "Yes. Collections can be shared using secure read-only links. Shared collections always display the latest version of the collection. You can disable sharing at any time."
    },
    // {
    //     question: "Can I delete original photos?",
    //     answer: "Yes. Original photos can be removed after processing to reduce storage usage while keeping resized photos, maps, notes and sharing available."
    // },    
    {
        question: "Are my photos private and stored securely?",
        answer: "Yes. Your photos are private by default. Only collections that you explicitly share using a public read-only link become accessible to others. All uploaded content is securely stored in S3-compatible object storage."
    },
    {
        question: "Why don't some photos keep their original file names or GPS information?",
        answer: "When uploading from a mobile device, some mobile browsers may not provide access to original file names or GPS metadata. This depends on the browser and operating system, not PhotoMap."
    },
    // {
    //     question: "Can I access PhotoMap on mobile devices?",
    //     answer: "Yes. PhotoMap works in modern desktop and mobile web browsers."
    // },
    {
        question: "What happens if I reach my storage limit?",
        answer: "You can free up storage by using the Storage Optimisation feature to remove original photos, deleting unused collections or archives, or upgrading to the Pro plan for additional storage. Original photos usually occupy most of the storage space."
    },
    {
        question: "What is included in the Pro plan?",
        answer: "The Pro plan includes all Free plan features, plus 50 GB of storage and supports up to 1500 photos per collection."
    },
    {
        question: "How do I activate the Pro plan?",
        answer: "Currently, Pro plans can be activated manually upon request while online subscriptions are being developed. Use the contact form to request Pro activation."
    },
    // {
    //     question: "How can I contact you?",
    //     answer: "Use the contact form or email provided in the Contact section."
    // }
];