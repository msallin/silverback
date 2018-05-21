﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback
{
    /// <summary>
    /// Continuously scans a folder and fires an event each time a new file is created.
    /// </summary>
    /// <seealso cref="Silverback.FolderWatcher" />
    public class PollingFolderWatcher : FolderWatcher
    {
        private readonly DirectoryInfo _directoryInfo;
        private DateTime _timestamp;
        private List<string> _filesCache;
        private readonly CancellationTokenSource _cancellationToken;

        /// <summary>
        /// Initializes a new instance of the <see cref="PollingFolderWatcher"/> class.
        /// </summary>
        /// <param name="folderPath">The folder path.</param>
        public PollingFolderWatcher(string folderPath)
        {
            _directoryInfo = new DirectoryInfo(folderPath);
            _timestamp = DateTime.Now;

            _cancellationToken = new CancellationTokenSource();

            Task.Run(() =>
            {
                while (!_cancellationToken.IsCancellationRequested)
                {
                    CheckFolder();

                    if (_cancellationToken.IsCancellationRequested)
                        break;

                    //await Task.Delay(100);
                }
            }, _cancellationToken.Token);
        }

        /// <summary>
        /// Checks if new files have been created in the folder and fires the event.
        /// </summary>
        private void CheckFolder()
        {
            var files = _directoryInfo.GetFiles("*.txt").Where(f => f.LastWriteTime >= _timestamp).OrderBy(f => f.LastWriteTime).ToArray();

            if (!files.Any())
                return;

            foreach (var file in files)
            {
                if (_filesCache != null && _filesCache.Contains(file.FullName))
                    continue;

                FireFileCreatedEvent(file.FullName);
            }

            _filesCache = files.Select(f => f.FullName).ToList();
            _timestamp = files.Max(f => f.LastWriteTime);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            _cancellationToken.Cancel();
        }
    }
}