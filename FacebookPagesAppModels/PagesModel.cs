using System;
using System.Reactive;
using System.Windows.Input;
using ReactiveUI;
using RxApp;

namespace FacebookPagesApp
{
    public interface IPagesViewModel : INavigableViewModel, IServiceViewModel
    {
        IReadOnlyReactiveList<object> Pages { get; }
        IReadOnlyReactiveList<object> Posts { get; }

        ICommand CreatePost { get; }
    }

    public interface IPagesControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        IReactiveList<object> Pages { get; }
        IReactiveList<object> Posts { get; }

        IObservable<Unit> CreatePost { get; }
    }

    public class PagesModel : MobileModel, IPagesViewModel, IPagesControllerModel
    {
        public PagesModel()
        {
        }

        IReadOnlyReactiveList<object> IPagesViewModel.Pages
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IReadOnlyReactiveList<object> IPagesViewModel.Posts
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        ICommand IPagesViewModel.CreatePost
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IReactiveList<object> IPagesControllerModel.Pages
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IReactiveList<object> IPagesControllerModel.Posts
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        IObservable<Unit> IPagesControllerModel.CreatePost
        {
            get
            {
                throw new NotImplementedException();
            }
        }
    }
}

