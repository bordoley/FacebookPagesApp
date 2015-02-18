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
    public interface IPagesViewModel : INavigableViewModel, IServiceViewModel
    {
        IObservable<string> UserName { get; }
        IObservable<IBitmap> ProfilePhoto { get; }
        IRxProperty<bool> ShowUnpublishedPosts { get; }

        FacebookAPI.Page CurrentPage { set; }

        IObservable<IEnumerable<FacebookAPI.Page>> Pages { get; }

        IRxCommand CreatePost { get; }

        IRxCommand LogOut { get; }


        IObservable<IReadOnlyList<FacebookAPI.Post>> Posts { get; }

        IRxCommand RefeshPosts { get; }

        IRxCommand LoadMorePosts { get; }
    }

    public interface IPagesControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        string UserName { set; }

        IBitmap ProfilePhoto { set; }

        IObservable<bool> ShowUnpublishedPosts { get; }

        IRxProperty<IEnumerable<FacebookAPI.Page>> Pages { get; }

        IRxProperty<FSharpOption<FacebookAPI.Page>> CurrentPage { get; }

        IObservable<FacebookAPI.Page> LoadPage { get; }

        IObservable<Unit> CreatePost { get; }

        IObservable<Unit> LogOut { get; }


        IRxProperty<PersistentVector<FacebookAPI.Post>> Posts { get; }
       
        bool CanLoadMorePosts { set; }
        IObservable<Unit> LoadMorePosts { get; }

        bool CanRefreshPosts { set; }
        IObservable<Unit> RefreshPosts { get; }
    }

    public class PagesModel : MobileModel, IPagesViewModel, IPagesControllerModel
    { 
        private readonly IRxProperty<PersistentVector<FacebookAPI.Post>> _posts = 
            RxProperty.Create(PersistentVector<FacebookAPI.Post>.Empty());

        private readonly IRxCommand _loadMorePosts;
        private readonly IRxProperty<bool> _canLoadMorePosts = RxProperty.Create(false);

        private readonly IRxCommand _refreshPosts;
        private readonly IRxProperty<bool> _canRefreshPosts = RxProperty.Create(false);


        private readonly IRxCommand _createPost = RxCommand.Create();
        private readonly IRxCommand _logOut = RxCommand.Create();


        private readonly IRxProperty<IEnumerable<FacebookAPI.Page>> _pages = 
            RxProperty.Create((IEnumerable<FacebookAPI.Page>) new List<FacebookAPI.Page>());


        private readonly IRxProperty<bool> _showUnpublishedPosts =  RxProperty.Create<bool>(false);
        private readonly IRxProperty<string> _userName = RxProperty.Create<string>("");
        private readonly IRxProperty<IBitmap> _profilePhoto = RxProperty.Create<IBitmap>(null);

        private IRxProperty<FSharpOption<FacebookAPI.Page>> _currentPage = RxProperty.Create(FSharpOption<FacebookAPI.Page>.None);

        public PagesModel()
        {
            _refreshPosts = _canRefreshPosts.ToCommand();
            _loadMorePosts = _canLoadMorePosts.ToCommand();
        }

        IRxProperty<bool> IPagesViewModel.ShowUnpublishedPosts { get { return _showUnpublishedPosts; } }

        IObservable<bool> IPagesControllerModel.ShowUnpublishedPosts { get { return _showUnpublishedPosts; } }


        IObservable<string> IPagesViewModel.UserName { get { return _userName; } }

        string IPagesControllerModel.UserName { set { _userName.Value = value; } }


        IObservable<IBitmap> IPagesViewModel.ProfilePhoto { get { return _profilePhoto; } }

        IBitmap IPagesControllerModel.ProfilePhoto { set { _profilePhoto.Value = value; } }


        FacebookAPI.Page IPagesViewModel.CurrentPage { set { _currentPage.Value = FSharpOption<FacebookAPI.Page>.Some(value); } }

        IRxProperty<FSharpOption<FacebookAPI.Page>> IPagesControllerModel.CurrentPage 
        { 
            get { return _currentPage; } 
        }

        IObservable<FacebookAPI.Page> IPagesControllerModel.LoadPage 
        { 
            get { return this._currentPage.Where(x => OptionModule.IsSome(x)).Select(x => x.Value); } 
        }


        IObservable<IEnumerable<FacebookAPI.Page>> IPagesViewModel.Pages { get { return _pages; } }

        IRxProperty<IEnumerable<FacebookAPI.Page>> IPagesControllerModel.Pages { get { return _pages; } }


        IRxCommand IPagesViewModel.CreatePost { get { return _createPost; } }

        IObservable<Unit> IPagesControllerModel.CreatePost { get { return _createPost; } }


        IRxCommand IPagesViewModel.LogOut { get { return _logOut; } }

        IObservable<Unit> IPagesControllerModel.LogOut { get { return _logOut; } }



        IRxCommand IPagesViewModel.RefeshPosts { get { return _refreshPosts; } }

        IObservable<Unit> IPagesControllerModel.RefreshPosts { get { return _refreshPosts; } }

        bool IPagesControllerModel.CanRefreshPosts { set { _canRefreshPosts.Value = value; } }


        IRxCommand IPagesViewModel.LoadMorePosts { get { return _loadMorePosts; } }

        IObservable<Unit> IPagesControllerModel.LoadMorePosts { get { return _loadMorePosts; } }

        bool IPagesControllerModel.CanLoadMorePosts { set { _canLoadMorePosts.Value = value; } }


        IObservable<IReadOnlyList<FacebookAPI.Post>> IPagesViewModel.Posts
        {
            get { return this._posts.Select(x => (IReadOnlyList<FacebookAPI.Post>)x); }
        }

        IRxProperty<PersistentVector<FacebookAPI.Post>> IPagesControllerModel.Posts
        {
            get { return this._posts; }
        }
    }
}

