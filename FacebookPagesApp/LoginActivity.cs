using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using System;
using System.Reactive.Linq;
using RxApp;
using RxApp.Android;

namespace FacebookPagesApp
{
    [Activity(Theme = "@style/LoginTheme", Label = "Pages Login")]    
    public sealed class LoginActivity : RxActivity<ILoginViewModel>
    {
        // This is very, very evil, but it works reliably and allows the rest of the code to pretend
        // that activities don't matter.
        private static LoginActivity _current;
        public static LoginActivity Current { get { return _current; } }

        private IDisposable subscription = null;
        private Button authButton;
        private TextView networkUnavailableMessage = null;

        public LoginActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            this.SetContentView(Resource.Layout.Login);

            authButton = this.FindViewById<Button>(Resource.Id.login_button);
            networkUnavailableMessage = this.FindViewById<TextView>(Resource.Id.network_unavailable_message);
        }

        protected override void OnResume()
        {
            base.OnResume();

            LoginActivity._current = this;

            this.subscription = Disposable.Compose(
                this.ViewModel.Login.Bind(this.authButton),

                this.ViewModel.LoginFailed
                    .BindTo(Toast.MakeText(
                                this, 
                                this.Resources.GetString(Resource.String.login_failed), 
                                ToastLength.Long).Show),

                this.ViewModel.NetworkAvailable
                              .Select(x => x ? ViewStates.Gone : ViewStates.Visible)
                              .BindTo(networkUnavailableMessage, x => x.Visibility),

                this.ViewModel.NetworkAvailable
                              .Select(x => x ? ViewStates.Visible : ViewStates.Gone)
                              .BindTo(authButton, x => x.Visibility)
            );
        }

        protected override void OnPause()
        {
            subscription.Dispose();

            LoginActivity._current = null;

            base.OnPause();
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            FacebookSession.onActivityResultDelegate (this, requestCode, resultCode, data);
        }
    }
}

