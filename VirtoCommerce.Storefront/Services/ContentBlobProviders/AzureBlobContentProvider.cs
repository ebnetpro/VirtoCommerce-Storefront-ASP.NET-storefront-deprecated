using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using CacheManager.Core;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Services;

namespace VirtoCommerce.Storefront.Services
{
    public class AzureBlobContentProvider : IContentBlobProvider, IStaticContentBlobProvider, IDisposable
    {
        private readonly CloudBlobClient _cloudBlobClient;
        private readonly CloudStorageAccount _cloudStorageAccount;
        private readonly CloudBlobContainer _container;
        private readonly CloudBlobDirectory _directory;
        private readonly string _containerName;
        private readonly string _baseDirectoryPath;
        private readonly ICacheManager<object> _cacheManager;
        private readonly CancellationTokenSource _cancelSource;

        public AzureBlobContentProvider(string connectionString, string basePath, ICacheManager<object> cacheManager)
        {
            _cacheManager = cacheManager;
            var parts = basePath.Split(new[] { "/", "\\" }, StringSplitOptions.RemoveEmptyEntries);

            _containerName = parts.FirstOrDefault();
            _baseDirectoryPath = string.Join("/", parts.Skip(1));

            if (!CloudStorageAccount.TryParse(connectionString, out _cloudStorageAccount))
            {
                throw new InvalidOperationException("Failed to get valid connection string");
            }
            _cloudBlobClient = _cloudStorageAccount.CreateCloudBlobClient();
            _container = _cloudBlobClient.GetContainerReference(_containerName);
            if (_baseDirectoryPath != null)
            {
                _directory = _container.GetDirectoryReference(_baseDirectoryPath);
            }

            _cancelSource = new CancellationTokenSource();

            var enabledTrackChanges = ConfigurationHelper.GetAppSettingsValue("VirtoCommerce:AzureBlobStorage:TrackChanges", true);
            if (enabledTrackChanges)
            {
                Task.Run(() => MonitorFileSystemChanges(_cancelSource.Token), _cancelSource.Token);
            }
        }

        private void MonitorFileSystemChanges(CancellationToken cancellationToken)
        {
            var intetval = ConfigurationHelper.GetAppSettingsValue("VirtoCommerce:AzureBlobStorage:TrackChangesInterval", 5000);

            var latestModifiedDate = DateTimeOffset.UtcNow;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var maxModifiedDate = DateTimeOffset.MinValue;

                    foreach (var file in EnumBlobFiles())
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;

                        if (file.Properties.LastModified.HasValue)
                        {
                            if (maxModifiedDate < file.Properties.LastModified)
                                maxModifiedDate = (DateTimeOffset)file.Properties.LastModified;

                            if (file.Properties.LastModified > latestModifiedDate)
                            {
                                RaiseChangedEvent(new FileSystemEventArgs(WatcherChangeTypes.Changed, Path.GetDirectoryName(file.Name), Path.GetFileName(file.Name)));
                            }
                        }
                    }

                    latestModifiedDate = maxModifiedDate;
                }
                catch (Exception ex)
                {
                    Trace.TraceError(ex.ToString());
                }

