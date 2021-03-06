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
using Cube.FileSystem.SevenZip.Mixin;
using Cube.Generics;
using Cube.Log;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Cube.FileSystem.SevenZip
{
    /* --------------------------------------------------------------------- */
    ///
    /// ArchiveExtractCallback
    ///
    /// <summary>
    /// 圧縮ファイルを展開する際のコールバック関数群を定義したクラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    internal sealed class ArchiveExtractCallback :
        ArchivePasswordCallback, IArchiveExtractCallback, IDisposable
    {
        #region Constructors

        /* ----------------------------------------------------------------- */
        ///
        /// ArchiveExtractCallback
        ///
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        ///
        /// <param name="src">圧縮ファイルのパス</param>
        /// <param name="dest">展開先ディレクトリ</param>
        /// <param name="items">展開項目一覧</param>
        /// <param name="io">ファイル操作用オブジェクト</param>
        ///
        /* ----------------------------------------------------------------- */
        public ArchiveExtractCallback(string src, string dest, IEnumerable<ArchiveItem> items, IO io)
            : base(src, io)
        {
            _dispose     = new OnceAction<bool>(Dispose);
            Destination  = dest;
            Items        = items;
            TotalCount   = -1;
            TotalBytes   = -1;
            Report.Count = 0;
            Report.Bytes = 0;
            _inner       = Items.GetEnumerator();
        }

        #endregion

        #region Properties

        /* ----------------------------------------------------------------- */
        ///
        /// Destination
        ///
        /// <summary>
        /// 展開先ディレクトリのパスを取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public string Destination { get; }

        /* ----------------------------------------------------------------- */
        ///
        /// Items
        ///
        /// <summary>
        /// 展開する項目一覧を取得します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public IEnumerable<ArchiveItem> Items { get; }

        /* ----------------------------------------------------------------- */
        ///
        /// Filters
        ///
        /// <summary>
        /// 展開をスキップするファイル名またはディレクトリ名一覧を
        /// 取得または設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public IEnumerable<string> Filters { get; set; }

        /* ----------------------------------------------------------------- */
        ///
        /// TotalCount
        ///
        /// <summary>
        /// 展開後のファイルおよびディレクトリの合計を取得または
        /// 設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public long TotalCount
        {
            get => Report.TotalCount;
            set => Report.TotalCount = value;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// TotalBytes
        ///
        /// <summary>
        /// 展開後の総バイト数を取得または設定します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public long TotalBytes
        {
            get => Report.TotalBytes;
            set => Report.TotalBytes = value;
        }

        #endregion

        #region Methods

        #region IArchiveExtractCallback

        /* ----------------------------------------------------------------- */
        ///
        /// SetTotal
        ///
        /// <summary>
        /// 展開後のバイト数を通知します。
        /// </summary>
        ///
        /// <param name="bytes">バイト数</param>
        ///
        /* ----------------------------------------------------------------- */
        public void SetTotal(ulong bytes) => Invoke(() =>
        {
            if (TotalCount < 0) TotalCount = Items.Count();
            if (TotalBytes < 0) TotalBytes = (long)bytes;

            _hack = Math.Max((long)bytes - Report.TotalBytes, 0);
        });

        /* ----------------------------------------------------------------- */
        ///
        /// SetCompleted
        ///
        /// <summary>
        /// 展開の完了したバイトサイズを通知します。
        /// </summary>
        ///
        /// <param name="bytes">展開の完了したバイト数</param>
        ///
        /// <remarks>
        /// IInArchive.Extract を複数回実行する場合、SetTotal および
        /// SetCompleted で取得できる値が Format によって異なります。
        /// 例えば、zip の場合は毎回 Extract に指定したファイルのバイト数を
        /// 表しますが、7z の場合はそれまでに Extract で展開した累積
        /// バイト数となります。ArchiveExtractCallback では Format 毎の
        /// 違いをなくすために正規化しています。
        /// </remarks>
        ///
        /* ----------------------------------------------------------------- */
        public void SetCompleted(ref ulong bytes)
        {
            var cvt = Math.Min(Math.Max((long)bytes - _hack, 0), Report.TotalBytes);
            Invoke(() => Report.Bytes = cvt);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// GetStream
        ///
        /// <summary>
        /// 展開した内容を保存するためのストリームを取得します。
        /// </summary>
        ///
        /// <param name="index">圧縮ファイル中のインデックス</param>
        /// <param name="stream">出力ストリーム</param>
        /// <param name="mode">展開モード</param>
        ///
        /// <returns>OperationResult</returns>
        ///
        /* ----------------------------------------------------------------- */
        public int GetStream(uint index, out ISequentialOutStream stream, AskMode mode)
        {
            stream = Invoke(() => CreateStream(index, mode), false);
            return (int)Result;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// PrepareOperation
        ///
        /// <summary>
        /// 展開処理の直前に実行されます。
        /// </summary>
        ///
        /// <param name="mode">展開モード</param>
        ///
        /* ----------------------------------------------------------------- */
        public void PrepareOperation(AskMode mode)
        {
            var item = _inner.Current;
            if (item != null && _streams.ContainsKey(item)) Invoke(() =>
            {
                Report.Current = item;
                Report.Status  = ReportStatus.Begin;
            });
        }

        /* ----------------------------------------------------------------- */
        ///
        /// SetOperationResult
        ///
        /// <summary>
        /// 処理結果を通知します。
        /// </summary>
        ///
        /// <param name="result">処理結果</param>
        ///
        /* ----------------------------------------------------------------- */
        public void SetOperationResult(OperationResult result) => Invoke(() =>
        {
            var item = _inner.Current;
            if (item != null && _streams.ContainsKey(item))
            {
                _streams[item].Dispose();
                _streams.Remove(item);
            }

            Teminate(item, result);
            Result = result;
        });

        #endregion

        #region IDisposable

        /* ----------------------------------------------------------------- */
        ///
        /// ~ArchiveExtractCallback
        ///
        /// <summary>
        /// オブジェクトを破棄します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        ~ArchiveExtractCallback() { _dispose.Invoke(false); }

        /* ----------------------------------------------------------------- */
        ///
        /// Dispose
        ///
        /// <summary>
        /// リソースを開放します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        public void Dispose()
        {
            _dispose.Invoke(true);
            GC.SuppressFinalize(this);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Dispose
        ///
        /// <summary>
        /// リソースを開放します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var item in _streams)
                {
                    item.Value.Dispose();
                    Invoke(() => Teminate(item.Key, Result));
                }
                _streams.Clear();
            }
        }

        #endregion

        #endregion

        #region Implementations

        /* ----------------------------------------------------------------- */
        ///
        /// CreateStream
        ///
        /// <summary>
        /// ストリームを生成します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        private ArchiveStreamWriter CreateStream(uint index, AskMode mode)
        {
            if (Result != OperationResult.OK || mode != AskMode.Extract) return null;

            while (_inner.MoveNext())
            {
                var src = _inner.Current;

                if (src.Index != index) continue;
                if (!src.FullName.HasValue()) return Skip();
                if (Filters != null && src.Match(Filters)) return Skip();
                if (src.IsDirectory) return CreateDirectory();

                var dest = new ArchiveStreamWriter(IO.Create(IO.Combine(Destination, src.FullName)));
                _streams.Add(src, dest);
                return dest;
            }

            return null;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// CreateDirectory
        ///
        /// <summary>
        /// ディレクトリを生成します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        private ArchiveStreamWriter CreateDirectory()
        {
            Report.Current = _inner.Current;
            Report.Status  = ReportStatus.Begin;
            _inner.Current.CreateDirectory(Destination, IO);
            return null;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Skip
        ///
        /// <summary>
        /// 展開処理をスキップします。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        private ArchiveStreamWriter Skip()
        {
            this.LogDebug($"Skip:{_inner.Current.FullName}");
            return null;
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Terminate
        ///
        /// <summary>
        /// Invokes post processing.
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        private void Teminate(ArchiveItem src, OperationResult result)
        {
            if (result == OperationResult.OK) src.SetAttributes(Destination, IO);
            Report.Current = src;
            Report.Status  = ReportStatus.End;
            Report.Count++;
        }

        #endregion

        #region Fields
        private readonly OnceAction<bool> _dispose;
        private readonly IEnumerator<ArchiveItem> _inner;
        private readonly IDictionary<ArchiveItem, ArchiveStreamWriter> _streams = new Dictionary<ArchiveItem, ArchiveStreamWriter>();
        private long _hack = 0;
        #endregion
    }
}
