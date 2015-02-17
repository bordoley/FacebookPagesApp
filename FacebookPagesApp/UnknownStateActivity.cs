using System;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;

using RxApp;

namespace FacebookPagesApp
{
    [Activity()]    
    public class UnknownStateActivity : RxActivity<IUnknownStateViewModel>
    {
        public UnknownStateActivity()
        {
        }
    }
}

