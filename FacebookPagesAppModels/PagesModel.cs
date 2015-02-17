using System;
using System.Reactive.Linq;
using RxApp;
using Splat;
using Microsoft.FSharp.Core;

using Unit = System.Reactive.Unit;

namespace FacebookPagesApp
{
    public interface IPagesViewModel : INavigableViewModel, IServiceViewModel
    {
        IObservable<string> UserName { get; }
        IObservable<IBitmap> ProfilePhoto { get; }
        IRxProperty<bool> ShowUnpublishedPosts { get; }

        FacebookAPI.Page CurrentPage { set; }

        IRxReadOnlyList<FacebookAPI.Page> Pages { get; }

        IRxReadOnlyList<object> Posts { get; }

        IRxCommand CreatePost { get; }

        IRxCommand LogOut { get; }

        IRxCommand RefeshPosts { get; }
    }

    public interface IPagesControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        string UserName { set; }

        IBitmap ProfilePhoto { set; }

        IObservable<bool> ShowUnpublishedPosts { get; }

        IRxList<FacebookAPI.Page> Pages { get; }

        FSharpOption<FacebookAPI.Page> CurrentPage { set; get; }

        // Don't make F# code need to know about ReactiveUI
        IObservable<FacebookAPI.Page> LoadPage { get; }

        IRxList<object> Posts { get; }

        IObservable<Unit> CreatePost { get; }

        IObservable<Unit> LogOut { get; }

        IObservable<Unit> RefreshPosts { get; }
        bool RefreshingPosts { set; }
    }

    public class PagesModel : MobileModel, IPagesViewModel, IPagesControllerModel
    {
        private readonly IRxList<FacebookAPI.Page> _pages = RxList.Create<FacebookAPI.Page>();
        private readonly IRxList<object> _posts = RxList.Create<object>();

        private readonly IRxCommand _createPost = RxCommand.Create();
        private readonly IRxCommand _logOut = RxCommand.Create();
        private readonly IRxCommand _refreshPosts;

        private readonly IRxProperty<bool> _refreshingPosts = RxProperty.Create(false);
        private readonly IRxProperty<bool> _showUnpublishedPosts =  RxProperty.Create<bool>(false);
        private readonly IRxProperty<string> _userName = RxProperty.Create<string>("");
        private readonly IRxProperty<IBitmap> _profilePhoto = RxProperty.Create<IBitmap>(null);

        private IRxProperty<FSharpOption<FacebookAPI.Page>> _currentPage = RxProperty.Create(FSharpOption<FacebookAPI.Page>.None);

        public PagesModel()
        {
            _refreshPosts = _refreshingPosts.ToCommand();
        }

        bool IPagesControllerModel.RefreshingPosts { set { _refreshingPosts.Value = value; } }


        IRxProperty<bool> IPagesViewModel.ShowUnpublishedPosts { get { return _showUnpublishedPosts; } }

        IObservable<bool> IPagesControllerModel.ShowUnpublishedPosts { get { return _showUnpublishedPosts; } }


        IObservable<string> IPagesViewModel.UserName { get { return _userName; } }

        string IPagesControllerModel.UserName { set { _userName.Value = value; } }


        IObservable<IBitmap> IPagesViewModel.ProfilePhoto { get { return _profilePhoto; } }

        IBitmap IPagesControllerModel.ProfilePhoto { set { _profilePhoto.Value = value; } }


        FacebookAPI.Page IPagesViewModel.CurrentPage { set { _currentPage.Value = FSharpOption<FacebookAPI.Page>.Some(value); } }

        FSharpOption<FacebookAPI.Page> IPagesControllerModel.CurrentPage 
        { 
            get { return _currentPage.Value; }
            set { _currentPage.Value = value; } 
        }

        IObservable<FacebookAPI.Page> IPagesControllerModel.LoadPage 
        { 
            get { return this._currentPage.Where(x => OptionModule.IsSome(x)).Select(x => x.Value); } 
        }


        IRxReadOnlyList<FacebookAPI.Page> IPagesViewModel.Pages { get { return _pages.ToRxReadOnlyList(); } }

        IRxList<FacebookAPI.Page> IPagesControllerModel.Pages { get { return _pages; } }


        IRxReadOnlyList<object> IPagesViewModel.Posts { get { return _posts.ToRxReadOnlyList(); } }

        IRxList<object> IPagesControllerModel.Posts { get { return _posts; } }


        IRxCommand IPagesViewModel.CreatePost { get { return _createPost; } }

        IObservable<Unit> IPagesControllerModel.CreatePost { get { return _createPost; } }


        IRxCommand IPagesViewModel.LogOut { get { return _logOut; } }

        IObservable<Unit> IPagesControllerModel.LogOut { get { return _logOut; } }


        IRxCommand IPagesViewModel.RefeshPosts { get { return _refreshPosts; } }

        IObservable<Unit> IPagesControllerModel.RefreshPosts { get { return _refreshPosts; } }
    }
}

