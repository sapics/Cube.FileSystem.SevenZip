﻿/* ------------------------------------------------------------------------- */
///
/// Copyright (c) 2010 CubeSoft, Inc.
///
/// This program is free software: you can redistribute it and/or modify
/// it under the terms of the GNU Lesser General Public License as
/// published by the Free Software Foundation, either version 3 of the
/// License, or (at your option) any later version.
///
/// This program is distributed in the hope that it will be useful,
/// but WITHOUT ANY WARRANTY; without even the implied warranty of
/// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
/// GNU Lesser General Public License for more details.
///
/// You should have received a copy of the GNU Lesser General Public License
/// along with this program.  If not, see <http://www.gnu.org/licenses/>.
///
/* ------------------------------------------------------------------------- */
using System;
using System.Collections.Generic;

namespace Cube.FileSystem.SevenZip
{
    /* --------------------------------------------------------------------- */
    ///
    /// ArchiveWriter
    /// 
    /// <summary>
    /// 圧縮ファイルを作成するクラスです。
    /// </summary>
    ///
    /* --------------------------------------------------------------------- */
    public class ArchiveWriter : IDisposable
    {
        #region Constructors

        /* ----------------------------------------------------------------- */
        ///
        /// ArchiveWriter
        ///
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// 
        /// <param name="format">圧縮フォーマット</param>
        /// 
        /* ----------------------------------------------------------------- */
        public ArchiveWriter(Format format) : this(format, new Operator()) { }

        /* ----------------------------------------------------------------- */
        ///
        /// ArchiveWriter
        ///
        /// <summary>
        /// オブジェクトを初期化します。
        /// </summary>
        /// 
        /// <param name="format">圧縮フォーマット</param>
        /// <param name="io">ファイル操作用オブジェクト</param>
        /// 
        /* ----------------------------------------------------------------- */
        public ArchiveWriter(Format format, Operator io)
        {
            Format = format;
            _7z = new SevenZipLibrary();
            _io = io;
        }

        #endregion

        #region Properties

        /* ----------------------------------------------------------------- */
        ///
        /// Format
        ///
        /// <summary>
        /// 圧縮ファイルのフォーマットを取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public Format Format { get; }

        /* ----------------------------------------------------------------- */
        ///
        /// Option
        ///
        /// <summary>
        /// 圧縮ファイルのフォーマットを取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public ArchiveOption Option { get; set; }

        #endregion

        #region Methods

        /* ----------------------------------------------------------------- */
        ///
        /// Add
        ///
        /// <summary>
        /// ファイルまたはディレクトリを圧縮ファイルに追加します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public void Add(string path)
            => Add(path, _io.Get(path).Name);

