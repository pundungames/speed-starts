using System;
using System.Globalization;
using System.IO;
using System.Text;
using SpeedStars.Core.Services;

namespace SpeedStars.Meta.Persistence
{
    /// <summary>
    /// <c>ISaveStore</c> on local files: one file per key under a root folder given by the caller (tests pass a temp
    /// folder; the game uses <c>CreateDefault</c>). Each file starts with a one-line header holding a hash of the content,
    /// so a torn, truncated or zero-filled file is recognised as broken instead of being handed to the parser.
    /// Write never touches the live file until the new content is fully on disk: it writes "key.json.tmp", moves the
    /// current file to "key.json.bak", then moves the temp file into place. At every step at least one intact copy
    /// exists, and TryRead falls back to the backup when the main file is missing or broken — a crash or power cut
    /// mid-save costs at most that one write, never the previous state.
    /// </summary>
    public sealed class FileSaveStore : ISaveStore
    {
        private const string FileExtension = ".json";
        private const string TempSuffix = ".tmp";
        private const string BackupSuffix = ".bak";
        private const string DefaultFolderName = "Save";

        // Format marker + space + 16 hex digits of the payload hash, then '\n', then the UTF-8 payload.
        private const string HeaderPrefix = "SSAVE1 ";
        private const byte HeaderEnd = (byte)'\n';

        // FNV-1a 64-bit: algorithm constants, not tuning values. Detects accidental damage, not tampering.
        private const ulong FnvOffsetBasis = 14695981039346656037UL;
        private const ulong FnvPrime = 1099511628211UL;

        private static readonly Encoding Utf8 = new UTF8Encoding(false);

        public FileSaveStore(string rootDirectory)
        {
            if (string.IsNullOrWhiteSpace(rootDirectory))
                throw new ArgumentException("A root directory is required.", nameof(rootDirectory));
            RootDirectory = rootDirectory;
        }

        /// <summary>The game's store under <c>Application.persistentDataPath</c>; the only place the engine path is used.</summary>
        public static FileSaveStore CreateDefault()
        {
            return new FileSaveStore(Path.Combine(UnityEngine.Application.persistentDataPath, DefaultFolderName));
        }

        public string RootDirectory { get; }

        public string GetMainPath(string key) => Path.Combine(RootDirectory, ValidKey(key) + FileExtension);
        public string GetBackupPath(string key) => GetMainPath(key) + BackupSuffix;
        public string GetTempPath(string key) => GetMainPath(key) + TempSuffix;

        /// <summary>
        /// Main file if intact, else the backup, else false. A leftover temp file is ignored: its write never finished,
        /// so the caller never saw it succeed.
        /// </summary>
        public bool TryRead(string key, out string contents)
        {
            string main = GetMainPath(key);
            if (TryReadIntact(main, out contents)) return true;
            if (TryReadIntact(main + BackupSuffix, out contents)) return true;
            contents = null;
            return false;
        }

        /// <summary>Throws (IOException and friends) if the disk refuses; the previous content is then still readable.</summary>
        public void Write(string key, string contents)
        {
            if (contents == null) throw new ArgumentNullException(nameof(contents));

            string main = GetMainPath(key);
            string temp = main + TempSuffix;
            string backup = main + BackupSuffix;

            Directory.CreateDirectory(RootDirectory);
            WriteToDisk(temp, Encode(contents));

            if (TryReadIntact(main, out _))
            {
                DeleteIfExists(backup);
                File.Move(main, backup);
            }
            else
            {
                // A broken main must not become the backup, or the last good copy would be thrown away.
                DeleteIfExists(main);
            }

            File.Move(temp, main);
        }

        public void Delete(string key)
        {
            string main = GetMainPath(key);
            DeleteIfExists(main);
            DeleteIfExists(main + BackupSuffix);
            DeleteIfExists(main + TempSuffix);
        }

        private static string ValidKey(string key)
        {
            if (string.IsNullOrEmpty(key)
                || key.IndexOf('/') >= 0
                || key.IndexOf('\\') >= 0
                || key.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                throw new ArgumentException("Save key must be a plain file name: " + key, nameof(key));
            return key;
        }

        private static void WriteToDisk(string path, byte[] bytes)
        {
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                stream.Write(bytes, 0, bytes.Length);
                // Push past the OS cache: without it a power cut can leave a moved-in but empty file.
                stream.Flush(true);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path)) File.Delete(path);
        }

        private static bool TryReadIntact(string path, out string contents)
        {
            contents = null;
            if (!File.Exists(path)) return false;

            byte[] bytes;
            try
            {
                bytes = File.ReadAllBytes(path);
            }
            catch (IOException)
            {
                return false;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }

            return TryDecode(bytes, out contents);
        }

        private static byte[] Encode(string contents)
        {
            byte[] payload = Utf8.GetBytes(contents);
            string hex = Hash(payload, 0, payload.Length).ToString("x16", CultureInfo.InvariantCulture);
            byte[] header = Encoding.ASCII.GetBytes(HeaderPrefix + hex);

            var bytes = new byte[header.Length + 1 + payload.Length];
            Buffer.BlockCopy(header, 0, bytes, 0, header.Length);
            bytes[header.Length] = HeaderEnd;
            Buffer.BlockCopy(payload, 0, bytes, header.Length + 1, payload.Length);
            return bytes;
        }

        private static bool TryDecode(byte[] bytes, out string contents)
        {
            contents = null;
            int end = Array.IndexOf(bytes, HeaderEnd);
            if (end < 0) return false;

            string header = Encoding.ASCII.GetString(bytes, 0, end);
            if (!header.StartsWith(HeaderPrefix, StringComparison.Ordinal)) return false;
            if (!ulong.TryParse(header.Substring(HeaderPrefix.Length), NumberStyles.AllowHexSpecifier,
                    CultureInfo.InvariantCulture, out ulong expected))
                return false;

            int start = end + 1;
            if (Hash(bytes, start, bytes.Length - start) != expected) return false;

            contents = Utf8.GetString(bytes, start, bytes.Length - start);
            return true;
        }

        private static ulong Hash(byte[] data, int offset, int count)
        {
            ulong hash = FnvOffsetBasis;
            int stop = offset + count;
            for (int i = offset; i < stop; i++)
            {
                hash ^= data[i];
                hash *= FnvPrime;
            }
            return hash;
        }
    }
}
