import './App.css'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import RegisterPage from './pages/RegisterPage';
import ConfirmEmailPage from './pages/ConfirmEmailPage';
import CheckEmailPage from './pages/CheckEmailPage';

function App() {  

  return (
    <BrowserRouter>
      <Routes>        
        <Route path='/' element={<Navigate to="/dashboard" replace/>}/>
        <Route path='/login' element={<LoginPage/>}/>
        <Route path='/register' element={<RegisterPage/>}/>
        <Route path='/check-email' element={<CheckEmailPage/>}/>
        <Route path='/confirm-email' element={<ConfirmEmailPage/>}/>
        <Route path='/dashboard' element={<DashboardPage/>}/>
      </Routes>
    </BrowserRouter>    
  )
}

export default App
