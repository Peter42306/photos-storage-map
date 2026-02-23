import './App.css'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import LoginPage from './pages/LoginPage';
import DashboardPage from './pages/DashboardPage';
import RegisterPage from './pages/RegisterPage';
import ConfirmEmailPage from './pages/ConfirmEmailPage';
import CheckEmailPage from './pages/CheckEmailPage';
import ProtectedRoute from './routes/ProtectedRoute';
import LandingPage from './pages/LandingPage';
import Navbar from './components/Navbar';
import ForgotPasswordPage from './pages/ForgotPasswordPage';
import ResendConfirmationPage from './pages/ResendConfirmation';
import ResetPasswordPage from './pages/ResetPassword';
import UploadTestpage from './pages/UploadTestPage';
import CollectionsPage from './pages/CollectionsPage';
import CollectionPage from './pages/CollectionPage';

function App() {  

  return (
    <BrowserRouter>
      <Navbar/>

      <Routes>        
        {/* public */}        
        <Route path='/' element={<LandingPage/>}/>
        <Route path='/login' element={<LoginPage/>}/>
        <Route path='/register' element={<RegisterPage/>}/>
        <Route path='/check-email' element={<CheckEmailPage/>}/>
        <Route path='/confirm-email' element={<ConfirmEmailPage/>}/>
        <Route path='/forgot-password' element={<ForgotPasswordPage/>}/>
        <Route path='/resend-confirmation' element={<ResendConfirmationPage/>}/>        
        <Route path='/reset-password' element={<ResetPasswordPage/>}/>        

          {/* protected */}
          <Route element={<ProtectedRoute/>}>
            {/* <Route path='/' element={<Navigate to="/dashboard" replace/>}/> */}
            
            {/* <Route path='/app' element={<DashboardPage/>}/> */}
            <Route path='/upload-test' element={<UploadTestpage/>}/> 
            <Route path='/dashboard' element={<DashboardPage/>}/>

            <Route path='/collections' element={<CollectionsPage/>}/>
            <Route path='/collections/:id' element={<CollectionPage/>}/>



          </Route>

          {/* fallback */}
          <Route path='*' element={<Navigate to="/" replace/>}/>
      </Routes>
    </BrowserRouter>    
  )
}

export default App