using System;
using RxApp;

namespace FacebookPagesApp
{
    public interface IUnknownStateViewModel : INavigableViewModel, IServiceViewModel
    {
    }

    public interface IUnknownStateControllerModel : INavigableControllerModel, IServiceControllerModel
    {
    }

    public sealed class UnknownStateModel : MobileModel, IUnknownStateViewModel, IUnknownStateControllerModel
    {
        public UnknownStateModel()
        {
        }
    }
}

