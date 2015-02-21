using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using RxApp;
using Splat;
using Microsoft.FSharp.Core;
using FSharpx.Collections;

using Unit = System.Reactive.Unit;

namespace FacebookPagesApp
{
    public interface IPagesViewModel : INavigationModel, IServiceViewModel
    {
        IObservable<string> UserName { get; }

        IObservable<IBitmap> ProfilePhoto { get; }

        IRxProperty<bool> ShowUnpublishedPosts { get; }

        IRxProperty<FSharpOption<FacebookAPI.Page>> CurrentPage { get; }

        IObservable<IReadOnlyList<FacebookAPI.Page>> Pages { get; }

        IObservable<bool> ShowRefresher { get; }

        IRxCommand CreatePost { get; }

        IRxCommand LogOut { get; }

        IObservable<IReadOnlyList<FacebookAPI.Post>> Posts { get; }

        IRxCommand RefeshPosts { get; }

        IRxCommand LoadMorePosts { get; }
    }

    public interface IPagesControllerModel : INavigationModel, IServiceControllerModel
    {
        IRxProperty<string> UserName { get; }

        IRxProperty<IBitmap> ProfilePhoto { get; }

        IObservable<bool> ShowUnpublishedPosts { get; }

        IObservable<FSharpOption<FacebookAPI.Page>> CurrentPage { get; }

        IRxProperty<IReadOnlyList<FacebookAPI.Page>> Pages { get; }

        IRxProperty<bool> ShowRefresher { get; }

        IObservable<Unit> CreatePost { get; }

        IObservable<Unit> LogOut { get; }

        IRxProperty<FacebookAPI.PostFeed> Posts { get; }

        IRxProperty<bool> CanLoadMorePosts { get; }
        IObservable<Unit> LoadMorePosts { get; }

        IObservable<Unit> RefreshPosts { get; }
    }

    public sealed class PagesModel : MobileModel, IPagesViewModel, IPagesControllerModel
    { 
        private readonly IRxProperty<FacebookAPI.PostFeed> _posts = 
            RxProperty.Create(FacebookAPI.PostFeedModule.Empty);

        private readonly IRxCommand _loadMorePosts;
        private readonly IRxProperty<bool> _canLoadMorePosts = RxProperty.Create(false);
        private readonly IRxCommand _refreshPosts;

        private readonly IRxCommand _createPost = RxCommand.Create();
        private readonly IRxCommand _logOut = RxCommand.Create();

        private readonly IRxProperty<bool> _showRefresher = RxProperty.Create(false);

        private readonly IRxProperty<IReadOnlyList<FacebookAPI.Page>> _pages = 
            RxProperty.Create((IReadOnlyList<FacebookAPI.Page>) new List<FacebookAPI.Page>());
        
        private readonly IRxProperty<bool> _showUnpublishedPosts =  RxProperty.Create<bool>(false);
        private readonly IRxProperty<string> _userName = RxProperty.Create<string>("");
        private readonly IRxProperty<IBitmap> _profilePhoto = RxProperty.Create<IBitmap>(null);

        private IRxProperty<FSharpOption<FacebookAPI.Page>> _currentPage = RxProperty.Create(FSharpOption<FacebookAPI.Page>.None);

        public PagesModel()
        {
            _loadMorePosts = _canLoadMorePosts.ToCommand();

            // FIXME: Consider always allowing a pull to refresh
            // and instead use an async lock to prevent multiple loads of data
            _refreshPosts = _canLoadMorePosts.ToCommand();
        }

        IRxProperty<bool> IPagesViewModel.ShowUnpublishedPosts { get { return _showUnpublishedPosts; } }

        IObservable<bool> IPagesControllerModel.ShowUnpublishedPosts { get { return _showUnpublishedPosts; } }


        IObservable<string> IPagesViewModel.UserName { get { return _userName; } }

        IRxProperty<string> IPagesControllerModel.UserName { get { return _userName; } }


        IObservable<IBitmap> IPagesViewModel.ProfilePhoto { get { return _profilePhoto; } }

        IRxProperty<IBitmap> IPagesControllerModel.ProfilePhoto { get { return _profilePhoto; } }


        IRxProperty<FSharpOption<FacebookAPI.Page>> IPagesViewModel.CurrentPage { get { return _currentPage; } }

        IObservable<FSharpOption<FacebookAPI.Page>> IPagesControllerModel.CurrentPage { get { return _currentPage; } }


        IObservable<IReadOnlyList<FacebookAPI.Page>> IPagesViewModel.Pages { get { return _pages; } }

        IRxProperty<IReadOnlyList<FacebookAPI.Page>> IPagesControllerModel.Pages { get { return _pages; } }


        IRxCommand IPagesViewModel.CreatePost { get { return _createPost; } }

        IObservable<Unit> IPagesControllerModel.CreatePost { get { return _createPost; } }


        IRxCommand IPagesViewModel.LogOut { get { return _logOut; } }

        IObservable<Unit> IPagesControllerModel.LogOut { get { return _logOut; } }


        IRxCommand IPagesViewModel.RefeshPosts { get { return _refreshPosts; } }

        IObservable<Unit> IPagesControllerModel.RefreshPosts { get { return _refreshPosts; } }


        IRxCommand IPagesViewModel.LoadMorePosts { get { return _loadMorePosts; } }

        IObservable<Unit> IPagesControllerModel.LoadMorePosts { get { return _loadMorePosts; } }

        IRxProperty<bool> IPagesControllerModel.CanLoadMorePosts { get { return _canLoadMorePosts; } }


        IObservable<IReadOnlyList<FacebookAPI.Post>> IPagesViewModel.Posts { get { return this._posts.Select(x => x.posts); } }

        IRxProperty<FacebookAPI.PostFeed> IPagesControllerModel.Posts { get { return this._posts; } }

 
        IRxProperty<bool> IPagesControllerModel.ShowRefresher { get { return this._showRefresher; } }

        IObservable<bool> IPagesViewModel.ShowRefresher { get { return this._showRefresher; } }
    }
}

