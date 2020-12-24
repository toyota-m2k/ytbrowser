using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace ytbrowser {
    public static class FileUtil {
        public static async Task<StorageFile> GetFile(string name) {
            var dir = ApplicationData.Current.LocalFolder;
            try {
                return await dir.TryGetItemAsync(name) as StorageFile;
            }
            catch (Exception e) {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        public static async Task<StorageFile> GetOrCreateFile(string name) {
            var dir = ApplicationData.Current.LocalFolder;
            try { 
                return await dir.CreateFileAsync(name, CreationCollisionOption.OpenIfExists);
            } catch (Exception e) {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

    }
}
