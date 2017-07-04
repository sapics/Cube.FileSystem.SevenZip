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
using System;
using System.Collections.Generic;
using System.Linq;
using Cube.FileSystem.SevenZip;

namespace Cube.FileSystem.App.Ice
{
    /* --------------------------------------------------------------------- */
    ///
    /// Request
    ///
    /// <summary>
    /// CubeICE へのリクエスト内容を表すオブジェクトです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public class Request
    {
        #region Constructors

        /* ----------------------------------------------------------------- */
        ///
        /// Request
        /// 
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// 
        /// <param name="args">プログラムオプション</param>
        /// 
        /* ----------------------------------------------------------------- */
        public Request(string[] args)
        {
            if (args == null || args.Length <= 0) return;

            var mode = args[0];
            if (mode.Length < 2 || mode[0] != '/') return;

            switch (mode[1])
            {
                case 'c':
                    Mode   = Mode.Archive;
                    Format = GetFormat(mode);
                    break;
                case 'x':
                    Mode   = Mode.Extract;
                    Format = Format.Unknown;
                    break;
                default:
                    return;
            }

            var sources = new List<string>();
            for (var i = 1; i < args.Length; ++i)
            {
                if (!args[i].StartsWith("/")) sources.Add(args[i]);
                else if (args[i].StartsWith("/o")) Location = GetLocation(args[i]);
                else if (args[i] == "p") Password = true;
            }
            Sources = sources;
        }

        #endregion

        #region Properties

        /* ----------------------------------------------------------------- */
        ///
        /// Mode
        /// 
        /// <summary>
        /// 実行モードを取得または設定します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public Mode Mode { get; set; } = Mode.None;

        /* ----------------------------------------------------------------- */
        ///
        /// Format
        /// 
        /// <summary>
        /// フォーマットを取得または設定します。
        /// </summary>
        /// 
        /// <remarks>
        /// このプロパティは Mode が Archive の時に有効です。
        /// </remarks>
        /// 
        /* ----------------------------------------------------------------- */
        public Format Format { get; set; } = Format.Zip;

        /* ----------------------------------------------------------------- */
        ///
        /// Location
        /// 
        /// <summary>
        /// 圧縮または展開したファイルの保存位置を取得または設定します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public SaveLocation Location { get; set; } = SaveLocation.Runtime;

        /* ----------------------------------------------------------------- */
        ///
        /// Password
        /// 
        /// <summary>
        /// 圧縮ファイルにパスワードを設定するかどうかを示す値を取得
        /// または設定します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public bool Password { get; set; } = false;

        /* ----------------------------------------------------------------- */
        ///
        /// Sources
        /// 
        /// <summary>
        /// 圧縮または解凍ファイル一覧を取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public IEnumerable<string> Sources { get; }

        #endregion

        #region Implementations

        /* ----------------------------------------------------------------- */
        ///
        /// GetFormat
        /// 
        /// <summary>
        /// 文字列に対応する Format オブジェクトを取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private Format GetFormat(string s)
        {
            var index = s.IndexOf(':');
            if (index < 0 || index >= s.Length - 1) return Format.Zip;

            var query = s.Substring(index + 1).ToLower();
            return FormatConversions.FromString(query);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// GetLocation
        /// 
        /// <summary>
        /// 文字列に対応する SaveLocation オブジェクトを取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private SaveLocation GetLocation(string s)
        {
            var index = s.IndexOf(':');
            if (index < 0 || index >= s.Length - 1) return SaveLocation.Runtime;

            var query = s.Substring(index + 1).ToLower();
            foreach (SaveLocation item in Enum.GetValues(typeof(SaveLocation)))
            {
                if (item.ToString().ToLower() == query) return item;
            }
            return SaveLocation.Runtime;
        }

        #endregion
    }

    /* --------------------------------------------------------------------- */
    ///
    /// Mode
    ///
    /// <summary>
    /// 実行モードを表す列挙型です。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public enum Mode
    {
        None,
        Archive,
        Extract,
    }

    /* --------------------------------------------------------------------- */
    ///
    /// SaveLocation
    ///
    /// <summary>
    /// 保存場所を表す列挙型です。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public enum SaveLocation
    {
        Source,
        Desktop,
        MyDocuments,
        Runtime,
    }
}
