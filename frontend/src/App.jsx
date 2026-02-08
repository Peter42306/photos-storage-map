// import { useEffect, useState } from 'react'
// import reactLogo from './assets/react.svg'
// import viteLogo from '/vite.svg'
import { useEffect, useState } from 'react'
import './App.css'
import { clearToken, getToken, setToken, login, me } from './api';

function App() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const [token, setTokenState] = useState(getToken() || "");
  const [meData, setMeData] = useState(null);

  const [status, setStatus] = useState("");
  const [error, setError] = useState("");

  async function handleLogin(e) {
    e.preventDefault();
    setError("");
    setStatus("Logging in ...");

    try {
      const res = await login(email, password);
      setToken(res.accessToken);
      setTokenState(res.accessToken);
      setStatus(`OK. Expires: ${res.expiresAtUtc}`);
    } catch (ex) {
      setStatus("");
      setError(ex.message);
    }
  }

  async function loadMe() {
    setError("");
    setStatus("Loading /api/me...");

    try {
      const data = await me();
      setMeData(data);
      setStatus("OK");
    } catch (ex) {
      setMeData(null);
      setStatus("");
      setError(ex.message);
    }
  }

  function logout() {
    clearToken();
    setTokenState("");
    setMeData(null);
    setStatus("Logged out");
    setError("");
  }

  useEffect(() =>{
    if (token) {
      loadMe();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [token]);




  // const [count, setCount] = useState(0)
  // const [data, setData] = useState([]);
  // const [error, setError] = useState(null);

  // useEffect(() =>{
  //   fetch("http://localhost:5008/WeatherForecast")
  //     .then((res) =>{
  //       if(!res.ok){
  //         throw new Error("Request failed");          
  //       }

  //       return res.json();
  //     })
  //     .then(setData)
  //     .catch((err) => setError(err.message));
  // }, []);

  return (
    <div className='container py-4'>
      
      <div className='d-flex align-items-center justify-content-between mb-3'>        
        <h2>PhotosStorageMap - Test UI</h2>

        {token ?(
          <button className='btn btn-outline-danger' onClick={logout}>
            Logout
          </button>
        ) : null}
      </div>

      {status ? (
        <div className='alert alert-success py-2'>{status}</div>
      ) : null}

      {error ?(
        <div className='alert alert-danger py-2'>{error}</div>
      ) : null}

      {!token ?(
        <div className='card shadow-sm'>
          <div className='card-body'>
            <h2 className='card-title'>Login</h2>

            <form onSubmit={handleLogin}>
              <div className='mb-3'>
                <label className='form-label'>Email</label>
                <input
                  className='form-control'
                  type='email'
                  value={email}
                  onChange={(e) => setEmail(e.target.value)}
                  autoComplete='email'
                  required
                />
              </div>

              <div className='mb-3'>
                <label className='form-label'>Password</label>
                <input
                  className='form-control'
                  type='password'
                  value={password}
                  onChange={(e) => setPassword(e.target.value)}
                  autoComplete='current-password'
                  required
                />
              </div>

              <button className='btn btn-primary' type='submit'>
                Login
              </button>

              <div className='form-text mt-2'>
                Token is saved in localStorage
              </div>
            </form> 

          </div>
        </div>
      ) : (
        <div className='card shadow-sm'>
          <div className='card-body'>
            <div className='d-flex gap-2 flex-wrap'>
              
              <button 
                className='btn btn-outline-primary' onClick={loadMe}
              >
                Load /api/me
              </button>

              <button
                className='btn btn-outline-secondary'
                onClick={() => navigator.clipboard.writeText(token)}
                title='Copy token to clipboard'
              >
                Copy token
              </button>

            </div>
            <hr/>

            <pre className='bg-light border rounded p-2 mt-2 small mb-0'>
                  {token}
            </pre>
            <div className='mt-3'>{token}</div>
          </div>
        </div>
      )}

      <div className='card shadow-sm mt-3'>
        <div className='card-body'>
          <h5 className='card-title'>/api/me response</h5>
          <pre className='bg-light border rounded p-2 mt-2 small mb-0'>
            {meData ? JSON.stringify(meData, null, 2) : "-"}
          </pre>
        </div>
      </div>



      





      {/* <h1>Weather from backend</h1>

      {error && <p>{error}</p>}

      <ul>
        {data.map((w, i) => (
          <li key={i}>
            {w.date} {w.temperatureC} - {w.summary} 
          </li>
        ))}
      </ul> */}
    </div>

    // <>
    //   <div>
    //     <a href="https://vite.dev" target="_blank">
    //       <img src={viteLogo} className="logo" alt="Vite logo" />
    //     </a>
    //     <a href="https://react.dev" target="_blank">
    //       <img src={reactLogo} className="logo react" alt="React logo" />
    //     </a>
    //   </div>
    //   <h1>Vite + React</h1>
    //   <div className="card">
    //     <button onClick={() => setCount((count) => count + 1)}>
    //       count is {count}
    //     </button>
    //     <p>
    //       Edit <code>src/App.jsx</code> and save to test HMR
    //     </p>
    //   </div>
    //   <p className="read-the-docs">
    //     Click on the Vite and React logos to learn more
    //   </p>
    // </>
  )
}

export default App
