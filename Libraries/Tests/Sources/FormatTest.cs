﻿/* ------------------------------------------------------------------------- */
//
// Copyright (c) 2010 CubeSoft, Inc.
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as
// published by the Free Software Foundation, either version 3 of the
// License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//
/* ------------------------------------------------------------------------- */
using Cube.FileSystem.TestService;
using NUnit.Framework;
using System;

namespace Cube.FileSystem.SevenZip.Tests
{
    /* --------------------------------------------------------------------- */
    ///
    /// FormatTest
    ///
    /// <summary>
    /// Format に関わる機能のテスト用クラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    [TestFixture]
    class FormatTest : FileFixture
    {
        #region Tests

        /* ----------------------------------------------------------------- */
        ///
        /// Detect
        ///
        /// <summary>
        /// 圧縮ファイル形式を判別するテストを実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase("Sample.txt",      ExpectedResult = Format.Unknown)]
        [TestCase("Empty.txt",       ExpectedResult = Format.Unknown)]
        [TestCase("Password.7z",     ExpectedResult = Format.SevenZip)]
        [TestCase("Sample.cab",      ExpectedResult = Format.Cab)]
        [TestCase("Sample.chm",      ExpectedResult = Format.Chm)]
        [TestCase("Sample.docx",     ExpectedResult = Format.Zip)]
        [TestCase("Sample.exe",      ExpectedResult = Format.PE)]
        [TestCase("Sample.flv",      ExpectedResult = Format.Flv)]
        [TestCase("Sample.jar",      ExpectedResult = Format.Zip)]
        [TestCase("Sample.lha",      ExpectedResult = Format.Lzh)]
        [TestCase("Sample.lzh",      ExpectedResult = Format.Lzh)]
        [TestCase("Sample.nupkg",    ExpectedResult = Format.Zip)]
        [TestCase("Sample.pptx",     ExpectedResult = Format.Zip)]
        [TestCase("Sample.rar",      ExpectedResult = Format.Rar)]
        [TestCase("Sample.rar5",     ExpectedResult = Format.Rar5)]
        [TestCase("Sample.tar",      ExpectedResult = Format.Tar)]
        [TestCase("Sample.tar.z",    ExpectedResult = Format.Lzw)]
        [TestCase("Sample.tbz",      ExpectedResult = Format.BZip2)]
        [TestCase("Sample.tgz",      ExpectedResult = Format.GZip)]
        [TestCase("Sample.txz",      ExpectedResult = Format.XZ)]
        [TestCase("Sample.xlsx",     ExpectedResult = Format.Zip)]
        [TestCase("Sample.zip",      ExpectedResult = Format.Zip)]
        [TestCase("SampleSfx.exe",   ExpectedResult = Format.Sfx)]
        public Format Detect(string filename)
        {
            var src  = GetExamplesWith(filename);
            var dest = GetResultsWith(Guid.NewGuid().ToString("D"));
            IO.Copy(src, dest);
            return Formats.FromFile(dest);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// FromFile_NotFound
        ///
        /// <summary>
        /// 存在しないファイルを指定した時の挙動を確認します。
        /// </summary>
        ///
        /// <remarks>
        /// ファイルが存在しなかった場合、拡張子から判断します。
        /// </remarks>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void FromFile_NotFound()=> Assert.That(
            Formats.FromFile(GetExamplesWith("NotFound.rar")),
            Is.EqualTo(Format.Rar)
        );

        /* ----------------------------------------------------------------- */
        ///
        /// FromStream_CannotRead
        ///
        /// <summary>
        /// 書き込み専用のストリームを指定した時の挙動を確認します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void FromStream_CannotRead() => Assert.That(
            () => Formats.FromStream(IO.OpenWrite(GetExamplesWith("Sample.zip"))),
            Throws.TypeOf<NotSupportedException>()
        );

        /* ----------------------------------------------------------------- */
        ///
        /// ToExtension
        ///
        /// <summary>
        /// 拡張子に変換するテストを実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase(Format.Zip,      ExpectedResult = ".zip")]
        [TestCase(Format.SevenZip, ExpectedResult = ".7z")]
        [TestCase(Format.BZip2,    ExpectedResult = ".bz2")]
        [TestCase(Format.GZip,     ExpectedResult = ".gz")]
        [TestCase(Format.Lzw,      ExpectedResult = ".z")]
        [TestCase(Format.Sfx,      ExpectedResult = ".exe")]
        [TestCase(Format.Unknown,  ExpectedResult = "")]
        public string ToExtension(Format format) => format.ToExtension();

        /* ----------------------------------------------------------------- */
        ///
        /// ToFormat
        ///
        /// <summary>
        /// CompressionMethod から Format に変換するテストを実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase(CompressionMethod.GZip, ExpectedResult = Format.GZip)]
        [TestCase(CompressionMethod.Lzma, ExpectedResult = Format.Unknown)]
        public Format ToFormat(CompressionMethod method) => method.ToFormat();

        /* ----------------------------------------------------------------- */
        ///
        /// ToMethod
        ///
        /// <summary>
        /// Format から CompressionMethod に変換するテストを実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase(Format.BZip2, ExpectedResult = CompressionMethod.BZip2)]
        [TestCase(Format.Zip,   ExpectedResult = CompressionMethod.Default)]
        public CompressionMethod ToMethod(Format format) => format.ToMethod();

        #endregion
    }
}
