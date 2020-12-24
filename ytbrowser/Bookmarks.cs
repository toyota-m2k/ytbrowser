using common;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using Windows.Storage;

namespace ytbrowser {
    public class Bookmarks : ObservableCollection<Bookmarks.BookmarkRec> {
        public class BookmarkRec {
            public string Name { get; set; }
            public string Url { get; set; }
            public string DisplayName => string.IsNullOrWhiteSpace(Name) ? Url : Name;

            public BookmarkRec() {

            }

            public BookmarkRec(string name, string url) {
                Name = name;
                Url = url;
            }
        }

        private static Bookmarks sInstance = null;
        //public static Bookmarks Instance => sInstance;
        public static TaskCompletionSource<Bookmarks> InitTaskSource = new TaskCompletionSource<Bookmarks>();
        public static async void Initialize() {
            if (null == sInstance) {
                sInstance = await Deserialize();
                InitTaskSource.TrySetResult(sInstance);
            }
        }
        public static async Task Terminate() {
            await sInstance?.Run(async (bm) => {
                await bm.Serialize();
            });
            sInstance = null;
        }
        public static async void GetInstance(Action<Bookmarks> completed) {
            var instance = await InitTaskSource.Task;
            completed(instance);
        }

        private const string FILENAME = "bookmarks.xml";

        private async Task<bool> Serialize() {
            try {
                var serializer = new XmlSerializer(this.GetType());
                using (var sw = new StringWriter(new StringBuilder())) { 
                    //シリアル化し、XMLファイルに保存する
                    serializer.Serialize(sw, this);
                    var xml = sw.ToString();
                    if(!string.IsNullOrWhiteSpace(xml)) {
                        var file = await FileUtil.GetOrCreateFile(FILENAME);
                        if (null != file) {
                            await FileIO.WriteTextAsync(file, sw.ToString());
                            return true;
                        }
                    }
                }
            }
            catch (Exception e) {
                Debug.WriteLine(e);
            }
            return false;
        }

        private static async Task<Bookmarks> Deserialize() {
            try {
                // xmlファイルから読み込む
                var file = await FileUtil.GetFile(FILENAME);
                if(file==null) {
                    return new Bookmarks();
                }

                var xml = await FileIO.ReadTextAsync(file);
                using (var sr = new StringReader(xml)) {
                    //XMLファイルから読み込み、逆シリアル化する
                    //XmlSerializerオブジェクトを作成
                    var serializer = new XmlSerializer(typeof(Bookmarks));
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

