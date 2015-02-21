using System;
using RxApp;

namespace FacebookPagesApp
{
    public interface IUnknownStateViewModel : INavigationModel, IServiceViewModel
    {
    }

    public interface IUnknownStateControllerModel : INavigationModel, IServiceControllerModel
    {
    }

    public sealed class UnknownStateModel : MobileModel, IUnknownStateViewModel, IUnknownStateControllerModel
    {
        public UnknownStateModel()
        {
        }
    }
}

