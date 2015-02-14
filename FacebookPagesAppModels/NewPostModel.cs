using System;
using System.Windows.Input;
using ReactiveUI;
using RxApp;
using System.Reactive;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    public interface INewPostViewModel : INavigableViewModel, IServiceViewModel
    {

    }

    public interface INewPostControllerModel : INavigableControllerModel, IServiceControllerModel
    {

    }

    public class NewPostModel : MobileModel, INewPostViewModel, INewPostControllerModel
    {
        public NewPostModel()
        {
        }
    }
}

