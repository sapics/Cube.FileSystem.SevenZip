﻿/* ------------------------------------------------------------------------- */
///
/// Copyright (c) 2010 CubeSoft, Inc.
/// 
/// Licensed under the Apache License, Version 2.0 (the "License");
/// you may not use this file except in compliance with the License.
/// You may obtain a copy of the License at
///
///  http://www.apache.org/licenses/LICENSE-2.0
///
/// Unless required by applicable law or agreed to in writing, software
/// distributed under the License is distributed on an "AS IS" BASIS,
/// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
/// See the License for the specific language governing permissions and
/// limitations under the License.
///
/* ------------------------------------------------------------------------- */
using System.IO;
using NUnit.Framework;
using Cube.FileSystem.Files;

namespace Cube.FileSystem.Tests
{
    /* --------------------------------------------------------------------- */
    ///
    /// FileInfoTest
    /// 
    /// <summary>
    /// FileInfo の拡張メソッドのテスト用クラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    [Parallelizable]
    [TestFixture]
    class FileInfoTest : FileResource
    {
        /* ----------------------------------------------------------------- */
        ///
        /// GetTypeName
        ///
        /// <summary>
        /// ファイルの種類を表す文字列を取得するテストを実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [TestCase("Sample.txt")]
        [TestCase("NotExist.dummy")]
        public void GetTypeName(string filename)
        {
            var info = new FileInfo(Example(filename));
            Assert.That(info.GetTypeName(), Is.Not.Null.And.Not.Empty);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// GetTypeName_Null
        ///
        /// <summary>
        /// Null が指定された時の挙動を確認します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        [Test]
        public void GetTypeName_Null()
            => Assert.That(Operations.GetTypeName(null), Is.Null);
    }
}