                Thread.Sleep(intetval);
            }
        }

        private IEnumerable<CloudBlob> EnumBlobFiles()
        {
            return _directory?.ListBlobs(useFlatBlobListing: true).OfType<CloudBlob>()
                ?? _container.ListBlobs(useFlatBlobListing: true).OfType<CloudBlob>();
        }

        #region IContentBlobProvider Members
        public event FileSystemEventHandler Changed;
        public event RenamedEventHandler Renamed;

        /// <summary>
        /// Open blob for read 
        /// </summary>
        /// <param name="path">blob relative path /folder/blob.md</param>
        /// <returns></returns>
        public virtual Stream OpenRead(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }
            path = NormalizePath(path);
            if (_directory != null)
            {
                return _directory.GetBlockBlobReference(path).OpenRead();
            }

            return _container.GetBlobReference(path).OpenRead();
        }

        /// <summary>
        /// Open blob for write by path
        /// </summary>
        /// <param name="path"></param>
        /// <returns>blob stream</returns>
        public virtual Stream OpenWrite(string path)
        {
            //Container name
            path = NormalizePath(path);
            var blob = _container.GetBlockBlobReference(path);
            blob.Properties.ContentType = MimeMapping.GetMimeMapping(Path.GetFileName(path));
            return blob.OpenWrite();
        }
        /// <summary>
        /// Check that blob or folder with passed path exist
        /// </summary>
        /// <param name="path">path /folder/blob.md</param>
        /// <returns></returns>
        public virtual bool PathExists(string path)
        {
            path = NormalizePath(path);

            var result = _cacheManager.Get("AzureBlobContentProvider.PathExists:" + path.GetHashCode(), "ContentRegion", () =>
            {
                // If requested path is a directory we should always return true because Azure blob storage does not support checking if directories exist
                var retVal = string.IsNullOrEmpty(Path.GetExtension(path));
                if (!retVal)
                {
                    var url = GetAbsoluteUrl(path);
                    try
                    {
                        retVal = _cloudBlobClient.GetBlobReferenceFromServer(new Uri(url)).Exists();
                    }
                    catch (Exception)
                    {
                        //Azure blob storage client does not provide method to check blob url exist without throwing exception
                    }
                }

                return (object)retVal;
            });

            return (bool)result;
        }

        /// <summary>
        /// Search blob content in specified folder
        /// </summary>
        /// <param name="path">folder path in which the search will be processed</param>
        /// <param name="searchPattern">search blob name pattern can be used mask (*, ? symbols)</param>
        /// <param name="recursive"> recursive search</param>
        /// <returns>Returns relative path for all found blobs  example: /folder/blob.md </returns>
        public virtual IEnumerable<string> Search(string path, string searchPattern, bool recursive)
        {
            var retVal = new List<string>();
            path = NormalizePath(path);
            //Search pattern may contains part of path /path/*.jpg then nedd add this part to base path
            searchPattern = searchPattern.Replace('\\', '/').TrimStart('/');
            var subDir = NormalizePath(Path.GetDirectoryName(searchPattern));
            if (!string.IsNullOrEmpty(subDir))
            {
                path = path.TrimEnd('/') + "/" + subDir;
                searchPattern = Path.GetFileName(searchPattern);
            }

            IEnumerable<IListBlobItem> blobItems;
            if (_directory != null)
            {
                var directoryBlob = _directory;
                if (!string.IsNullOrEmpty(path))
                {
                    directoryBlob = _directory.GetDirectoryReference(path);
                }
                blobItems = directoryBlob.ListBlobs(useFlatBlobListing: recursive);
            }
            else
            {
                blobItems = _container.ListBlobs(useFlatBlobListing: recursive);
            }
            // Loop over items within the container and output the length and URI.
            foreach (var item in blobItems)
            {
                var block = item as CloudBlockBlob;
                if (block != null)
                {
                    var blobRelativePath = GetRelativeUrl(block.Uri.ToString());
                    var fileName = Path.GetFileName(Uri.UnescapeDataString(block.Uri.ToString()));
                    if (fileName.FitsMask(searchPattern))
                    {
                        retVal.Add(blobRelativePath);
                    }
                }
            }
            return retVal;
        }

        #endregion

        protected virtual string NormalizePath(string path)
        {
            return path.Replace('\\', '/').TrimStart('/');
        }

        protected virtual string GetRelativeUrl(string url)
        {
            var absoluteUrl = GetAbsoluteUrl("");
            return url.Replace(absoluteUrl, string.Empty);
        }

        protected virtual string GetAbsoluteUrl(string path)
        {
            path = NormalizePath(path);
            return string.Join("/", _cloudBlobClient.BaseUri.ToString().TrimEnd('/'), _containerName, _baseDirectoryPath, path);
        }


        protected virtual void RaiseChangedEvent(FileSystemEventArgs args)
        {
            var changedEvent = Changed;
            changedEvent?.Invoke(this, args);
        }

        protected virtual void RaiseRenamedEvent(RenamedEventArgs args)
        {
            var renamedEvent = Renamed;
            renamedEvent?.Invoke(this, args);
        }

        #region IDisposable Support
        private bool _disposedValue; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    _cancelSource.Cancel();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                _disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~AzureBlobContentProvider() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
