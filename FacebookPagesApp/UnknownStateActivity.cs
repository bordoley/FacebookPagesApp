using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using RxApp;
using RxApp.Android;

namespace FacebookPagesApp
{
    [Activity()]    
    public sealed class UnknownStateActivity : RxActivity<IUnknownStateViewModel>
    {
        public UnknownStateActivity()
        {
        }
    }
}

