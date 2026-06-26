export default function ZipProgressBar({
    status,
    percent,
    processedFiles,
    totalFiles
}) {
    if (!status ) return null;

    return(
        <div className="alert alert-info mt-3">
            <div className="small mb-2">
                {status}
                {totalFiles > 0 && (
                    <>
                        {" "}
                        ({processedFiles} / {totalFiles} files)
                    </>
                )}
            </div>
            <div className="progress">
                <div
                    className="progress-bar"
                    role="progressbar"
                    style={{ width: `${percent}%`}}
                    aria-valuenow={percent}
                    aria-valuemin="0"
                    aria-valuemax="100"
                >
                    {percent}%
                </div>
            </div>
        </div>
    );
}