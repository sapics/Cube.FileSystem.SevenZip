﻿// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
/* ------------------------------------------------------------------------- */
using NUnit.Framework;
using System;
using System.Linq;

namespace Cube.FileSystem.SevenZip.Tests
{
    /* --------------------------------------------------------------------- */
    ///
    /// ArchiveWriterExtTest
    ///
    /// <summary>
    /// Represents additional tests for the ArchiveWriter class.
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    [TestFixture]
    class ArchiveWriterExtTest : ArchiveFixture
    {
        #region Tests

         /* ----------------------------------------------------------------- */
        ///
        /// Archive_Filter
        ///
        /// <summary>
        /// フィルタ設定の結果を確認します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase(true,  ExpectedResult = 5)]
        [TestCase(false, ExpectedResult = 9)]
        public int Archive_Filter(bool filter)
        {
            var names = new[] { "Filter.txt", "FilterDirectory" };
            var s     = filter ? "True" : "False";
            var dest  = GetResultsWith($"Filter{s}.zip");

            using (var writer = new ArchiveWriter(Format.Zip))
            {
                if (filter) writer.Filters = names;
                writer.Add(GetExamplesWith("Sample.txt"));
                writer.Add(GetExamplesWith("Sample 00..01"));
                writer.Save(dest);
            }

            using (var reader = new ArchiveReader(dest)) return reader.Items.Count;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Archive_Japanese
        ///
        /// <summary>
        /// 日本語のファイル名を含むファイルを圧縮するテストを実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase(true)]
        [TestCase(false)]
        public void Archive_Japanese(bool utf8)
        {
            var fmt  = Format.Zip;
            var src  = GetResultsWith("日本語のファイル名.txt");
            var code = utf8 ? "UTF8" : "SJis";
            var dest = GetResultsWith($"ZipJapanese{code}.zip");

            IO.Copy(GetExamplesWith("Sample.txt"), src, true);
            Assert.That(IO.Exists(src), Is.True);

            using (var writer = new ArchiveWriter(fmt))
            {
                writer.Option = new ZipOption { CodePage = utf8 ? CodePage.Utf8 : CodePage.Japanese };
                writer.Add(src);
                writer.Save(dest);
            }

            using (var stream = System.IO.File.OpenRead(dest))
            {
                Assert.That(Formats.FromStream(stream), Is.EqualTo(fmt));
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Archive_PasswordCancel
        ///
        /// <summary>
        /// パスワードの設定をキャンセルした時の挙動を確認します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void Archive_PasswordCancel() => Assert.That(() =>
        {
            using (var writer = new ArchiveWriter(Format.Zip))
            {
                var dest  = GetResultsWith("PasswordCancel.zip");
                var query = new Query<string>(e => e.Cancel = true);
                writer.Add(GetExamplesWith("Sample.txt"));
                writer.Save(dest, query, null);
            }
        }, Throws.TypeOf<OperationCanceledException>());

        /* ----------------------------------------------------------------- */
        ///
        /// Archive_SfxNotFound
        ///
        /// <summary>
        /// 存在しない SFX モジュールを設定した時の挙動を確認します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void Archive_SfxNotFound() => Assert.That(() =>
        {
            using (var writer = new ArchiveWriter(Format.Sfx))
            {
                var dest = GetResultsWith("SfxNotFound.exe");
                writer.Option = new SfxOption { Module = "dummy.sfx" };
                writer.Add(GetExamplesWith("Sample.txt"));
                writer.Save(dest);
            }
        }, Throws.TypeOf<System.IO.FileNotFoundException>());

        /* ----------------------------------------------------------------- */
        ///
        /// Archive_PermissionError
        ///
        /// <summary>
        /// 読み込みできないファイルを指定した時の挙動を確認します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void Archive_PermissionError() => Assert.That(() =>
        {
            var dir = GetResultsWith("PermissionError");
            var src = IO.Combine(dir, "Sample.txt");

            IO.Copy(GetExamplesWith("Sample.txt"), src);

            using (var _ = OpenExclude(src))
            using (var writer = new ArchiveWriter(Format.Zip))
            {
                writer.Add(src);
                writer.Save(IO.Combine(dir, "Sample.zip"));
            }
        }, Throws.TypeOf<System.IO.IOException>());

        /* ----------------------------------------------------------------- */
        ///
        /// Archive_Skip
        ///
        /// <summary>
        /// 一部のファイルを無視して圧縮するテストを実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void Archive_Skip()
        {
            var dir    = GetResultsWith("Ignore");
            var ignore = IO.Combine(dir, "Sample.txt");

            var io = new IO();
            io.Failed += (s, e) => e.Cancel = true;
            io.Copy(GetExamplesWith("Sample.txt"), ignore);

            var dest = io.Combine(dir, "Sample.zip");

            using (var _ = OpenExclude(ignore))
            using (var writer = new ArchiveWriter(Format.Zip, io))
            {
                writer.Add(ignore);
                writer.Add(GetExamplesWith("Sample 00..01"));
                writer.Save(dest);
            }

            using (var reader = new ArchiveReader(dest))
            {
                Assert.That(reader.Items.Count, Is.EqualTo(8));
                Assert.That(reader.Items.Any(x => x.FullName == "Sample.txt"), Is.False);
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Add_NotFound
        ///
        /// <summary>
        /// 存在しないファイルを指定した時の挙動を確認します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void Add_NotFound() => Assert.That(() =>
        {
            using (var writer = new ArchiveWriter(Format.Zip))
            {
                writer.Add(GetExamplesWith("NotFound.txt"));
            }
        }, Throws.TypeOf<System.IO.FileNotFoundException>());

        #endregion

        #region Others

        /* ----------------------------------------------------------------- */
        ///
        /// OpenExclude
        ///
        /// <summary>
        /// ファイルを排他モードで開きます。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        private System.IO.Stream OpenExclude(string path) =>
            System.IO.File.Open(path,
                System.IO.FileMode.Open,
                System.IO.FileAccess.ReadWrite,
                System.IO.FileShare.None
            );

        #endregion
    }
}
