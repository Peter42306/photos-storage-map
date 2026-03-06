import { useEffect, useRef, useState } from "react";
import { useNavigate, useParams } from "react-router-dom";
import * as maptilersdk from "@maptiler/sdk";
import "@maptiler/sdk/dist/maptiler-sdk.css";
import { getCollectionMap } from "../api";


export default function MapPage() {
    const mapContainer = useRef(null);
    const map = useRef(null);
    const markersRef = useRef([]);

    const { id } = useParams();
    const navigate = useNavigate();

    const [photos, setPhotos] = useState([]);
    const [error, setError] = useState("");

    maptilersdk.config.apiKey = import.meta.env.VITE_MAPTILER_KEY;

    useEffect(() => {
        async function loadMapPhotos() {
            try {
                setError("");
                const data = await getCollectionMap(id);
                console.log("photos for map", data);
                setPhotos(data ?? []);                

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

            if (photo.thumbUrl) {
                const popupHtml = `
                    <div style="text-align: center; min-width:340px;">
                        <img
                            src="${photo.thumbUrl}"
                            alt="${photo.originalFileName ?? "photo"}"
                            style="width:100%; max-height:240px; object-fit:contain; border-radius: 8px; margin-bottom:6px;"
                        />
                        <div style="font-size:13px; word-break:break-word;">
                            ${photo.originalFileName ?? ""}
                        </div>
                    </div>
                `;

                marker = marker.setPopup(
                    new maptilersdk.Popup({ offset: 25, maxWidth: "360px" }).setHTML(popupHtml)
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
            <h2>Collection Map</h2>            

            <button
                className="btn btn-primary mb-3"
                onClick={() => navigate(`/Collections/${id}`)}
            >
                Back to collection
            </button>



            <p>Photos: {photos.length}</p>
            {error ? <div className="alert alert-danger">{error}</div> : null}
            
            <div 
                ref={mapContainer} 
                style={{ width: "100%", height: "600px", borderRadius: "8px" }}
            ></div>            
            
        </div>
        
    );
}