using common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace ytbrowser
{
    public class Requester : IDisposable {
        
        public Requester() {
            Open();
        }

        Stream mStream;

        private async void Open() {
            if (null == mStream) {
                try {
                    var file = await FileUtil.GetOrCreateFile("request.txt");
                    var stream = await file.OpenAsync(FileAccessMode.ReadWrite, StorageOpenOptions.AllowOnlyReaders);
                    mStream = stream.AsStreamForWrite();
                } catch(Exception e) {
                    Debug.WriteLine(e);
                }
            }
        }

        public async void Request(string url) {
            if (mStream == null) return;
            try {
                var urlBytes = System.Text.Encoding.UTF8.GetBytes(url + "\r\n");
                mStream.Seek(0, SeekOrigin.End);
                await mStream.WriteAsync(urlBytes, 0, urlBytes.Length);
                await mStream.FlushAsync();
                Debug.WriteLine($"URL:{url}");
            } catch(Exception e) {
                Debug.WriteLine(e);
            }
        }

        public void Dispose() {
            mStream?.Close();
            mStream?.Dispose();
            mStream = null;
        }
    }

    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Requester UrlRequester = null;
        private LoadingMonitor LMonitor { get; }
        private BrowserViewModel ViewModel {
            get => DataContext as BrowserViewModel;
            set {
                ViewModel?.Dispose();
                ViewModel = value;
            }
        }

        public MainPage() {
            DataContext = new BrowserViewModel();
            LMonitor = new LoadingMonitor(ViewModel);
            ViewModel.NavigateCommand.Subscribe(Navigate);
            ViewModel.GoBackCommand.Subscribe(GoBack);
            ViewModel.GoForwardCommand.Subscribe(GoForward);
            ViewModel.ReloadCommand.Subscribe(Reload);
            ViewModel.StopCommand.Subscribe(Stop);
            ViewModel.AddBookmarkCommand.Subscribe(() => AddBookmark(Browser.DocumentTitle, ViewModel.Url.Value));
            ViewModel.DelBookmarkCommand.Subscribe(() => DelBookmark(ViewModel.Url.Value));
            ViewModel.ShowBookmarkCommand.Subscribe(ShowBookmarks);
            ViewModel.CopyToClipboardCommand.Subscribe(()=>RequestDownload(ViewModel.Url.Value));

            this.InitializeComponent();
        }

        private string prevUrl = null;
        private void RequestDownload(string url) {
            var uri = FixUpUrl(url);
            if (null != uri && uri.Host=="www.youtube.com" && uri.AbsolutePath=="/watch") {
                if (uri.ToString() != prevUrl) {
                    prevUrl = uri.ToString();
                    UrlRequester.Request(uri.ToString());
                    //Clipboard.SetContent(new DataPackage().Apply((dp) => dp.SetText(uri.ToString())));
                }
            }
        }

        private void ShowBookmarks() {
            ViewModel.ShowBookmark.Value = !ViewModel.ShowBookmark.Value;
        }

        private void DelBookmark(string value) {
            ViewModel.BookmarkList.Value.RemoveBookmark(value);
            ViewModel.Url.ForceNotify();
        }

        private void AddBookmark(string name, string value) {
            ViewModel.BookmarkList.Value.AddBookmark(name, value);
            ViewModel.Url.ForceNotify();
        }

        private static Uri FixUpUrl(string url) {
            if (string.IsNullOrEmpty(url)) {
                return null;
            }
            try {
                return new Uri(url);
            }
            catch (Exception) {
                if (url.StartsWith("//")) {
                    return FixUpUrl("http:" + url);
                } else if (!url.StartsWith("http")) {
                    return FixUpUrl("http://" + url);
                } else {
                    return null;
                }
            }
        }


        public void Navigate(string url) {
            var uri = FixUpUrl(url);
            if(uri==null) {
                return;
            }
            Browser.Navigate(uri);
        }

        void Stop() {
            LMonitor.Renew();
            Browser.Stop();
        }

        void Reload() {
            Browser.Refresh();
        }

        void GoBack() {
            Browser.GoBack();
        }

        void GoForward() {
            Browser.GoForward();
        }

        void UpdateHistory() {
            ViewModel.HasPrev.Value = Browser.CanGoBack;
            ViewModel.HasNext.Value = Browser.CanGoForward;
        }


        private string callerName([CallerMemberName] string memberName = "") {
            return memberName;
        }

        #region Navigation

        private void WebView_NavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            Debug.WriteLine(callerName());
            LMonitor.Renew();
            UpdateHistory();
            RequestDownload(args.Uri.ToString());

            var state = Window.Current.CoreWindow.GetKeyState(VirtualKey.Control);
            if ((state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down) {
                args.Cancel = true;
            }
        }

        private void WebView_ContentLoading(WebView sender, WebViewContentLoadingEventArgs args) {
            Debug.WriteLine(callerName());
            ViewModel.ShowBookmark.Value = false;
            ViewModel.Url.Value = args.Uri.ToString();
            LMonitor.OnStartLoading(args.Uri.ToString(), false);
            UpdateHistory();
            RequestDownload(args.Uri.ToString());
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {
            Debug.WriteLine(callerName());
            UpdateHistory();
            //var script = @"
            //        var els = document.getElementsByTagName('a');
            //        Array.prototype.map.call(els, (v) => {
            //            v.onmouseover = function(e) {
            //                window.external.notify('i=' + e.currentTarget.href);
            //            }
            //            v.onmouseleave = function(e) {
            //                window.external.notify('o=' + e.currentTarget.href);
            //            }
            //        })";
            //_ = Browser.InvokeScriptAsync("eval", new string[] { script });
        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args) {
            Debug.WriteLine(callerName());
            LMonitor.OnEndLoading(args.Uri.ToString(), false);
            UpdateHistory();
        }

        private void WebView_NavigationFailed(object sender, WebViewNavigationFailedEventArgs e) {
            Debug.WriteLine(callerName());
        }

        private void WebView_LoadCompleted(object sender, NavigationEventArgs e) {
            Debug.WriteLine(callerName());
        }

        #endregion

        #region Frame Navigation

        private void WebView_FrameNavigationStarting(WebView sender, WebViewNavigationStartingEventArgs args) {
            Debug.WriteLine($"{callerName()} ({args.Uri})");
            var url = args.Uri.ToString();
            if (url != "about:blank" && !url.StartsWith("javascript:")) {
                UpdateHistory();
            }
        }

        private void WebView_FrameContentLoading(WebView sender, WebViewContentLoadingEventArgs args) {
            Debug.WriteLine(callerName());
            LMonitor.OnStartLoading(args.Uri.ToString(), true);
            UpdateHistory();
        }

        private void WebView_FrameDOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args) {
            Debug.WriteLine(callerName());
            UpdateHistory();
        }

        private void WebView_FrameNavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args) {
            Debug.WriteLine(callerName());
            LMonitor.OnEndLoading(args.Uri.ToString(), true);
            UpdateHistory();
        }


        private void WebView_ContainsFullScreenElementChanged(WebView sender, object args) {
            Debug.WriteLine(callerName());
        }

        #endregion

        #region Special Cases

        private void WebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs args) {
            Debug.WriteLine(callerName());
            // 自分自身に表示
            Navigate(args.Uri.ToString());
            args.Handled = true;
        }

        private void WebView_PermissionRequested(WebView sender, WebViewPermissionRequestedEventArgs args) {
            Debug.WriteLine(callerName());
            args.PermissionRequest.Deny();
        }

        private void WebView_LongRunningScriptDetected(WebView sender, WebViewLongRunningScriptDetectedEventArgs args) {
            Debug.WriteLine($"{callerName()} ExecutionTime={args.ExecutionTime}");
            //args.StopPageScriptExecution = true;
        }

        #endregion

        #region Interaction

        private void WebView_ScriptNotify(object sender, NotifyEventArgs e) {
            Debug.WriteLine($"{callerName()} {e.Value}");
            switch (e.Value[0]) {
                case 'i':
                    ViewModel.StatusLine.Value = e.Value.Substring(2);
                    break;
                case 'o':
                    LMonitor.ResumeStatusLine();
                    break;
                //case 'c':
                //    CopyCommand.Execute(e.Value.Substring(2));
                //    break;
                default:
                    Debug.Assert(false, $"unknown command:{e.Value}");
                    break;
            }
        }

        #endregion

        #region Unhandled Potential Probrems 

        private void WebView_UnsafeContentWarningDisplaying(WebView sender, object args) {
            Debug.WriteLine(callerName());
        }

        private void WebView_UnsupportedUriSchemeIdentified(WebView sender, WebViewUnsupportedUriSchemeIdentifiedEventArgs args) {
            Debug.WriteLine(callerName());
        }

        private void WebView_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args) {
            Debug.WriteLine(callerName());
        }
        #endregion

        private void UrlKeyDown(object sender, KeyRoutedEventArgs e) {
           if (e.Key == VirtualKey.Enter && sender is TextBox) {
                Navigate(((TextBox)sender).Text);
                e.Handled = true;
            }
        }

        private string InitialUrl = null;
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            base.OnNavigatedTo(e);
            InitialUrl = e.Parameter as string;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) {
            UrlRequester = new Requester();
            Navigate(!string.IsNullOrEmpty(InitialUrl) ? InitialUrl : "https://www.youtube.com");
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) {
            Debug.WriteLine(callerName());
            UrlRequester?.Dispose();
            UrlRequester = null;
        }

    }
}
