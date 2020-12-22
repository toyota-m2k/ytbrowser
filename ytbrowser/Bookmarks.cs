﻿using common;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Serialization;

namespace ytbrowser {
    public class Bookmarks : ObservableCollection<Bookmarks.BookmarkRec> {
        public class BookmarkRec {
            public string Name { get; set; }
            public string Url { get; set; }

            public BookmarkRec() {

            }

            public BookmarkRec(string name, string url) {
                Name = name;
                Url = url;
            }
        }

        private static Bookmarks sInstance = null;
        public static Bookmarks Instance => sInstance;
        public static void Initialize() {
            if (null == sInstance) {
                sInstance = Deserialize();
            }
        }
        public static void Terminate() {
            sInstance?.Serialize();
            sInstance = null;
        }

        private bool Serialize() {
            try {
                var serializer = new XmlSerializer(this.GetType());
                //書き込むファイルを開く（UTF-8 BOM無し）
                using (var sw = new System.IO.StreamWriter("bookmarks.xml", false, new System.Text.UTF8Encoding(false))) {

                    //シリアル化し、XMLファイルに保存する
                    serializer.Serialize(sw, this);
                    sw.Close();
                    return true;
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                return false;
            }
        }

        private static Bookmarks Deserialize() {
            try {
                //XmlSerializerオブジェクトを作成
                var serializer = new XmlSerializer(typeof(Bookmarks));

                //読み込むファイルを開く
                using (var sr = new StreamReader("bookmarks.xml", new UTF8Encoding(false))) {

                    //XMLファイルから読み込み、逆シリアル化する
                    var obj = serializer.Deserialize(sr);
                    if (null == obj) {
                        throw new Exception("cannot be deserialized.");
                    }
                    return (Bookmarks)obj;
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e);
                return new Bookmarks();
            }
        }


        public void AddBookmark(string name, string url) {
            var rec = FindBookmark(url);
            if (rec != null) {
                this.Move(IndexOf(rec), 0);
            } else {
                this.Insert(0, new BookmarkRec(name, url));
            }
        }
        
        public BookmarkRec FindBookmark(string url) {
            var org = this.Where((bm) => bm.Url == url);
            if (!Utils.IsNullOrEmpty(org)) {
                return org.First();
            }
            return null;
        }

        public BookmarkRec RemoveBookmark(string url) {
            var r = FindBookmark(url);
            if (null != r) {
                Remove(r);
            }
            return r;
        }

        public BookmarkRec BringUpBookmark(string url) {
            var r = FindBookmark(url);
            if (null != r) {
                var i = IndexOf(r);
                if (i != 0) {
                    Move(i, 0);
                }
            }
            return r;
        }
    }
}

