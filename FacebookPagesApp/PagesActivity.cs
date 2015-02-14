using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using ReactiveUI;
using RxApp;


namespace FacebookPagesApp
{
    [Activity()]    
    public class PagesActivity : RxActivity<ILoginViewModel>
    {
        public PagesActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        protected override void OnPause()
        {
        }
    }
}

