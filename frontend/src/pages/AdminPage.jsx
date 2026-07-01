import { useEffect, useState } from "react";
import { getAdminUsers, updateUserActive } from "../api";

export default function AdminPage() {

    const [users, setUsers] = useState([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    async function loadUsers() {
        try {
            setError("");
            setLoading(true);

            const data = await getAdminUsers();
            setUsers(data);
        } catch (err) {
            setError(err.message);
        } finally {
            setLoading(false);
        }
    }

    async function handleUserActiveChange(userId, isActive) {
        const confirmed = confirm(
                    
            isActive
                ? "Activate this user?"
                : "Deactivate this user?"
        );

        if (!confirmed) {
            return;
        }

        try {        
            setError("");
            await updateUserActive(userId, isActive);
            await loadUsers();
        } catch (err) {
            setError(err.message);
        }
    }

    useEffect(() => {
        loadUsers();
    }, [])

    if (loading) {
        return(
            <div className="container py-4">
                <div className="alert alert-info">
                    Loading admin users
                </div>
            </div>
        );
    }


    return(
        <>
        <div className="container py-4">
            <h2>Admin Panel</h2>
            <p>Users, storage usage, collections, photos and archives.</p>

            {error && (
                <div className="alert alert-danger">
                    {error}
                </div>
            )}

            <div className="table-responsive">
                <table className="table table-sm align-middle">
                    <thead>
                        <tr>
                            <th className="text-nowrap">Email</th>
                            <th className="text-nowrap">Name</th>
                            <th className="text-nowrap">Plan</th>
                            <th className="text-nowrap">Active</th>
                            <th className="text-nowrap">Collections</th>
                            <th className="text-nowrap">Photos</th>
                            <th className="text-nowrap">Archives</th>
                            <th className="text-nowrap">Storage</th>
                            <th className="text-nowrap">Last Login</th>
                        </tr>
                    </thead>
                    <tbody>
                        {users.map((u) => (
                            <tr key={u.userId}>
                                <td className="text-nowrap">{u.email}</td>
                                <td className="text-nowrap">{u.fullName || "-"}</td>
                                <td className="text-nowrap">{u.storagePlan}</td>
                                {/* <td className="text-nowrap">{u.isActive ? "Yes" : "No"}</td> */}
                                <td>
                                    <div className="form-check form-switch">                                        
                                        <input
                                            className="form-check-input"
                                            type="checkbox"
                                            role="switch"                                            
                                            checked={u.isActive}
                                            onChange={() => handleUserActiveChange(u.userId, !u.isActive)}
                                        />                                        
                                    </div>                                    
                                </td>
                                <td className="text-nowrap">{u.collectionsCount}</td>
                                <td className="text-nowrap">{u.photosCount}</td>
                                <td className="text-nowrap">{u.archivesCount}</td>
                                <td className="text-nowrap">{formatBytes(u.totalStorageBytes)}</td>
                                <td className="text-nowrap">{formatDate(u.lastLoginAt)}</td>                                
                            </tr>
                        ))}
                    </tbody>
                </table>
            </div>
        </div>
        </>
    );
}

function formatBytes(bytes) {
    if (!bytes) {
        return "0 B";
    }

    const sizes = ["B", "KB", "MB", "GB", "TB"];
    const i = Math.floor(Math.log(bytes) / Math.log(1024));

    return `${(bytes / Math.pow(1024, i)).toFixed(2)} ${sizes[i]}`;
}

function formatDate(dateString) {
    if (!dateString) return "-";

    const date = new Date(dateString);
    if (isNaN(date)) return "-";

    return new Intl.DateTimeFormat("en-GB", {
        day: "2-digit",
        month: "short",
        year: "numeric",
    }).format(date);
}