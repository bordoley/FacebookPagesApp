using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using RxApp;
using Android.App;
using Android.Runtime;
using Microsoft.FSharp.Core;

using Xamarin;

namespace FacebookPagesApp
{
    [Application]
    public sealed class FacebookPagesApplication : RxApplication
    {
        private FacebookPagesApplicationController applicationController;

        public FacebookPagesApplication(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
           
        }

        public override Type GetActivityType(object model)
        {
            if (model is ILoginViewModel) { return typeof(LoginActivity); } 
            else if (model is IUnknownStateViewModel) { return typeof(UnknownStateActivity); } 

            throw new Exception("No view for view model");
        }

        public override IDisposable ProvideController(object model)
        {
            return applicationController.Bind(model);
        }

        public override void OnCreate()
        {
            base.OnCreate();
            AndroidEnvironment.UnhandledExceptionRaiser += (sender, args) =>
                {
                    var path = Context.GetExternalFilesDir("exceptions").Path;
                    StreamWriter file = File.CreateText(path + "/exception.txt");
                    file.Write(args.Exception.StackTrace); // save the exception description and clean stack trace
                    file.Close();
                };
        }

        public override void Start()
        {
            applicationController = 
                new FacebookPagesApplicationController(
                    this.NavigationStack,
                    FacebookSession.observe(this.ApplicationContext),
                    FacebookSession.getManagerWithFunc(() => LoginActivity.Current));
            applicationController.Init();
        }

        public override void Stop()
        {
            (applicationController as IDisposable).Dispose();
        }
    }
}