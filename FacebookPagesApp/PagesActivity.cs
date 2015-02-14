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
    [Activity(Theme = "@android:style/Theme.Holo.Light")]    
    public class PagesActivity : RxActivity<IPagesViewModel>
    {
        public PagesActivity()
        {
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            this.SetContentView(Resource.Layout.Pages);
        }

        protected override void OnResume()
        {
            base.OnResume();
        }

        protected override void OnPause()
        {
            base.OnPause();
        }
    }
}

