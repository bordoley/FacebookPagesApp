using System;
using RxApp;
using System.Reactive;
using System.Reactive.Linq;

namespace FacebookPagesApp
{
    public interface ILoginViewModel : INavigableViewModel, IServiceViewModel
    {
        IObservable<Unit> LoginFailed { get; }
        IRxCommand Login { get; }
        IObservable<bool> NetworkAvailable { get; }
    }

    public interface ILoginControllerModel : INavigableControllerModel, IServiceControllerModel
    {
        IRxCommand LoginFailed { get; }
        IObservable<Unit> Login { get; }
        bool CanLogin { set; }
        bool NetworkAvailable { set; }
    }

    public sealed class LoginModel : MobileModel, ILoginViewModel, ILoginControllerModel
    {
        private readonly IRxProperty<bool> _canLogin = RxProperty.Create<bool>(true);

        private readonly IRxProperty<bool> _networkAvailable = RxProperty.Create<bool>(true);

        private readonly IRxCommand _login;

        private readonly IRxCommand _loginFailed = RxCommand.Create();


        public LoginModel() 
        { 
            _login = _canLogin.ToCommand();
        }


        IObservable<Unit> ILoginViewModel.LoginFailed { get { return _loginFailed; } }

        IRxCommand ILoginControllerModel.LoginFailed { get { return _loginFailed; } }


        IRxCommand ILoginViewModel.Login { get { return _login; } }

        IObservable<Unit> ILoginControllerModel.Login { get { return _login; } }


        IObservable<bool> ILoginViewModel.NetworkAvailable { get { return _networkAvailable; } }

        bool ILoginControllerModel.NetworkAvailable { set { _networkAvailable.Value = value; } }


        bool ILoginControllerModel.CanLogin { set { _canLogin.Value = value; } }
    }
}

