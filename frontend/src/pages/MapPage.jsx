import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import * as maptilersdk from "@maptiler/sdk";
import "@maptiler/sdk/dist/maptiler-sdk.css";
import { getCollectionMap } from "../api";

function formatDistance(meters) {
    if (meters == null) {
        return "";
    }

    if (meters < 1000) {
        return `${Math.round(meters)} m`;
    }

    return `${(meters / 1000).toFixed(2)} km`;
}


export default function MapPage() {
    const mapContainer = useRef(null);
    const map = useRef(null);
    const markersRef = useRef([]);

    const { id } = useParams();
    const navigate = useNavigate();

    const [photos, setPhotos] = useState([]);
    const [totalDistance, setTotalDistance] = useState(0);    
    const [error, setError] = useState("");

    maptilersdk.config.apiKey = import.meta.env.VITE_MAPTILER_KEY;

    useEffect(() => {
        async function loadMapPhotos() {
            try {
                setError("");
                const data = await getCollectionMap(id);                
                setPhotos(data.result ?? []);                
                setTotalDistance(data?.totalDistance ?? 0);

            } catch (err) {
                setError(err.message);
            }
        }

        if (id) {
            loadMapPhotos();
        }
    }, [id]);
    

    useEffect(() => {
        if (map.current) {
            return;
        }

        map.current = new maptilersdk.Map({
            container: mapContainer.current,
            style: `https://api.maptiler.com/maps/streets/style.json?key=${import.meta.env.VITE_MAPTILER_KEY}`,
            center: [0, 0],
            zoom: 2
        });
        
        map.current.addControl(new maptilersdk.FullscreenControl(), "top-right");

        return () => {
            markersRef.current.forEach(marker => marker.remove());
            markersRef.current = [];

            map.current?.remove();
            map.current = null;
        };
    }, []);

    useEffect(() => {
        if (!map.current) {
            return;
        }

        markersRef.current.forEach(marker => marker.remove());
        markersRef.current = [];

        if (!photos || photos.length === 0) {
            return;
        }

        const bounds = new maptilersdk.LngLatBounds();
        let hasPoints = false;

        photos.forEach((photo) => {
            const latitude = photo.latitude ?? photo.Latitude;
            const longitude = photo.longitude ?? photo.Longitude;

            if (latitude == null || longitude == null) {
                return;
            }
            
            hasPoints = true;

            const lngLat = [longitude, latitude];
            bounds.extend(lngLat);

            // new maptilersdk.Marker()
            //     .setLngLat(lngLat)
            //     .addTo(map.current);

            let marker = new maptilersdk.Marker().setLngLat(lngLat);            

            if(photo.thumbUrl) {
                const container = document.createElement("div");
                container.style.textAlign = "center";
                container.style.minWidth = "340px";

                const img = document.createElement("img");
                img.src = photo.thumbUrl;
                img.alt = photo.originalFileName ?? "photo";
                img.style.width = "100%";
                img.style.maxHeight = "240px";
                img.style.objectFit = "contain";
                img.style.borderRadius = "8px";
                img.style.marginBottom = "6px";

                const fileName = document.createElement("div");
                fileName.textContent = photo.originalFileName ?? "";
                fileName.style.fontSize = "13px";
                fileName.style.wordBreak = "break-word";

                container.appendChild(img);
                container.appendChild(fileName);

                marker = marker.setPopup(
                    new maptilersdk.Popup({ offset: 25, maxWidth: "360px" }).setDOMContent(container)
                );
            }

            marker.addTo(map.current);
            markersRef.current.push(marker);
        });

        if (hasPoints) {
            map.current.fitBounds(bounds, { padding: 50 })            
        }
        
    }, [photos]);

    return(
        <div className="container py-4">
            <div className="d-flex align-items-center justify-content-between">
                <h2 className="mb-0">Collection Map</h2>            
                <button
                    className="btn btn-primary mb-3"
                    onClick={() => navigate(`/Collections/${id}`)}
                >
                    Back to collection
                </button>
            </div>
            <hr/>
            

            



            <div className="d-flex align-items-center justify-content-between small">
                <p>Photos: {photos.length}</p>
                <p>Distance: {formatDistance(totalDistance)}</p>
            </div>
            
            


            {error ? <div className="alert alert-danger">{error}</div> : null}
            
            <div 
                ref={mapContainer} 
                style={{ width: "100%", height: "600px", borderRadius: "8px" }}
            ></div>            
            
        </div>
        
    );
}