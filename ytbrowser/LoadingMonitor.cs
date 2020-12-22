using common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ytbrowser {
    public class LoadingMonitor {
        private WeakReference<BrowserViewModel> mViewModel;
        private BrowserViewModel ViewModel => mViewModel?.GetValue();

        public LoadingMonitor(BrowserViewModel viewModel) {
            mViewModel = new WeakReference<BrowserViewModel>(viewModel);
        }

        class Info {
            ulong Generation;
            public string Url;
            public bool Frame;

            public Info(ulong gene, string url, bool frame) {
                Generation = gene;
                Url = url;
                Frame = frame;
            }

            public bool IsSame(string url, bool frame) {
                return url == Url && frame == Frame;
            }
        }

        ulong Generation = 0;
        static IEnumerable<Info> EMPTY = new Info[0];
        IEnumerable<Info> Loadings = EMPTY;

        IEnumerable<Info> One(Info info) {
            yield return info;
        }

        public void Renew() {
            Generation++;
            Loadings = EMPTY;
            //ViewModel.Loading.Value = false;
            ViewModel.StatusLine.Value = "";
        }

        public void OnStartLoading(string url, bool frame) {
            Loadings = Loadings.Concat(One(new Info(Generation, url, frame)));
            // 挙動から推測して、
            // ドキュメントのロードが完了してから、Frameのロードが始まり、その場合、NavigationCompletedイベントは発行されないようだ。
            // なので、frame の Loadが開始されるタイミングで、NavigationCompletedを受け取ったものとしてみる。
            if (frame) {
                Loadings = Loadings.Where((v) => { return v.Frame; });
            }
            ViewModel.Loading.Value = !Utils.IsNullOrEmpty(Loadings);
            ViewModel.StatusLine.Value = $"Loading> {url}";
        }

        public void OnEndLoading(string url, bool frame) {
            Loadings = Loadings.Where((v) => !v.IsSame(url, frame)) ?? EMPTY;
            ViewModel.Loading.Value = !Utils.IsNullOrEmpty(Loadings);
            ResumeStatusLine();
        }

        public void ResumeStatusLine() {
            if (Utils.IsNullOrEmpty(Loadings)) {
                ViewModel.StatusLine.Value = "Ready";
            } else {
                var last = Loadings.Last()?.Url;
                ViewModel.StatusLine.Value = (string.IsNullOrEmpty(last)) ? "Ready" : $"Loading> {last}";
            }
        }
    }
}
