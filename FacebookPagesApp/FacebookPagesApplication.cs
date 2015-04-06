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
using System.Reactive.Subjects;

namespace FacebookPagesApp
{
    [Application]
    public sealed class FacebookPagesApplication : RxApplication
    {
        private const string XAMARIN_INSIGHTS_KEY = 
            "483137a8b42bc65cd39f3b649599093a6e09ce46";

        private IDisposable subscription;

        public FacebookPagesApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public override void OnCreate()
        {
            base.OnCreate();
            Insights.Initialize(XAMARIN_INSIGHTS_KEY, this.ApplicationContext);

            var builder = new RxAndroidApplicationBuilder();
            builder.RegisterActivityMapping<ILoginViewModel,LoginActivity>();
            builder.RegisterActivityMapping<IUnknownStateViewModel,UnknownStateActivity>();
            builder.RegisterActivityMapping<IPagesViewModel,PagesActivity>();
            builder.RegisterActivityMapping<INewPostViewModel,NewPostActivity>();

            var httpClient = FunctionalHttp.Client.HttpClient.FromNetHttpClient(new HttpClient(new NativeMessageHandler()));
            builder.NavigationApplicaction = 
                ApplicationController.createController(
                    FacebookSession.observeWithFunc(() => this.ApplicationContext),
                    FacebookSession.getManagerWithFunc(() => LoginActivity.Current),
                    httpClient);

            builder.CreatedActivities = this.CreatedActivities;
            this.subscription = builder.Build().Subscribe();

            /* Code for getting the key hash for facebook
            foreach (var sig in this.PackageManager.GetPackageInfo(this.PackageName, Android.Content.PM.PackageInfoFlags.Signatures).Signatures)
            {
                var md = Java.Security.MessageDigest.GetInstance("SHA");
                md.Update(sig.ToByteArray());
                var key = System.Text.Encoding.UTF8.GetString(Android.Util.Base64.Encode(md.Digest(), 0));
                Android.Util.Log.Error("Key Hash=", key);
            }*/
        }

        public override void OnTerminate()
        {
            subscription.Dispose();
            base.OnTerminate();
        }
    }
}