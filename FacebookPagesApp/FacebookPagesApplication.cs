using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using RxApp.Android;
using Android.App;
using Android.Runtime;
using ModernHttpClient; 
using Microsoft.FSharp.Core;

using Xamarin;

namespace FacebookPagesApp
{
    [Application]
    public sealed class FacebookPagesApplication : RxApplication
    {
        private const string XAMARIN_INSIGHTS_KEY = 
            "483137a8b42bc65cd39f3b649599093a6e09ce46";

        private Func<object,IDisposable> bindController;
        private IObservable<IMobileModel> rootState;
       
        public FacebookPagesApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override Type GetActivityType(IMobileViewModel model)
        {
                 if (model is ILoginViewModel       ) { return typeof(LoginActivity);        } 
            else if (model is IUnknownStateViewModel) { return typeof(UnknownStateActivity); } 
            else if (model is IPagesViewModel       ) { return typeof(PagesActivity);        } 
            else if (model is INewPostViewModel     ) { return typeof(NewPostActivity);      }  

            throw new Exception("No view for view model");
        }

        public override IObservable<IMobileModel> RootState()
        { 
            return rootState;
        }

        public override IDisposable BindController(object model)
        {
            return bindController(model);
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Insights.Initialize(XAMARIN_INSIGHTS_KEY, this.ApplicationContext);

            /* Code for getting the key hash for facebook
            foreach (var sig in this.PackageManager.GetPackageInfo(this.PackageName, Android.Content.PM.PackageInfoFlags.Signatures).Signatures)
            {
                var md = Java.Security.MessageDigest.GetInstance("SHA");
                md.Update(sig.ToByteArray());
                var key = System.Text.Encoding.UTF8.GetString(Android.Util.Base64.Encode(md.Digest(), 0));
                Android.Util.Log.Error("Key Hash=", key);
            }*/

            var httpClient = FunctionalHttp.Client.HttpClient.FromNetHttpClient(new HttpClient(new NativeMessageHandler()));

            this.rootState = 
                ApplicationController.rootState(FacebookSession.observe(this.ApplicationContext));

            this.bindController = 
                ApplicationController.bindController(
                    FacebookSession.getManagerWithFunc(() => LoginActivity.Current),
                    httpClient);
        }
    }
}