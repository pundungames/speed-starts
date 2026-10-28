using System;
using System.IO;
using NUnit.Framework;
using SpeedStars.Meta.Persistence;

namespace SpeedStars.Tests.Meta.Persistence
{
    /// <summary>
    /// FileSaveStore against a throw-away temp folder (never persistentDataPath, so a test run cannot touch a real save).
    /// Crashes are simulated by leaving the files in the state a crash would.
    /// </summary>
    public sealed class FileSaveStoreTests
    {
        private const string Key = "slot";

        private string _root;
        private FileSaveStore _store;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "SpeedStarsTests", Guid.NewGuid().ToString("N"));
            _store = new FileSaveStore(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root)) Directory.Delete(_root, true);
        }

        [Test]
        public void TryRead_NothingWritten_ReturnsFalse()
        {
            Assert.IsFalse(_store.TryRead(Key, out string contents));
            Assert.IsNull(contents);
        }

        [Test]
        public void Write_ThenRead_RoundTripsContentIncludingNonAscii()
        {
            const string text = "{\"name\":\"Mor Mücevher ğüşıöç İ\"}";
            _store.Write(Key, text);

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual(text, contents);
        }

        [Test]
        public void Write_EmptyString_RoundTrips()
        {
            _store.Write(Key, string.Empty);

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual(string.Empty, contents);
        }

        [Test]
        public void Write_Twice_KeepsPreviousContentAsBackup_AndLeavesNoTempFile()
        {
            _store.Write(Key, "first");
            _store.Write(Key, "second");

            Assert.IsTrue(File.Exists(_store.GetBackupPath(Key)));
            Assert.IsFalse(File.Exists(_store.GetTempPath(Key)));

            File.Delete(_store.GetMainPath(Key));
            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual("first", contents);
        }

        [Test]
        public void HalfWrittenTempFile_DoesNotChangeWhatIsRead()
        {
            _store.Write(Key, "committed");
            // A crash while the next save was still writing its temp file.
            File.WriteAllText(_store.GetTempPath(Key), "{\"cash\":12");

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual("committed", contents);
        }

        [Test]
        public void HalfWrittenTempFile_NextWriteSucceeds()
        {
            _store.Write(Key, "committed");
            File.WriteAllText(_store.GetTempPath(Key), "{\"cash\":12");

            _store.Write(Key, "next");

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual("next", contents);
            Assert.IsFalse(File.Exists(_store.GetTempPath(Key)));
        }

        [Test]
        public void CrashBetweenBackupAndSwap_ReadsPreviousContent()
        {
            _store.Write(Key, "old");
            // State right after "main -> bak" and before "tmp -> main": main is gone, temp holds the unfinished write.
            File.Move(_store.GetMainPath(Key), _store.GetBackupPath(Key));
            File.WriteAllText(_store.GetTempPath(Key), "new but never confirmed");

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual("old", contents);
        }

        [Test]
        public void TruncatedMainFile_FallsBackToBackup()
        {
            _store.Write(Key, "good old");
            _store.Write(Key, "good new with some length");
            string main = _store.GetMainPath(Key);
            byte[] bytes = File.ReadAllBytes(main);
            Array.Resize(ref bytes, bytes.Length - 5);
            File.WriteAllBytes(main, bytes);

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual("good old", contents);
        }

        [Test]
        public void ZeroFilledMainFile_FallsBackToBackup()
        {
            _store.Write(Key, "good old");
            _store.Write(Key, "good new");
            string main = _store.GetMainPath(Key);
            File.WriteAllBytes(main, new byte[new FileInfo(main).Length]);

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual("good old", contents);
        }

        [Test]
        public void ChangedPayloadByte_IsDetectedAsBroken()
        {
            _store.Write(Key, "only copy");
            string main = _store.GetMainPath(Key);
            byte[] bytes = File.ReadAllBytes(main);
            bytes[bytes.Length - 1] ^= 0x01;
            File.WriteAllBytes(main, bytes);

            Assert.IsFalse(_store.TryRead(Key, out _));
        }

        [Test]
        public void HeaderlessFile_IsDetectedAsBroken()
        {
            Directory.CreateDirectory(_root);
            File.WriteAllText(_store.GetMainPath(Key), "{\"cash\":1}");

            Assert.IsFalse(_store.TryRead(Key, out _));
        }

        [Test]
        public void MainAndBackupBroken_ReturnsFalse()
        {
            _store.Write(Key, "a");
            _store.Write(Key, "b");
            File.WriteAllText(_store.GetMainPath(Key), "garbage");
            File.WriteAllText(_store.GetBackupPath(Key), "garbage");

            Assert.IsFalse(_store.TryRead(Key, out string contents));
            Assert.IsNull(contents);
        }

        [Test]
        public void WriteOverBrokenMain_KeepsLastGoodBackup()
        {
            _store.Write(Key, "last good");
            _store.Write(Key, "will break");
            File.WriteAllText(_store.GetMainPath(Key), "garbage");

            _store.Write(Key, "fresh");
            // Break the fresh file too: the backup must still be the last good copy, not the garbage.
            File.WriteAllText(_store.GetMainPath(Key), "garbage again");

            Assert.IsTrue(_store.TryRead(Key, out string contents));
            Assert.AreEqual("last good", contents);
        }

        [Test]
        public void Delete_RemovesMainBackupAndTemp()
        {
            _store.Write(Key, "a");
            _store.Write(Key, "b");
            File.WriteAllText(_store.GetTempPath(Key), "partial");

            _store.Delete(Key);

            Assert.IsFalse(_store.TryRead(Key, out _));
            Assert.IsFalse(File.Exists(_store.GetMainPath(Key)));
            Assert.IsFalse(File.Exists(_store.GetBackupPath(Key)));
            Assert.IsFalse(File.Exists(_store.GetTempPath(Key)));
        }

        [Test]
        public void Delete_UnknownKey_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => _store.Delete(Key));
        }

        [Test]
        public void Keys_AreIndependent()
        {
            _store.Write("a", "alpha");
            _store.Write("b", "beta");

            Assert.IsTrue(_store.TryRead("a", out string a));
            Assert.IsTrue(_store.TryRead("b", out string b));
            Assert.AreEqual("alpha", a);
            Assert.AreEqual("beta", b);
        }

        [Test]
        public void NewInstanceOnSameRoot_ReadsWhatTheOldOneWrote()
        {
            _store.Write(Key, "persisted");

            var reopened = new FileSaveStore(_root);

            Assert.IsTrue(reopened.TryRead(Key, out string contents));
            Assert.AreEqual("persisted", contents);
        }

        [TestCase("")]
        [TestCase("../escape")]
        [TestCase("sub/key")]
        [TestCase("sub\\key")]
        public void InvalidKey_Throws(string key)
        {
            Assert.Throws<ArgumentException>(() => _store.Write(key, "x"));
        }

        [Test]
        public void EmptyRoot_Throws()
        {
            Assert.Throws<ArgumentException>(() => new FileSaveStore(" "));
        }
    }
}