        /* ----------------------------------------------------------------- */
        ///
        /// Add
        ///
        /// <summary>
        /// ファイルまたはフォルダを圧縮ファイルに追加します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public void Add(string path, string pathInArchive)
        {
            var info = _io.Get(path);
            if (info.Exists) Add(info, pathInArchive);
            else throw new System.IO.FileNotFoundException(info.FullName);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Save
        ///
        /// <summary>
        /// 圧縮ファイルを作成し保存します。
        /// </summary>
        /// 
        /// <param name="path">保存パス</param>
        /// 
        /* ----------------------------------------------------------------- */
        public void Save(string path) => Save(path, string.Empty);

        /* ----------------------------------------------------------------- */
        ///
        /// Save
        ///
        /// <summary>
        /// 圧縮ファイルを作成し保存します。
        /// </summary>
        /// 
        /// <param name="path">保存パス</param>
        /// <param name="password">パスワード</param>
        /// 
        /* ----------------------------------------------------------------- */
        public void Save(string path, string password)
        {
            var query = new PasswordQuery(password);

            if (Format == Format.Executable) SaveCoreExe(path, query, null, _items);
            else if (Format == Format.Tar) SaveCoreTar(path, query, null, _items);
            else SaveCore(Format, path, query, null, _items);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Save
        ///
        /// <summary>
        /// 圧縮ファイルを作成し保存します。
        /// </summary>
        /// 
        /// <param name="path">保存パス</param>
        /// <param name="password">パスワード取得用オブジェクト</param>
        /// <param name="progress">進捗状況報告用オブジェクト</param>
        /// 
        /* ----------------------------------------------------------------- */
        public void Save(string path, IQuery<string, string> password, IProgress<ArchiveReport> progress)
        {
            var query = new PasswordQuery(password);

            if (Format == Format.Executable) SaveCoreExe(path, query, progress, _items);
            else if (Format == Format.Tar) SaveCoreTar(path, query, progress, _items);
            else SaveCore(Format, path, query, progress, _items);
        }

        #region IDisposable

        /* ----------------------------------------------------------------- */
        ///
        /// ~ArchiveWriter
        ///
        /// <summary>
        /// オブジェクトを破棄します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        // ~ArchiveWriter() {
        //   Dispose(false);
        // }

        /* ----------------------------------------------------------------- */
        ///
        /// Dispose
        ///
        /// <summary>
        /// リソースを破棄します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        public void Dispose()
        {
            Dispose(true);
            // GC.SuppressFinalize(this);
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Dispose
        ///
        /// <summary>
        /// リソースを破棄します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing) _7z?.Dispose();

            _disposed = true;
        }

        #endregion

        #endregion

        #region Implementations

        /* ----------------------------------------------------------------- */
        ///
        /// SaveCoreExe
        ///
        /// <summary>
        /// 自己解凍形式ファイルを作成し保存します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void SaveCoreExe(string path, IQuery<string, string> password,
            IProgress<ArchiveReport> progress, IList<FileItem> items)
        {
            var tmp  = _io.Combine(_io.Get(path).DirectoryName, Guid.NewGuid().ToString("D"));

            try
            {
                SaveCore(Format.SevenZip, tmp, password, progress, items);

                var sfx = (Option as ExecutableOption)?.Module;
                if (string.IsNullOrEmpty(sfx)) throw new System.IO.FileNotFoundException();

                using (var dest = _io.Create(path))
                {
                    using (var src = _io.OpenRead(sfx)) src.CopyTo(dest);
                    using (var src = _io.OpenRead(tmp)) src.CopyTo(dest);
                }
            }
            finally { if (_io.Get(tmp).Exists) _io.Delete(tmp); }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// SaveCoreTar
        ///
        /// <summary>
        /// Tar ファイルを作成し保存します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void SaveCoreTar(string path, IQuery<string, string> password,
            IProgress<ArchiveReport> progress, IList<FileItem> items)
        {
            var info = _io.Get(path);
            var nwe  = _io.Get(info.NameWithoutExtension);
            var name = (nwe.Extension == ".tar") ? nwe.Name : $"{nwe.Name}.tar";
            var dir  = _io.Combine(info.DirectoryName, Guid.NewGuid().ToString("D"));
            var tmp  = _io.Combine(dir, name);

            try
            {
                SaveCore(Format.Tar, tmp, password, progress, items);

                var f = new List<FileItem> { new FileItem(tmp) };
                var m = (Option as TarOption)?.CompressionMethod ?? CompressionMethod.Copy;

                switch (m)
                {
                    case CompressionMethod.BZip2:
                    case CompressionMethod.GZip:
                    case CompressionMethod.XZ:
                        SaveCore(FormatConversions.FromMethod(m), path, password, progress, f);
                        break;
                    default: // Copy
                        _io.Move(tmp, path, true);
                        break;
                }
            }
            finally { if (_io.Get(dir).Exists) _io.Delete(dir); }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// SaveCore
        ///
        /// <summary>
        /// 圧縮ファイルを作成し保存します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void SaveCore(Format format, string path, IQuery<string, string> password,
            IProgress<ArchiveReport> progress, IList<FileItem> items)
        {
            var dir = _io.Get(_io.Get(path).DirectoryName);
            if (!dir.Exists) _io.CreateDirectory(dir.FullName);

            var raw      = _7z.GetOutArchive(format);
            var stream   = new ArchiveStreamWriter(_io.Create(path));
            var callback = new ArchiveUpdateCallback(items, path, _io)
            {
                Password = password,
                Progress = progress,
            };

            try
            {
                if (Option != null) GetSetter()?.Execute(raw as ISetProperties);
                raw.UpdateItems(stream, (uint)items.Count, callback);
            }
            finally
            {
                var result = callback.Result;
                stream.Dispose();
                callback.Dispose();
                SaveResult(path, result);
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// SaveResult
        ///
        /// <summary>
        /// 圧縮後の処理を実行します。
        /// </summary>
        ///
        /* ----------------------------------------------------------------- */
        private void SaveResult(string path, OperationResult result)
        {
            switch (result)
            {
                case OperationResult.OK:
                    break;
                case OperationResult.UserCancel:
                    throw new UserCancelException();
                default:
                    throw new System.IO.IOException(result.ToString());
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// Add
        ///
        /// <summary>
        /// ファイルまたはディレクトリを圧縮ファイルに追加します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private void Add(IInformation info, string name)
        {
            var path = info.FullName;
            _items.Add(new FileItem(path, name));
            if (!info.IsDirectory) return;

            foreach (var file in _io.GetFiles(path))
            {
                var child = _io.Get(file);
                _items.Add(new FileItem(child.FullName, _io.Combine(name, child.Name)));
            }

            foreach (var dir in _io.GetDirectories(path))
            {
                var child = _io.Get(dir);
                Add(child, _io.Combine(name, child.Name));
            }
        }

        /* ----------------------------------------------------------------- */
        ///
        /// GetSetter
        ///
        /// <summary>
        /// ArchiveOptionSetter オブジェクトを取得します。
        /// </summary>
        /// 
        /* ----------------------------------------------------------------- */
        private ArchiveOptionSetter GetSetter()
        {
            switch (Format)
            {
                case Format.Zip:
                    return new ZipOptionSetter(Option);
                case Format.SevenZip:
                case Format.Executable:
                    return new SevenZipOptionSetter(Option);
                case Format.Tar:
                    return null;
                default:
                    return new ArchiveOptionSetter(Option);
            }
        }

        #region Fields
        private bool _disposed = false;
        private SevenZipLibrary _7z;
        private Operator _io;
        private IList<FileItem> _items = new List<FileItem>();
        #endregion

        #endregion
    }
}
