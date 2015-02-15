using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows.Input;
using ReactiveUI;
using RxApp;
using Splat;

namespace FacebookPagesApp
{
    public interface IPagesViewModel : INavigableViewModel, IServiceViewModel
    {
        string UserName { get; }
        IBitmap ProfilePhoto { get; }
        bool ShowUnpublishedPosts { set; }

        IReadOnlyReactiveList<FacebookAPI.Page> Pages { get; }
        IReadOnlyReactiveList<object> Posts { get; }

        ICommand CreatePost { get; }

        ICommand LogOut { get; }

        ICommand RefeshPosts { get; }
        bool RefreshingPosts { get; }
    }

    public interface IPagesControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        string UserName { set; }
        IBitmap ProfilePhoto { set; }

        bool ShowUnpublishedPosts { get; }

        IReactiveList<FacebookAPI.Page> Pages { get; }
        IReactiveList<object> Posts { get; }

        IObservable<Unit> CreatePost { get; }

        IObservable<Unit> LogOut { get; }

        IObservable<Unit> RefreshPosts { get; }
        bool RefreshingPosts { set; }
    }

    public class PagesModel : MobileModel, IPagesViewModel, IPagesControllerModel
    {
        private readonly ReactiveList<FacebookAPI.Page> _pages = new ReactiveList<FacebookAPI.Page>();
        private readonly ReactiveList<object> _posts = new ReactiveList<object>();
        private readonly IReactiveCommand<object> _createPost = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> _logOut = ReactiveCommand.Create();
        private readonly IReactiveCommand<object> _refreshPosts;

        private bool _refreshingPosts = false;
        private bool _showUnpublishedPosts = false;
        private string _userName = "";
        private IBitmap _profilePhoto = null;

        public PagesModel()
        {
            _refreshPosts = this.WhenAnyValue(x => ((IPagesViewModel) x).RefreshingPosts).Select(x => !x).ToCommand();
        }
            
        bool IPagesViewModel.RefreshingPosts { get { return _refreshingPosts; } }

        bool IPagesControllerModel.RefreshingPosts { set { this.RaiseAndSetIfChanged(ref _refreshingPosts, value); } }


        bool IPagesViewModel.ShowUnpublishedPosts { set { this.RaiseAndSetIfChanged(ref _showUnpublishedPosts, value); } }

        bool IPagesControllerModel.ShowUnpublishedPosts { get { return _showUnpublishedPosts; } }


        string IPagesViewModel.UserName { get { return _userName; } }

        string IPagesControllerModel.UserName { set { this.RaiseAndSetIfChanged(ref _userName, value); } }


        IBitmap IPagesViewModel.ProfilePhoto { get { return _profilePhoto; } }

        IBitmap IPagesControllerModel.ProfilePhoto { set { this.RaiseAndSetIfChanged(ref _profilePhoto, value); } }


        IReadOnlyReactiveList<FacebookAPI.Page> IPagesViewModel.Pages { get { return _pages; } }

        IReactiveList<FacebookAPI.Page> IPagesControllerModel.Pages { get { return _pages; } }


        IReadOnlyReactiveList<object> IPagesViewModel.Posts { get { return _posts; } }

        IReactiveList<object> IPagesControllerModel.Posts { get { return _posts; } }


        ICommand IPagesViewModel.CreatePost { get { return _createPost; } }

        IObservable<Unit> IPagesControllerModel.CreatePost { get { return _createPost.Select(_ => Unit.Default); } }


        ICommand IPagesViewModel.LogOut { get { return _logOut; } }

        IObservable<Unit> IPagesControllerModel.LogOut { get { return _logOut.Select(_ => Unit.Default); } }


        ICommand IPagesViewModel.RefeshPosts { get { return _refreshPosts; } }

        IObservable<Unit> IPagesControllerModel.RefreshPosts { get { return _refreshPosts.Select(_ => Unit.Default); } }
    }
}

