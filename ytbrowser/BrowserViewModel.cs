﻿using Reactive.Bindings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ytbrowser {
    public class BrowserViewModel : ViewModelBase {
        public ReactiveProperty<Bookmarks> BookmarkList { get; } = new ReactiveProperty<Bookmarks>();
        public ReactiveProperty<bool> ShowBookmark { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<string> Url { get; } = new ReactiveProperty<string>();

        public ReactiveProperty<bool> HasPrev { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> HasNext { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<bool> Loading { get; } = new ReactiveProperty<bool>(false);

        public ReadOnlyReactiveProperty<bool> IsBookmarked { get; }
        public ReactiveProperty<string> StatusLine { get; } = new ReactiveProperty<string>();

        public ReactiveCommand GoBackCommand { get; } = new ReactiveCommand();
        public ReactiveCommand GoForwardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ReloadCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClearURLCommand { get; } = new ReactiveCommand();
        public ReactiveCommand AddBookmarkCommand { get; } = new ReactiveCommand();
        public ReactiveCommand DelBookmarkCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ShowBookmarkCommand { get; } = new ReactiveCommand();
        public ReactiveCommand CopyToClipboardCommand { get; } = new ReactiveCommand();
        public ReactiveCommand<string> NavigateCommand { get; } = new ReactiveCommand<string>();

        public BrowserViewModel() {
            Bookmarks.GetInstance((v) => {
                BookmarkList.Value = v;
            });
            ClearURLCommand.Subscribe(()=> {
                Url.Value = "";
            });
            IsBookmarked = Url.Select((url) => {
                return BookmarkList.Value?.FindBookmark(url) != null;
            }).ToReadOnlyReactiveProperty();
        }
    }
}
