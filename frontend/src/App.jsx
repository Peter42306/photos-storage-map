import { useEffect, useState } from 'react'
// import reactLogo from './assets/react.svg'
// import viteLogo from '/vite.svg'
import './App.css'

function App() {
  // const [count, setCount] = useState(0)
  const [data, setData] = useState([]);
  const [error, setError] = useState(null);

  useEffect(() =>{
    fetch("http://localhost:5008/WeatherForecast")
      .then((res) =>{
        if(!res.ok){
          throw new Error("Request failed");          
        }

        return res.json();
      })
      .then(setData)
      .catch((err) => setError(err.message));
  }, []);

  return (
    <div>
      <h1>Weather from backend</h1>

      {error && <p>{error}</p>}

      <ul>
        {data.map((w, i) => (
          <li key={i}>
            {w.date} {w.temperatureC} - {w.summary} 
          </li>
        ))}
      </ul>
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
