using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Text;

namespace SimpleEventBus.FileTransport
{
    class FileName
    {
        private readonly string filePath;
        private static readonly char[] separatorArray = new char[] { '.' };

        public FileName(string filePath)
        {
            this.filePath = filePath;
        }

        public static FileName Create(string folderPath, string messageId)
            => Create(folderPath, messageId, DateTime.UtcNow);

        public static FileName Create(string folderPath, string messageId, DateTime timestampUtc)
            => new FileName(
                Path.Combine(
                    folderPath,
                    GetName(timestampUtc, 0, messageId)));

        public static FileName CreateWithIncrementedDequeueCount(FileName file, string newFolder, DateTime timestampUtc)
            => new FileName(
                Path.Combine(
                    newFolder,
                    GetName(timestampUtc, file.DequeueCount+1, file.MessageId)));


        public static FileName Create(FileName file, string newFolder)
            => new FileName(
                Path.Combine(
                    newFolder,
                    GetName(DateTime.UtcNow, file.DequeueCount, file.MessageId)));

        public static FileName Create(FileName file, DateTime newTimestampUtc)
            => new FileName(
                Path.Combine(
                    Path.GetDirectoryName(file.filePath),
                    GetName(newTimestampUtc, file.DequeueCount, file.MessageId)));

        public static FileName Create(FileName file, string newFolder, DateTime newTimestampUtc)
            => new FileName(
                Path.Combine(
                    newFolder,
                    GetName(newTimestampUtc, file.DequeueCount, file.MessageId)));

        public bool HasTimestampPassed(DateTime checkTimeUtc)
        {
            var timeExpired = GetTimestampString(checkTimeUtc);
            return string.CompareOrdinal(
                Path.GetFileNameWithoutExtension(filePath), timeExpired) < 0;
        }

        public void WriteAllUtf8Text(string text)
            => File.WriteAllText(filePath, text, Encoding.UTF8);

        public JObject ReadContentsAsJObject()
            => JObject.Parse(File.ReadAllText(filePath));

        public override string ToString()
            => filePath;

        public int DequeueCount
            => int.Parse(
                Path.GetFileName(filePath)
                    .Split(separatorArray, StringSplitOptions.None)[1],
                CultureInfo.InvariantCulture);

        public string MessageId
            => Path.GetFileName(filePath)
                .Split(separatorArray, StringSplitOptions.None)[2];

        [SuppressMessage("", "CA1308", Justification = "We want a lowercase name in the filename.")]
        private static string GetName(DateTime timestamp, int dequeueCount, string messageId)
            => $"{GetTimestampString(timestamp)}.{dequeueCount}.{messageId.ToLowerInvariant()}.json";

        private static string GetTimestampString(DateTime utcValue)
            => utcValue.ToString(
                "yyyyMMddHHmmssfffffff",
                CultureInfo.InvariantCulture);
    }
}
