import { useEffect, useRef, useState } from "react";
import { googleLogin, setToken } from "../api";

export default function GoogleLoginButton({ onLoggedIn }){
    const btnRef = useRef(null);
    const renderedRef = useRef(false);

    const [error, setError] = useState("");    
    const [gsiReady, setGsiReady] = useState(false);
    const [retryUsed, setRetryUsed] = useState(false);

    useEffect(() => {
        const clientId = import.meta.env.VITE_GOOGLE_CLIENT_ID;

        if (!clientId) {
            // eslint-disable-next-line react-hooks/set-state-in-effect
            setError("VITE_GOOGLE_CLIENT_ID is missing");
            return;
        }

        // for the 1st wrong loading of button
        // if (!window.google?.accounts?.id) {
        //     const key = "google_reload_attempted";
        //     if (!sessionStorage.getItem(key)) {
        //         sessionStorage.setItem(key, "1");
        //         window.location.reload();
        //         return;
        //     }
        //     setError("Google Identity script not loaded.")
        //     return;
        // }

        // for the 1st wrong loading of button, manually
        // if (!window.google?.accounts?.id) {                        
        //     setError("Google Identity script not loaded.");
        //     return;
        // }

        // check if GSI available
        if (!window.google?.accounts?.id) {
            setGsiReady(false);

            // 1 time reload without error notice
            const key = "google_reload_attempted";
            if (!sessionStorage.getItem(key)) {
                sessionStorage.setItem(key, "1");
                window.location.reload();
                return;
            }

            // if reloaded once
            setRetryUsed(true);
            return;

        }

        setGsiReady(true);
        setRetryUsed(false);

        // container must exist
        if (!btnRef.current) {
            return;
        }

        // do not re-render button
        if (renderedRef.current) {
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

        btnRef.current.innerHTML = "";

        // if (!window.google?.accounts?.id) {
        //     return;
        // }
        window.google.accounts.id.renderButton(btnRef.current,{
            theme: "outline",
            size: "large",
            width: 250,
        });                

        renderedRef.current = true;

        return () => {
            renderedRef.current = false;
        }

    }, [onLoggedIn]);

    // fallback if GSI is not ready    
    // if (!gsiReady) {
    //     return(
    //         <div className="mt-3 d-flex flex-column align-items-center">
    //             <button
    //                 type="button"
    //                 className="btn btn-outline-secondary"
    //                 style={{ width: 250 }}
    //                 onClick={() => window.location.reload()}
    //             >
    //                 Retry sign in with Google
    //             </button>

    //             {retryUsed && (
    //                 <div className="mt-2 text-muted">
    //                     Google sign-in is not available right now. Please try again.
    //                 </div>
    //             )}
    //         </div>
    //     );
    // }

    return(
        <div className="mt-3 d-flex flex-column align-items-center">
            <div ref={btnRef}></div>

            {!gsiReady && (
                <div className="d-flex flex-column align-items-center">
                    <button
                        type="button"
                        className="btn btn-outline-secondary"
                        style={{ width: 250 }}
                        onClick={() => window.location.reload()}
                    >
                        Try sign in with Google
                    </button>

                    {/* {retryUsed && (
                        <div className="mt-2 text-muted">
                            Google sign-in is not available right now. Please try again.
                        </div>
                    )} */}
                </div>
                
            )}

             {error && <div className="text-danger mt-2">{error}</div>}                      
        </div>
    );
}