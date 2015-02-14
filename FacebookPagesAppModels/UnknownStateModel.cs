using System;
using ReactiveUI;
using RxApp;

namespace FacebookPagesApp
{
    public interface IUnknownStateViewModel : INavigableViewModel, IServiceViewModel
    {
    }

    public interface IUnknownStateControllerModel : INavigableControllerModel, IServiceControllerModel
    {
    }

    public class UnknownStateModel : MobileModel, IUnknownStateViewModel, IUnknownStateControllerModel
    {
        public UnknownStateModel()
        {
        }
    }
}

