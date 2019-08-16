using SimpleEventBus.Abstractions.Incoming;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SimpleEventBus.FileTransport
{
    class FileOperations
    {
        private readonly string busPath;
        private readonly string thisSubscriberPath;
        private readonly string hiddenPath;
        private readonly string deadLetterPath;

        public FileOperations(string busPath)
        {
            this.busPath = busPath;

            thisSubscriberPath = Path.Combine(
                busPath,
                Guid.NewGuid().ToString("n", CultureInfo.InvariantCulture));

            hiddenPath = Path.Combine(
                thisSubscriberPath,
                "hidden");

            deadLetterPath = Path.Combine(
                thisSubscriberPath,
                "deadLetter");

            Directory.CreateDirectory(thisSubscriberPath);
            Directory.CreateDirectory(hiddenPath);
            Directory.CreateDirectory(deadLetterPath);
        }

        public void DeleteAllSubscriberFiles()
        {
            if (Directory.Exists(thisSubscriberPath))
            {
                Directory.Delete(thisSubscriberPath, true);
            }
        }

        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase")]
        public FileName GetHiddenFileForMessage(IncomingMessage message)
        {
            foreach (var file in Directory.EnumerateFiles(hiddenPath))
            {
                if (file.EndsWith(
                    message.Id.ToLowerInvariant() + ".json",
                    StringComparison.OrdinalIgnoreCase))
                {
                    return new FileName(file);
                }
            }

            throw new KeyNotFoundException();
        }

        public void DequeueAndMoveToHiddenFolder(FileName file, DateTime lockExpiresAtUtc)
        {
            var fullHiddenPath = FileName.CreateWithIncrementedDequeueCount(file, hiddenPath, lockExpiresAtUtc);
            File.Move(file.ToString(), fullHiddenPath.ToString());
        }

        public void MoveOutOfHiddenFolder(FileName file)
        {
            var fullVisiblePath = FileName.Create(file, thisSubscriberPath, DateTime.UtcNow);
            File.Move(file.ToString(), fullVisiblePath.ToString());
        }

        public void MoveToDeadLetterFolder(FileName file)
        {
            var fullDeadLetterPath = FileName.Create(file, deadLetterPath);
            File.Move(file.ToString(), fullDeadLetterPath.ToString());
        }

        public void RefreshHiddenUntil(FileName hiddenFile, DateTime newHiddenUntilUtc)
        {
            var newFileName = FileName.Create(hiddenFile, newHiddenUntilUtc);
            File.Move(hiddenFile.ToString(), newFileName.ToString());
        }

        public void Delete(FileName file)
            => File.Delete(file.ToString());

        public IEnumerable<FileName> GetHiddenFiles()
            => Directory
                .EnumerateFiles(hiddenPath, "*.json")
                .Select(filePath => new FileName(filePath));

        public IEnumerable<FileName> GetQueuedFiles()
            => Directory
                .EnumerateFiles(thisSubscriberPath, "*.json")
                .OrderBy(filePath => filePath)
                .Select(filePath => new FileName(filePath));

        public IEnumerable<string> GetAllSubscriberPaths()
            => Directory.EnumerateDirectories(busPath);
    }
}
