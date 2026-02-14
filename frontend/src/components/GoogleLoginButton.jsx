import { useEffect, useRef, useState } from "react";
import { getToken, googleLogin, setToken } from "../api";

export default function GoogleLoginButton({ onLoggedIn }){
    const btnRef = useRef(null);
    const [error, setError] = useState("");    

    useEffect(() => {
        const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

        if (!clientId) {
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setError("VITE_GOOGLE_CLIENT_ID is missing");
            return;
        }

        if (!window.google?.accounts?.id) {
            setError("Google Identity script not loaded.")
            return;
        }
        
        


        window.google.accounts.id.initialize({
            client_id: clientId,
            callback: async (response) => {
                try{
                    setError("");

                    if (!response?.credential) {
                        setError("No credential received from Google");
                        return;
                    }

                    const idToken = response.credential;

                    const data = await googleLogin(idToken);
                    
                    setToken(data.accessToken);
                    
                    onLoggedIn?.(data);
                } catch (ex) {
                    setError(ex?.message ?? "Google login error");
                }
            },
        });        

        window.google.accounts.id.renderButton(btnRef.current,{
            theme: "outline",
            size: "large",
            width: 250,
        }, 0);
        

        return()=>{
            window.google?.accounts?.id.cancel();
        }
    }, []);

    return(
        <div className="mt-3 d-flex justify-content-center">
            <div ref={btnRef}></div>
            {error && <div className="text-danger mt-2">{error}</div>}            
        </div>
    );
}