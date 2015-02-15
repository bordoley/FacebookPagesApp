﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using Android.App;
using Android.Runtime;
using ModernHttpClient; 

using Xamarin;

namespace FacebookPagesApp
{
    [Application]
    public sealed class FacebookPagesApplication : RxApplication
    {
        public const string XAMARIN_INSIGHTS_KEY = 
            "483137a8b42bc65cd39f3b649599093a6e09ce46";

        public FacebookPagesApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override Type GetActivityType(object model)
        {
                 if (model is ILoginViewModel       ) { return typeof(LoginActivity);        } 
            else if (model is IUnknownStateViewModel) { return typeof(UnknownStateActivity); } 
            else if (model is IPagesViewModel       ) { return typeof(PagesActivity);        } 
            else if (model is INewPostViewModel     ) { return typeof(NewPostActivity);      }  

            throw new Exception("No view for view model");
        }

        public override IApplication ProvideApplication()
        {
            var httpClient = new HttpClient(new NativeMessageHandler());
            // FIXME: doing this here is causing all types of assmbly issues I don't want to deal with so fuck it.
            //var functionalClient = FunctionalHttp.Client.HttpClient.FromNetHttpClient(httpClient);

            return ApplicationController.create(
                this.NavigationStack,
                FacebookSession.observe(this.ApplicationContext),
                FacebookSession.getManagerWithFunc(() => LoginActivity.Current),
                httpClient);
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
        }
    }
}